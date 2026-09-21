import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import 'dev_log_entry.dart';
import 'dev_log_sink.dart';

/// Keys whose values must never be logged verbatim (auth tokens/passwords).
const Set<String> _sensitiveJsonKeys = {'password', 'token'};

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
    final label = '${request.method} ${request.url.path}';
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
          detail: isError
              ? _redact(utf8.decode(bytes, allowMalformed: true))
              : null,
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
          detail: error.toString(),
        ),
      );
      rethrow;
    }
  }

  /// Best-effort redaction: if the body is a JSON object, blank out any
  /// [_sensitiveJsonKeys] values; otherwise return the raw text unchanged.
  String _redact(String body) {
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map<String, dynamic>) {
        for (final key in _sensitiveJsonKeys) {
          if (decoded.containsKey(key)) {
            decoded[key] = '<redacted>';
          }
        }
        return jsonEncode(decoded);
      }
    } catch (_) {
      // Not JSON; fall through to the raw body.
    }
    return body;
  }
}
