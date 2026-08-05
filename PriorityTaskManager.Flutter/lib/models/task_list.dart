import 'package:hive_ce/hive.dart';

part 'task_list.g.dart';

/// A named container for a collection of [TaskItem]s, stored locally via Hive.
@HiveType(typeId: 1)
class TaskList extends HiveObject {
  TaskList({required this.id, required this.name, this.description});

  @HiveField(0)
  final String id;

  @HiveField(1)
  String name;

  @HiveField(2)
  String? description;

  TaskList copyWith({String? name, String? description}) {
    return TaskList(
      id: id,
      name: name ?? this.name,
      description: description ?? this.description,
    );
  }
}
