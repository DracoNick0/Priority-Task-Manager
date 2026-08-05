import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/api_schedule_repository.dart';
import '../data/schedule_repository.dart';
import '../models/schedule_models.dart';
import 'task_providers.dart';

enum ViewMode { minimalist, dense }

final viewModeProvider = StateProvider<ViewMode>((ref) => ViewMode.minimalist);

/// The [ScheduleRepository] used to compute the displayed schedule. Currently
/// always the sidecar-backed implementation (see docs/VISION.md's MVP scope:
/// the Flutter client runs the real scheduling algorithm, not a mock).
final scheduleRepositoryProvider = Provider<ScheduleRepository>((ref) {
  return ApiScheduleRepository();
});

/// The computed schedule for the active list's incomplete tasks, run through
/// the real `PriorityTaskManager` scheduling algorithm via the local sidecar.
final scheduleProvider = FutureProvider<DailySchedule>((ref) async {
  final activeListId = ref.watch(activeListIdProvider);
  if (activeListId == null) {
    return DailySchedule.empty();
  }

  final tasks = await ref.watch(tasksProvider(activeListId).future);
  final incompleteTasks = tasks.where((t) => !t.isCompleted).toList();
  final repository = ref.watch(scheduleRepositoryProvider);
  return repository.computeSchedule(tasks: incompleteTasks);
});
