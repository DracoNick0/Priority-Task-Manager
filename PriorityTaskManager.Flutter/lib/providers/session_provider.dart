import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/auth_repository.dart';
import '../data/secure_token_store.dart';

enum SessionStatus {
  /// First run: the user has never made the Guest-vs-Authenticated entry
  /// choice yet.
  needsEntryChoice,

  /// No account; all data stays local (see #32's offline `TaskRepository`).
  guest,

  /// Logged in; [SessionState.token] should be attached to authenticated API
  /// calls (e.g. `/api/schedule`).
  authenticated,
}

class SessionState {
  const SessionState({
    required this.status,
    this.email,
    this.token,
    this.betaGracePeriodNotice,
  });

  final SessionStatus status;
  final String? email;
  final String? token;
  final String? betaGracePeriodNotice;

  static const needsEntryChoice = SessionState(
    status: SessionStatus.needsEntryChoice,
  );
  static const guest = SessionState(status: SessionStatus.guest);
}

/// Owns Guest-vs-Authenticated session state (issue #44): resolves the
/// persisted session on startup, drives the one-time guest-first entry
/// choice, and performs login/register/logout. UI code should never touch
/// [AuthRepository]/[SecureTokenStore] directly; go through this controller.
class SessionController extends AsyncNotifier<SessionState> {
  late final AuthRepository _authRepository = AuthRepository();
  late final SecureTokenStore _tokenStore = SecureTokenStore();

  @override
  Future<SessionState> build() async {
    final stored = await _tokenStore.readSession();
    if (stored == null) {
      return SessionState.needsEntryChoice;
    }
    if (!stored.isAuthenticated) {
      return SessionState.guest;
    }
    if (stored.isExpired) {
      // Never force a re-login; silently fall back to Guest's offline
      // experience instead of blocking or nagging the user.
      await _tokenStore.writeSession(const StoredSession.guest());
      return SessionState.guest;
    }
    return SessionState(
      status: SessionStatus.authenticated,
      email: stored.email,
      token: stored.token,
    );
  }

  Future<void> continueAsGuest() async {
    await _tokenStore.writeSession(const StoredSession.guest());
    state = const AsyncData(SessionState.guest);
  }

  Future<void> login(String email, String password) async {
    final result = await _authRepository.login(email, password);
    await _applyAuthenticated(email: email, result: result);
  }

  Future<void> register(String email, String password) async {
    final result = await _authRepository.register(email, password);
    await _applyAuthenticated(email: email, result: result);
  }

  /// Signs out of the account and returns to Guest mode, without re-showing
  /// the one-time entry choice (see #44's guest-first, never-forced-login
  /// requirement).
  Future<void> logout() async {
    await _tokenStore.writeSession(const StoredSession.guest());
    state = const AsyncData(SessionState.guest);
  }

  Future<void> _applyAuthenticated({
    required String email,
    required AuthResult result,
  }) async {
    await _tokenStore.writeSession(
      StoredSession.authenticated(
        email: email,
        token: result.token,
        expiresAtUtc: result.expiresAtUtc,
      ),
    );
    state = AsyncData(
      SessionState(
        status: SessionStatus.authenticated,
        email: email,
        token: result.token,
        betaGracePeriodNotice: result.betaGracePeriodNotice,
      ),
    );
  }
}

final sessionControllerProvider =
    AsyncNotifierProvider<SessionController, SessionState>(
      SessionController.new,
    );
