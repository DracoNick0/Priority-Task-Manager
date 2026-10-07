import 'package:hive_ce/hive.dart';

part 'task_item.g.dart';

/// A single unit of work belonging to a [TaskList], stored locally via Hive.
@HiveType(typeId: 0)
class TaskItem extends HiveObject {
  TaskItem({
    required this.id,
    required this.listId,
    required this.title,
    this.description = '',
    this.isCompleted = false,
    this.dueDate,
    this.estimatedDurationMinutes = 60,
    List<String>? dependencies,
    this.importance = 5,
    this.complexity = 1,
    this.notBefore,
    this.isPinned = false,
    this.isDivisible = false,
    this.link = '',
  }) : dependencies = dependencies ?? <String>[];

  @HiveField(0)
  final String id;

  @HiveField(1)
  String listId;

  @HiveField(2)
  String title;

  @HiveField(3)
  String description;

  @HiveField(4)
  bool isCompleted;

  @HiveField(5)
  DateTime? dueDate;

  @HiveField(6)
  int estimatedDurationMinutes;

  /// IDs of `TaskItem`s that must be completed before this one, mirroring
  /// the dependency model in `PriorityTaskManager.Models.TaskItem`.
  @HiveField(7)
  List<String> dependencies;

  /// User-defined importance (1-10), mirroring `PriorityTaskManager.Models.TaskItem.Importance`.
  /// Feeds the scheduling algorithm, defaults to 5.
  @HiveField(8)
  int importance;

  /// Cognitive load/effort, mirroring `PriorityTaskManager.Models.TaskItem.Complexity`.
  @HiveField(9)
  int complexity;

  /// Earliest allowed start time, mirroring `PriorityTaskManager.Models.TaskItem.NotBefore`.
  @HiveField(10)
  DateTime? notBefore;

  /// Whether the scheduling algorithm should skip this task, mirroring
  @HiveField(11)
  bool isPinned;

  /// Whether the task can be split across multiple scheduled chunks, mirroring
  @HiveField(12)
  bool isDivisible;

  @HiveField(13)
  String link;

  TaskItem copyWith({
    String? title,
    String? description,
    bool? isCompleted,
    DateTime? dueDate,
    bool clearDueDate = false,
    int? estimatedDurationMinutes,
    List<String>? dependencies,
    int? importance,
    int? complexity,
    DateTime? notBefore,
    bool clearNotBefore = false,
    bool? isPinned,
    bool? isDivisible,
    String? link,
  }) {
    return TaskItem(
      id: id,
      listId: listId,
      title: title ?? this.title,
      description: description ?? this.description,
      isCompleted: isCompleted ?? this.isCompleted,
      dueDate: clearDueDate ? null : (dueDate ?? this.dueDate),
      estimatedDurationMinutes:
          estimatedDurationMinutes ?? this.estimatedDurationMinutes,
      dependencies: dependencies ?? List<String>.from(this.dependencies),
      importance: importance ?? this.importance,
      complexity: complexity ?? this.complexity,
      notBefore: clearNotBefore ? null : (notBefore ?? this.notBefore),
      isPinned: isPinned ?? this.isPinned,
      isDivisible: isDivisible ?? this.isDivisible,
      link: link ?? this.link,
    );
  }
}
