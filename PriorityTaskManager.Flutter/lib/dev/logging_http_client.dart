import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import 'dev_log_entry.dart';
import 'dev_log_sink.dart';

/// Keys whose values must never be logged verbatim (auth tokens/passwords).
const Set<String> _sensitiveJsonKeys = {'password', 'token'};

/// Logged request/response bodies are truncated past this length so a single
/// large payload can't bloat the in-memory ring buffer.
const int _maxBodyPreviewLength = 4000;

/// [http.BaseClient] wrapper that records every request/response as a
/// [DevLogEntry] (issue #59), so `ApiTaskRepository`, `ApiScheduleRepository`,
/// and `AuthRepository` get call logging without duplicating logging code in
/// each of them — they just get constructed with one of these instead of a
/// plain [http.Client] when `kDebugMode` is true (see the relevant provider
/// files for the `kDebugMode` gating).
class LoggingHttpClient extends http.BaseClient {
  LoggingHttpClient(this._inner, {DevLogSink? sink})
    : _sink = sink ?? DevLogSink.instance;

  final http.Client _inner;
  final DevLogSink _sink;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final stopwatch = Stopwatch()..start();
    final label = '${request.method} ${request.url}';
    final requestPreview = _requestBodyPreview(request);
    try {
      final response = await _inner.send(request);
      stopwatch.stop();
      final bytes = await response.stream.toBytes();
      final isError = response.statusCode >= 400;
      _sink.log(
        DevLogEntry(
          kind: DevLogKind.httpCall,
          label: '$label -> ${response.statusCode}',
          timestamp: DateTime.now(),
          isError: isError,
          durationMs: stopwatch.elapsedMilliseconds,
          detail: _combineDetail(
            requestPreview,
            'resp',
            _redact(utf8.decode(bytes, allowMalformed: true)),
          ),
        ),
      );
      return http.StreamedResponse(
        Stream.value(bytes),
        response.statusCode,
        contentLength: bytes.length,
        request: response.request,
        headers: response.headers,
        isRedirect: response.isRedirect,
        persistentConnection: response.persistentConnection,
        reasonPhrase: response.reasonPhrase,
      );
    } catch (error) {
      stopwatch.stop();
      _sink.log(
        DevLogEntry(
          kind: DevLogKind.httpCall,
          label: '$label -> failed',
          timestamp: DateTime.now(),
          isError: true,
          durationMs: stopwatch.elapsedMilliseconds,
          detail: _combineDetail(requestPreview, 'error', error.toString()),
        ),
      );
      rethrow;
    }
  }

  /// Captures the outgoing request body (redacted), if any — [http.Request]
  /// is the only [http.BaseRequest] subtype with a readable body.
  String? _requestBodyPreview(http.BaseRequest request) {
    if (request is http.Request && request.body.isNotEmpty) {
      return _redact(request.body);
    }
    return null;
  }

  /// Joins the optional request preview with a labeled response/error
  /// preview; returns null when there's nothing worth showing.
  String? _combineDetail(
    String? requestPreview,
    String responseLabel,
    String responseContent,
  ) {
    final parts = <String>[
      if (requestPreview != null) 'req: $requestPreview',
      if (responseContent.isNotEmpty) '$responseLabel: $responseContent',
    ];
    return parts.isEmpty ? null : parts.join('\n');
  }

  /// Best-effort redaction: if the body is a JSON object, blank out any
  /// [_sensitiveJsonKeys] values; otherwise return the raw text unchanged.
  /// Result is truncated to [_maxBodyPreviewLength].
  String _redact(String body) {
    var result = body;
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map<String, dynamic>) {
        for (final key in _sensitiveJsonKeys) {
          if (decoded.containsKey(key)) {
            decoded[key] = '<redacted>';
          }
        }
        result = jsonEncode(decoded);
      }
    } catch (_) {
      // Not JSON; keep the raw body.
    }
    return result.length > _maxBodyPreviewLength
        ? '${result.substring(0, _maxBodyPreviewLength)}… (truncated)'
        : result;
  }
}
