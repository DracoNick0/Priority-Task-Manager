import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/schedule_models.dart';
import '../models/task_item.dart';
import 'local_sidecar.dart';
import 'schedule_repository.dart';

/// Formats a duration as .NET's default `TimeSpan` JSON shape (`hh:mm:ss`).
String _formatDuration(Duration duration) {
  String twoDigits(int n) => n.toString().padLeft(2, '0');
  final hours = duration.inHours;
  final minutes = duration.inMinutes.remainder(60);
  final seconds = duration.inSeconds.remainder(60);
  return '${twoDigits(hours)}:${twoDigits(minutes)}:${twoDigits(seconds)}';
}

/// Renders total minutes as a "D days H hours M minutes" string, matching the
/// CLI dashboard's slack summary formatting.
String _formatSlackMinutes(double? totalMinutes) {
  if (totalMinutes == null) return '-';
  final isNegative = totalMinutes < 0;
  final absMinutes = totalMinutes.abs().round();
  final days = absMinutes ~/ (24 * 60);
  final hours = (absMinutes % (24 * 60)) ~/ 60;
  final minutes = absMinutes % 60;
  final sign = isNegative ? '-' : '';
  return '$sign$days days $hours hours $minutes minutes';
}

/// [ScheduleRepository] implementation that computes the schedule by calling
/// the unauthenticated local sidecar's `/api/local/schedule` endpoint (see
/// `PriorityTaskManager.API/Local/LocalScheduleEndpoints.cs`), which runs the
/// real `PriorityTaskManager` scheduling strategies against the tasks/profile
/// this client sends. Nothing is persisted server-side; Hive stays the source
/// of truth.
class ApiScheduleRepository implements ScheduleRepository {
  ApiScheduleRepository({http.Client? httpClient})
    : _httpClient = httpClient ?? http.Client();

  final http.Client _httpClient;

  @override
  Future<DailySchedule> computeSchedule({
    required List<TaskItem> tasks,
    DateTime? now,
  }) async {
    if (tasks.isEmpty) {
      return DailySchedule.empty();
    }

    final baseUri = await LocalSidecar.instance.ensureRunning();
    final effectiveNow = now ?? DateTime.now();

    final requestBody = jsonEncode({
      'tasks': tasks.map(_taskToJson).toList(),
      'events': const <Object?>[],
      'profile': _defaultProfileJson(),
      'now': effectiveNow.toIso8601String(),
    });

    final response = await _httpClient.post(
      baseUri.resolve('/api/local/schedule'),
      headers: {'Content-Type': 'application/json'},
      body: requestBody,
    );

    if (response.statusCode != 200) {
      throw StateError(
        'Schedule computation failed (${response.statusCode}): ${response.body}',
      );
    }

    final json = jsonDecode(response.body) as Map<String, dynamic>;
    return _toDailySchedule(json, effectiveNow);
  }

  Map<String, dynamic> _taskToJson(TaskItem task) => {
    'id': task.id,
    'title': task.title,
    'isCompleted': task.isCompleted,
    'progress': 0.0,
    'importance': task.importance,
    'complexity': task.complexity,
    'points': 0.0,
    'dueDate': task.dueDate?.toIso8601String(),
    'notBefore': task.notBefore?.toIso8601String(),
    'estimatedDuration': _formatDuration(
      Duration(minutes: task.estimatedDurationMinutes),
    ),
    'dependencies': task.dependencies,
    'isPinned': task.isPinned,
    'beforePadding': null,
    'afterPadding': null,
    'isDivisible': task.isDivisible,
  };

  /// Default scheduling preferences (mirrors `UserProfile`'s defaults) until
  /// list/profile settings are editable from the Flutter UI.
  Map<String, dynamic> _defaultProfileJson() => {
    'workStartTime': '09:00:00',
    'workEndTime': '17:00:00',
    'workDays': ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
    'schedulingMode': 'GoldPanning',
    'desiredBreatherDuration': '00:15:00',
    'slackThresholdDire': 0.5,
    'slackThresholdPressing': 1.0,
    'slackThresholdFocus': 3.0,
    'slackThresholdSafe': 5.0,
  };

  DailySchedule _toDailySchedule(Map<String, dynamic> json, DateTime now) {
    final scheduledTasksJson = (json['scheduledTasks'] as List<dynamic>? ?? [])
        .cast<Map<String, dynamic>>();

    final todayTasks = <ScheduledTask>[];
    final futureTasks = <ScheduledTask>[];

    for (final taskJson in scheduledTasksJson) {
      final parts = (taskJson['scheduledParts'] as List<dynamic>? ?? [])
          .cast<Map<String, dynamic>>();
      if (parts.isEmpty) continue;

      final startTimes = parts
          .map((p) => DateTime.parse(p['startTime'] as String))
          .toList();
      final endTimes = parts
          .map((p) => DateTime.parse(p['endTime'] as String))
          .toList();
      final start = startTimes.reduce((a, b) => a.isBefore(b) ? a : b);
      final end = endTimes.reduce((a, b) => a.isAfter(b) ? a : b);
      final totalHours = parts.fold<double>(
        0,
        (sum, p) =>
            sum +
            DateTime.parse(p['endTime'] as String)
                    .difference(DateTime.parse(p['startTime'] as String))
                    .inMinutes /
                60.0,
      );

      final isToday =
          start.year == now.year &&
          start.month == now.month &&
          start.day == now.day;

      final scheduledTask = ScheduledTask(
        id: taskJson['id'] as String,
        title: (taskJson['title'] as String?) ?? '',
        startTime: start,
        endTime: end,
        dueDate: taskJson['dueDate'] == null
            ? null
            : DateTime.parse(taskJson['dueDate'] as String),
        chunkHours: totalHours,
        isFuture: !isToday,
      );

      (isToday ? todayTasks : futureTasks).add(scheduledTask);
    }

    todayTasks.sort((a, b) => a.startTime.compareTo(b.startTime));
    futureTasks.sort((a, b) => a.startTime.compareTo(b.startTime));

    return DailySchedule(
      todayTasks: todayTasks,
      futureTasks: futureTasks,
      unscheduledTaskIds: (json['unscheduledTaskIds'] as List<dynamic>? ?? [])
          .cast<String>(),
      leastSlackTask: (json['leastSlackTaskTitle'] as String?) ?? 'None',
      realisticSlack: _formatSlackMinutes(
        (json['leastSlackRealisticMinutes'] as num?)?.toDouble(),
      ),
      actualSlack: _formatSlackMinutes(
        (json['leastSlackActualMinutes'] as num?)?.toDouble(),
      ),
    );
  }
}
