import 'dart:convert';

import 'package:http/http.dart' as http;

/// Fixed dev-only account credentials seeded by the API on startup (see
/// `PriorityTaskManager.API/Dev/DevAccountSeeder.cs`). Only usable against an
/// API instance running with `ASPNETCORE_ENVIRONMENT=Development` and a
/// reachable Postgres database (i.e. not the `LocalOnly` launch config).
class DevAutoLoginAccounts {
  static const String freeEmail = 'free@dev.local';
  static const String subscriptionEmail = 'subscriber@dev.local';
  static const String password = 'DevPassword123!';
}

/// Reads the `PTM_DEV_AUTOLOGIN` compile-time define (set via
/// `--dart-define=PTM_DEV_AUTOLOGIN=free` or `=subscriber`) and, if set, logs
/// in as the matching seeded dev account against [baseUri]'s
/// `POST /api/auth/login`, returning the bearer token to attach to
/// authenticated requests (e.g. `/api/schedule`).
///
/// Returns `null` when the define is unset, which is the default: the app
/// has no login screen yet, so outside of this dev define there is no way to
/// authenticate, and callers should fall back to unauthenticated local
/// endpoints (see `ApiScheduleRepository`).
Future<String?> resolveDevAutoLoginToken({
  required Uri baseUri,
  http.Client? httpClient,
}) async {
  const tier = String.fromEnvironment('PTM_DEV_AUTOLOGIN');
  if (tier.isEmpty) {
    return null;
  }

  final email = switch (tier) {
    'free' => DevAutoLoginAccounts.freeEmail,
    'subscriber' => DevAutoLoginAccounts.subscriptionEmail,
    _ => throw StateError(
      'Unknown PTM_DEV_AUTOLOGIN value "$tier" (expected "free" or "subscriber").',
    ),
  };

  final client = httpClient ?? http.Client();
  final http.Response response;
  try {
    response = await client.post(
      baseUri.resolve('/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'email': email,
        'password': DevAutoLoginAccounts.password,
      }),
    );
  } catch (error) {
    throw StateError(
      'Dev auto-login could not reach $baseUri. Make sure PriorityTaskManager.API '
      'is running in cloud mode (not LocalOnly) with Postgres reachable (see '
      'docs/WORKFLOW.md). Underlying error: $error',
    );
  }

  if (response.statusCode != 200) {
    throw StateError(
      'Dev auto-login as "$tier" failed (${response.statusCode}): ${response.body}',
    );
  }

  final json = jsonDecode(response.body) as Map<String, dynamic>;
  return json['token'] as String;
}
