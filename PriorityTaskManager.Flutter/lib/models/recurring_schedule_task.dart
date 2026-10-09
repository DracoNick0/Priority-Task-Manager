import 'recurrence_rule.dart';

/// Persisted recurring-series data needed by the stateless schedule endpoint.
class RecurringScheduleTask {
  const RecurringScheduleTask({
    required this.id,
    required this.title,
    required this.description,
    required this.listId,
    required this.importance,
    required this.complexity,
    required this.points,
    required this.estimatedDuration,
    required this.isPinned,
    required this.beforePadding,
    required this.afterPadding,
    required this.isDivisible,
    required this.recurrenceRule,
    required this.seriesId,
    required this.progressionMode,
    required this.requiredCompletions,
    required this.occurrenceStates,
    this.showMissedIndicator = true,
    this.trackStreak = false,
    this.currentStreak = 0,
    this.bestStreak = 0,
  });

  final String id;
  final String title;
  final String description;
  final String listId;
  final int importance;
  final int complexity;
  final double points;
  final String estimatedDuration;
  final bool isPinned;
  final String? beforePadding;
  final String? afterPadding;
  final bool isDivisible;
  final RecurrenceRule recurrenceRule;
  final String? seriesId;
  final String progressionMode;
  final int requiredCompletions;
  final List<RecurringScheduleOccurrenceState> occurrenceStates;
  final bool showMissedIndicator;
  final bool trackStreak;
  final int currentStreak;
  final int bestStreak;

  Map<String, dynamic> toJson() => {
    'id': id,
    'listId': listId,
    'title': title,
    'description': description,
    'link': null,
    'isCompleted': false,
    'progress': 0.0,
    'importance': importance,
    'complexity': complexity,
    'points': points,
    'dueDate': null,
    'notBefore': null,
    'estimatedDuration': estimatedDuration,
    'dependencies': const <String>[],
    'isPinned': isPinned,
    'beforePadding': beforePadding,
    'afterPadding': afterPadding,
    'isDivisible': isDivisible,
    'recurrenceRule': recurrenceRule.toJson(),
    'seriesId': seriesId,
    'progressionMode': progressionMode,
    'requiredCompletions': requiredCompletions,
    'showMissedIndicator': showMissedIndicator,
    'trackStreak': trackStreak,
    'currentStreak': currentStreak,
    'bestStreak': bestStreak,
    'occurrenceStates': occurrenceStates
        .map((state) => state.toJson())
        .toList(),
  };

  factory RecurringScheduleTask.fromApiJson(Map<String, dynamic> json) {
    final rule = RecurrenceRule.fromJson(
      json['recurrenceRule'] as Map<dynamic, dynamic>?,
    );
    if (rule == null) {
      throw const FormatException(
        'Recurring task has no valid recurrence rule.',
      );
    }
    final rawStates = json['occurrenceStates'];
    if (rawStates is! List<dynamic>) {
      throw const FormatException(
        'Recurring task response has no occurrence-state metadata.',
      );
    }

    return RecurringScheduleTask(
      id: json['id'] as String,
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      listId: json['listId'] as String,
      importance: json['importance'] as int,
      complexity: json['complexity'] as int,
      points: (json['points'] as num).toDouble(),
      estimatedDuration: json['estimatedDuration'] as String,
      isPinned: json['isPinned'] as bool,
      beforePadding: json['beforePadding'] as String?,
      afterPadding: json['afterPadding'] as String?,
      isDivisible: json['isDivisible'] as bool,
      recurrenceRule: rule,
      seriesId: json['seriesId'] as String?,
      progressionMode:
          json['progressionMode'] as String? ?? 'RollForwardKeepBacklog',
      requiredCompletions: json['requiredCompletions'] as int? ?? 1,
      showMissedIndicator: json['showMissedIndicator'] as bool? ?? true,
      trackStreak: json['trackStreak'] as bool? ?? false,
      currentStreak: json['currentStreak'] as int? ?? 0,
      bestStreak: json['bestStreak'] as int? ?? 0,
      occurrenceStates: rawStates
          .map(
            (state) => RecurringScheduleOccurrenceState.fromApiJson(
              state as Map<String, dynamic>,
            ),
          )
          .toList(),
    );
  }
}

/// One persisted occurrence state serialized with a recurring schedule input.
class RecurringScheduleOccurrenceState {
  const RecurringScheduleOccurrenceState({
    required this.scheduledDate,
    required this.status,
    required this.completionCount,
    required this.completedAt,
  });

  final DateTime scheduledDate;
  final String status;
  final int completionCount;
  final DateTime? completedAt;

  Map<String, dynamic> toJson() => {
    'scheduledDate': scheduledDate.toIso8601String(),
    'status': status,
    'completionCount': completionCount,
    'completedAt': completedAt?.toIso8601String(),
  };

  factory RecurringScheduleOccurrenceState.fromApiJson(
    Map<String, dynamic> json,
  ) => RecurringScheduleOccurrenceState(
    scheduledDate: DateTime.parse(json['scheduledDate'] as String),
    status: json['status'] as String,
    completionCount: json['completionCount'] as int,
    completedAt: json['completedAt'] == null
        ? null
        : DateTime.parse(json['completedAt'] as String),
  );
}
