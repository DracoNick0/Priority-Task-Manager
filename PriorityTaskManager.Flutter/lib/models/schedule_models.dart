/// A single computed block of scheduled work for a task, as returned by the
/// scheduling engine (see `ScheduleRepository`). Purely a display model; the
/// task's persisted state lives in [TaskItem]/Hive.
class ScheduledTask {
  final String id;
  final String title;
  final DateTime startTime;
  final DateTime endTime;
  final DateTime? dueDate;
  final double chunkHours;
  final bool isFuture;

  ScheduledTask({
    required this.id,
    required this.title,
    required this.startTime,
    required this.endTime,
    required this.dueDate,
    required this.chunkHours,
    this.isFuture = false,
  });
}

/// The computed daily schedule for the active list: today's placed tasks,
/// upcoming placed tasks, tasks the scheduler could not fit, and the
/// least-slack summary shown on the CLI dashboard meter.
class DailySchedule {
  final List<ScheduledTask> todayTasks;
  final List<ScheduledTask> futureTasks;
  final List<String> unscheduledTaskIds;
  final String leastSlackTask;
  final String realisticSlack;
  final String actualSlack;

  DailySchedule({
    required this.todayTasks,
    required this.futureTasks,
    this.unscheduledTaskIds = const [],
    required this.leastSlackTask,
    required this.realisticSlack,
    required this.actualSlack,
  });

  /// An empty schedule shown while there are no tasks to place.
  factory DailySchedule.empty() => DailySchedule(
    todayTasks: [],
    futureTasks: [],
    leastSlackTask: 'None',
    realisticSlack: '-',
    actualSlack: '-',
  );
}
