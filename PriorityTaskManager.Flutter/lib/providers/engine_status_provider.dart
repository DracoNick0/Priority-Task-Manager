import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'session_provider.dart';
import 'task_providers.dart';
import 'user_profile_provider.dart';

/// Ticks once a minute so the Left Rail's Engine Status clock stays live,
/// unless the active list has a simulated-time override set, in which case
/// it freezes on that instant instead (mirrors the CLI's `list time`).
/// Simulated time only applies to Authenticated sessions, since scheduling
/// is online-exclusive and Guests never compute a schedule.
final engineClockProvider = StreamProvider<DateTime>((ref) async* {
  final session = ref.watch(sessionControllerProvider).asData?.value;
  final isAuthenticated = session?.status == SessionStatus.authenticated;
  final activeListId = ref.watch(activeListIdProvider);
  final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
  final list = lists.where((l) => l.id == activeListId).firstOrNull;
  final simulatedTime = isAuthenticated ? list?.simulatedTime : null;

  if (simulatedTime != null) {
    yield simulatedTime;
    return;
  }

  yield DateTime.now();
  yield* Stream.periodic(const Duration(minutes: 1), (_) => DateTime.now());
});

/// Whether the Engine Status clock is currently showing a frozen simulated
/// time rather than real wall-clock time.
final isSimulatedTimeProvider = Provider<bool>((ref) {
  final session = ref.watch(sessionControllerProvider).asData?.value;
  final isAuthenticated = session?.status == SessionStatus.authenticated;
  final activeListId = ref.watch(activeListIdProvider);
  final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
  final list = lists.where((l) => l.id == activeListId).firstOrNull;
  return isAuthenticated && list?.simulatedTime != null;
});

/// The active scheduling/algorithm mode label shown in the Engine Status
/// indicator, resolved from the active list's effective settings (list
/// override merged with the global default).
final algorithmModeProvider = Provider<String>((ref) {
  final activeListId = ref.watch(activeListIdProvider);
  final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
  final profile = ref.watch(userProfileProvider).asData?.value;
  if (profile == null) return 'Gold Panning';

  final list = lists.where((l) => l.id == activeListId).firstOrNull;
  final mode = list == null
      ? profile.schedulingMode
      : (list.schedulingMode ?? profile.schedulingMode);
  return mode == 0 ? 'Gold Panning' : 'Constraint Optimization';
});

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
