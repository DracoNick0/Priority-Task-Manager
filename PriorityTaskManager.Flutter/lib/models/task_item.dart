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

  TaskItem copyWith({
    String? title,
    String? description,
    bool? isCompleted,
    DateTime? dueDate,
    bool clearDueDate = false,
    int? estimatedDurationMinutes,
    List<String>? dependencies,
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
    );
  }
}
