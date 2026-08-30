import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'task_providers.dart';
import 'user_profile_provider.dart';

/// Ticks once a minute so the Left Rail's Engine Status clock stays live.
///
/// Placeholder for a future time-simulation provider (see
/// docs/ARCHITECTURE_SCHEDULING.md); currently always reflects wall-clock time.
final engineClockProvider = StreamProvider<DateTime>((ref) async* {
  yield DateTime.now();
  yield* Stream.periodic(const Duration(minutes: 1), (_) => DateTime.now());
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

