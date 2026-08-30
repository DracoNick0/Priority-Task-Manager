import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'session_provider.dart';

/// The bearer token to attach to authenticated API calls, derived from
/// [sessionControllerProvider] (see `lib/providers/session_provider.dart`).
/// Resolves to `null` while Guest or still resolving the entry choice, since
/// Guests have no access to online-only features like scheduling (see
/// docs/VISION.md).
final authTokenProvider = FutureProvider<String?>((ref) async {
  final session = await ref.watch(sessionControllerProvider.future);
  return session.token;
});

