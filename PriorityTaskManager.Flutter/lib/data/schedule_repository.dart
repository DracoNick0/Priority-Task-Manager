import '../models/effective_settings.dart';
import '../models/fixed_event.dart';
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
  /// current time), applying [settings] (resolved list overrides merged with
  /// the global defaults) and any fixed, unmovable [events].
  Future<DailySchedule> computeSchedule({
    required List<TaskItem> tasks,
    required EffectiveListSettings settings,
    List<FixedEvent> events,
    DateTime? now,
  });
}
