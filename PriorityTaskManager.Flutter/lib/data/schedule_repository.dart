import '../models/schedule_models.dart';
import '../models/task_item.dart';

/// Client-side abstraction over computing a [DailySchedule] from a set of tasks.
///
/// Implementations run the real scheduling algorithms from `PriorityTaskManager`
/// (see docs/VISION.md's MVP scope: the Flutter client must run the actual
/// scheduler, not a mock). This repository never persists anything itself;
/// [TaskRepository]/Hive remains the single source of truth for task data.
abstract class ScheduleRepository {
  /// Computes the schedule for [tasks] as of [now] (defaults to the caller's
  /// current time), using default scheduling preferences until list/profile
  /// settings are editable from the Flutter UI.
  Future<DailySchedule> computeSchedule({
    required List<TaskItem> tasks,
    DateTime? now,
  });
}
