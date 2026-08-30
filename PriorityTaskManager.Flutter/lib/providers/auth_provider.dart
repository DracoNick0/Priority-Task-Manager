import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'session_provider.dart';

/// The bearer token to attach to authenticated API calls, derived from
/// [sessionControllerProvider] (see `lib/providers/session_provider.dart`).
/// Resolves to `null` while Guest or still resolving the entry choice, since
/// callers should fall back to unauthenticated local endpoints in that case.
final authTokenProvider = FutureProvider<String?>((ref) async {
  final session = await ref.watch(sessionControllerProvider.future);
  return session.token;
});

