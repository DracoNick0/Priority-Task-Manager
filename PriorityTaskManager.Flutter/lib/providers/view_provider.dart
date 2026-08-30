import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/api_schedule_repository.dart';
import '../data/schedule_repository.dart';
import '../models/effective_settings.dart';
import '../models/schedule_models.dart';
import 'auth_provider.dart';
import 'event_providers.dart';
import 'task_providers.dart';
import 'user_profile_provider.dart';

enum ViewMode { minimalist, dense, fluid }

final viewModeProvider = StateProvider<ViewMode>((ref) => ViewMode.minimalist);

/// The [ScheduleRepository] used to compute the displayed schedule. Currently
/// always the API-backed implementation (see docs/VISION.md's MVP scope: the
/// Flutter client runs the real scheduling algorithm, not a mock). Depends on
/// [authTokenProvider] so it calls the authenticated `/api/schedule` route
/// when dev auto-login is active, or the unauthenticated local route otherwise.
final scheduleRepositoryProvider = FutureProvider<ScheduleRepository>((
  ref,
) async {
  final authToken = await ref.watch(authTokenProvider.future);
  return ApiScheduleRepository(authToken: authToken);
});

/// The computed schedule for the active list's incomplete tasks, run through
/// the real `PriorityTaskManager` scheduling algorithm via a locally-running
/// scheduling API (see docs/WORKFLOW.md for how to start it), with the
/// list's effective settings (list overrides merged with the global
/// defaults) and fixed events applied.
final scheduleProvider = FutureProvider<DailySchedule>((ref) async {
  final activeListId = ref.watch(activeListIdProvider);
  if (activeListId == null) {
    return DailySchedule.empty();
  }

  final tasks = await ref.watch(tasksProvider(activeListId).future);
  final incompleteTasks = tasks.where((t) => !t.isCompleted).toList();

  final lists = await ref.watch(taskListsProvider.future);
  final list = lists.where((l) => l.id == activeListId).firstOrNull;
  final profile = await ref.watch(userProfileProvider.future);
  final settings = list == null
      ? EffectiveListSettings.fromProfile(profile)
      : EffectiveListSettings.resolve(list, profile);

  final events = await ref.watch(eventsProvider(activeListId).future);

  final repository = await ref.watch(scheduleRepositoryProvider.future);
  return repository.computeSchedule(
    tasks: incompleteTasks,
    settings: settings,
    events: events,
  );
});

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
