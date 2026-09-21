// Unit tests for LoggingHttpClient (issue #59): verifies it records success
// and failure entries in DevLogSink without altering the underlying
// response, and redacts sensitive JSON fields in logged error bodies.

import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:priority_task_manager/dev/dev_log_entry.dart';
import 'package:priority_task_manager/dev/dev_log_sink.dart';
import 'package:priority_task_manager/dev/logging_http_client.dart';

void main() {
  group('LoggingHttpClient', () {
    test('logs a successful call and preserves the response body', () async {
      final sink = DevLogSink();
      final mock = MockClient(
        (request) async => http.Response('{"ok":true}', 200),
      );
      final client = LoggingHttpClient(mock, sink: sink);

      final response = await client.get(Uri.parse('http://x/api/tasks'));

      expect(response.statusCode, 200);
      expect(response.body, '{"ok":true}');
      expect(sink.entries, hasLength(1));
      final entry = sink.entries.single;
      expect(entry.kind, DevLogKind.httpCall);
      expect(entry.isError, isFalse);
      expect(entry.label, contains('GET'));
      expect(entry.label, contains('200'));
    });

    test('logs an error response and redacts sensitive fields', () async {
      final sink = DevLogSink();
      final mock = MockClient(
        (request) async => http.Response(
          jsonEncode({'error': 'bad request', 'password': 'hunter2'}),
          400,
        ),
      );
      final client = LoggingHttpClient(mock, sink: sink);

      final response = await client.post(Uri.parse('http://x/api/auth/login'));

      expect(response.statusCode, 400);
      final entry = sink.entries.single;
      expect(entry.isError, isTrue);
      expect(entry.detail, isNot(contains('hunter2')));
      expect(entry.detail, contains('<redacted>'));
    });

    test('logs a thrown network error', () async {
      final sink = DevLogSink();
      final mock = MockClient((request) async => throw Exception('offline'));
      final client = LoggingHttpClient(mock, sink: sink);

      await expectLater(
        client.get(Uri.parse('http://x/api/tasks')),
        throwsException,
      );

      final entry = sink.entries.single;
      expect(entry.isError, isTrue);
      expect(entry.label, contains('failed'));
    });
  });
}
