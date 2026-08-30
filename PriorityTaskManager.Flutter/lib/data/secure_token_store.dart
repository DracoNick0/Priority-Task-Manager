import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// A persisted session: either a Guest choice (no account) or an
/// authenticated account's bearer token, plus whether the user has ever made
/// the one-time entry choice at all (see `docs/VISION.md`'s guest-first entry
/// flow).
class StoredSession {
  const StoredSession.guest() : email = null, token = null, expiresAtUtc = null;

  const StoredSession.authenticated({
    required this.email,
    required this.token,
    required this.expiresAtUtc,
  });

  final String? email;
  final String? token;
  final DateTime? expiresAtUtc;

  bool get isAuthenticated => token != null;

  bool get isExpired =>
      expiresAtUtc != null && expiresAtUtc!.isBefore(DateTime.now());

  Map<String, dynamic> toJson() => {
    'email': email,
    'token': token,
    'expiresAtUtc': expiresAtUtc?.toIso8601String(),
  };

  static StoredSession fromJson(Map<String, dynamic> json) {
    final token = json['token'] as String?;
    if (token == null) {
      return const StoredSession.guest();
    }
    return StoredSession.authenticated(
      email: json['email'] as String?,
      token: token,
      expiresAtUtc: DateTime.parse(json['expiresAtUtc'] as String),
    );
  }
}

/// Persists the current session (Guest or Authenticated) in platform secure
/// storage (see issue #44's "secure local storage of the returned JWT bearer
/// token per client platform" requirement), so a returning user does not see
/// the one-time entry choice or login screen again. Storage is a no-op-safe
/// wrapper around `flutter_secure_storage`, which uses Keychain/Keystore/DPAPI
/// on native platforms and `window.localStorage` on web.
class SecureTokenStore {
  SecureTokenStore({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  static const _sessionKey = 'ptm_session_v1';

  final FlutterSecureStorage _storage;

  /// Reads the persisted session, or `null` if the user has never made the
  /// one-time entry choice yet (first run).
  Future<StoredSession?> readSession() async {
    final raw = await _storage.read(key: _sessionKey);
    if (raw == null) return null;
    return StoredSession.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  Future<void> writeSession(StoredSession session) =>
      _storage.write(key: _sessionKey, value: jsonEncode(session.toJson()));

  Future<void> clear() => _storage.delete(key: _sessionKey);
}
