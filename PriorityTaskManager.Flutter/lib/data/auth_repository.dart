import 'dart:convert';

import 'package:http/http.dart' as http;

import 'api_schedule_repository.dart';

/// Thrown when registration/login fails, carrying a message safe to show
/// directly to the user (mirrors the API's `{ "error": "..." }` bodies, or a
/// generic message for network/unexpected failures).
class AuthException implements Exception {
  const AuthException(this.message);

  final String message;

  @override
  String toString() => message;
}

/// A successful registration/login result: the bearer token to attach as
/// `Authorization: Bearer <token>`, its expiry, and an optional beta-grace-period
/// notice (see `PriorityTaskManager.API/Auth/AuthDtos.cs`'s `AuthResponse`).
class AuthResult {
  const AuthResult({
    required this.token,
    required this.expiresAtUtc,
    this.betaGracePeriodNotice,
  });

  final String token;
  final DateTime expiresAtUtc;
  final String? betaGracePeriodNotice;
}

/// Calls the API's `POST /api/auth/register` and `POST /api/auth/login`
/// endpoints (see `PriorityTaskManager.API/Auth/AuthEndpoints.cs`).
class AuthRepository {
  AuthRepository({Uri? baseUri, http.Client? httpClient})
    : baseUri = baseUri ?? ApiScheduleRepository.defaultBaseUri,
      _httpClient = httpClient ?? http.Client();

  final Uri baseUri;
  final http.Client _httpClient;

  Future<AuthResult> register(String email, String password) => _post(
    '/api/auth/register',
    {'email': email, 'password': password},
  );

  Future<AuthResult> login(String email, String password) => _post(
    '/api/auth/login',
    {'email': email, 'password': password},
  );

  Future<AuthResult> _post(String path, Map<String, String> body) async {
    final http.Response response;
    try {
      response = await _httpClient.post(
        baseUri.resolve(path),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(body),
      );
    } catch (error) {
      throw AuthException(
        'Could not reach the server at $baseUri. Check your connection and try again.',
      );
    }

    if (response.statusCode == 200) {
      final json = jsonDecode(response.body) as Map<String, dynamic>;
      return AuthResult(
        token: json['token'] as String,
        expiresAtUtc: DateTime.parse(json['expiresAtUtc'] as String),
        betaGracePeriodNotice: json['betaGracePeriodNotice'] as String?,
      );
    }

    if (response.statusCode == 401) {
      throw const AuthException('Incorrect email or password.');
    }
    if (response.statusCode == 409) {
      throw const AuthException('An account with this email already exists.');
    }

    String message = 'Something went wrong. Please try again.';
    try {
      final json = jsonDecode(response.body) as Map<String, dynamic>;
      if (json['error'] is String) {
        message = json['error'] as String;
      }
    } catch (_) {
      // Non-JSON error body; fall back to the generic message above.
    }
    throw AuthException(message);
  }
}
