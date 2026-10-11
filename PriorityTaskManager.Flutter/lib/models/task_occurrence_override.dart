/// A persisted edit to one recurring task occurrence, keyed by its original date.
class TaskOccurrenceOverride {
  const TaskOccurrenceOverride({
    required this.originalOccurrenceDate,
    required this.title,
    required this.description,
    required this.dueDate,
    required this.notBefore,
    required this.estimatedDuration,
    required this.importance,
    required this.complexity,
    required this.isPinned,
    required this.isDivisible,
    required this.link,
  });

  final DateTime originalOccurrenceDate;
  final String title;
  final String description;
  final DateTime? dueDate;
  final DateTime? notBefore;
  final String estimatedDuration;
  final int importance;
  final int complexity;
  final bool isPinned;
  final bool isDivisible;
  final String? link;

  Map<String, dynamic> toJson() => {
    'originalOccurrenceDate': originalOccurrenceDate.toIso8601String(),
    'title': title,
    'description': description,
    'dueDate': dueDate?.toIso8601String(),
    'notBefore': notBefore?.toIso8601String(),
    'estimatedDuration': estimatedDuration,
    'importance': importance,
    'complexity': complexity,
    'isPinned': isPinned,
    'isDivisible': isDivisible,
    'link': link,
  };

  factory TaskOccurrenceOverride.fromJson(Map<String, dynamic> json) =>
      TaskOccurrenceOverride(
        originalOccurrenceDate: DateTime.parse(
          json['originalOccurrenceDate'] as String,
        ),
        title: json['title'] as String? ?? '',
        description: json['description'] as String? ?? '',
        dueDate: json['dueDate'] == null
            ? null
            : DateTime.parse(json['dueDate'] as String),
        notBefore: json['notBefore'] == null
            ? null
            : DateTime.parse(json['notBefore'] as String),
        estimatedDuration: json['estimatedDuration'] as String,
        importance: json['importance'] as int,
        complexity: json['complexity'] as int,
        isPinned: json['isPinned'] as bool,
        isDivisible: json['isDivisible'] as bool,
        link: json['link'] as String?,
      );
}

/// A cancelled scheduled date in a recurring task series.
class TaskRecurrenceException {
  const TaskRecurrenceException({
    required this.originalOccurrenceDate,
    required this.isCancelled,
  });

  final DateTime originalOccurrenceDate;
  final bool isCancelled;

  Map<String, dynamic> toJson() => {
    'originalOccurrenceDate': originalOccurrenceDate.toIso8601String(),
    'isCancelled': isCancelled,
  };

  factory TaskRecurrenceException.fromJson(Map<String, dynamic> json) =>
      TaskRecurrenceException(
        originalOccurrenceDate: DateTime.parse(
          json['originalOccurrenceDate'] as String,
        ),
        isCancelled: json['isCancelled'] as bool? ?? true,
      );
}
