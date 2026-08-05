import 'package:hive_ce_flutter/hive_flutter.dart';
import 'package:uuid/uuid.dart';

import '../models/task_item.dart';
import '../models/task_list.dart';
import 'task_repository.dart';

const String taskListsBoxName = 'task_lists';
const String tasksBoxName = 'tasks';

/// Local, on-device [TaskRepository] implementation backed by Hive boxes.
///
/// This is the only wired implementation for the offline/guest MVP shell;
/// an API-backed implementation is out of scope here (see issue #44).
class LocalTaskRepository implements TaskRepository {
  LocalTaskRepository(this._listsBox, this._tasksBox);

  final Box<TaskList> _listsBox;
  final Box<TaskItem> _tasksBox;
  final Uuid _uuid = const Uuid();

  static Future<LocalTaskRepository> open() async {
    final listsBox = await Hive.openBox<TaskList>(taskListsBoxName);
    final tasksBox = await Hive.openBox<TaskItem>(tasksBoxName);

    if (listsBox.isEmpty) {
      final defaultList = TaskList(id: const Uuid().v4(), name: 'General');
      await listsBox.put(defaultList.id, defaultList);
    }

    return LocalTaskRepository(listsBox, tasksBox);
  }

  @override
  Future<List<TaskList>> getLists() async => _listsBox.values.toList();

  @override
  Future<TaskList> createList({
    required String name,
    String? description,
  }) async {
    final list = TaskList(id: _uuid.v4(), name: name, description: description);
    await _listsBox.put(list.id, list);
    return list;
  }

  @override
  Future<void> updateList(TaskList list) async {
    await _listsBox.put(list.id, list);
  }

  @override
  Future<void> deleteList(String listId) async {
    final tasksToRemove = _tasksBox.values
        .where((task) => task.listId == listId)
        .map((task) => task.id)
        .toList();
    for (final taskId in tasksToRemove) {
      await _tasksBox.delete(taskId);
    }
    await _listsBox.delete(listId);
  }

  @override
  Future<List<TaskItem>> getTasks(String listId) async =>
      _tasksBox.values.where((task) => task.listId == listId).toList();

  @override
  Future<TaskItem> addTask({
    required String listId,
    required String title,
    String description = '',
    DateTime? dueDate,
    int estimatedDurationMinutes = 60,
  }) async {
    final task = TaskItem(
      id: _uuid.v4(),
      listId: listId,
      title: title,
      description: description,
      dueDate: dueDate,
      estimatedDurationMinutes: estimatedDurationMinutes,
    );
    await _tasksBox.put(task.id, task);
    return task;
  }

  @override
  Future<void> updateTask(TaskItem task) async {
    await _tasksBox.put(task.id, task);
  }

  @override
  Future<void> deleteTask(String taskId) async {
    // Dependents must not silently keep referencing a deleted prerequisite.
    for (final task in _tasksBox.values) {
      if (task.dependencies.contains(taskId)) {
        task.dependencies = List<String>.from(task.dependencies)
          ..remove(taskId);
        await _tasksBox.put(task.id, task);
      }
    }
    await _tasksBox.delete(taskId);
  }

  @override
  Future<void> setCompleted(String taskId, bool isCompleted) async {
    final task = _tasksBox.get(taskId);
    if (task == null) return;
    task.isCompleted = isCompleted;
    await _tasksBox.put(task.id, task);
  }

  @override
  Future<void> addDependency(String taskId, String dependsOnTaskId) async {
    if (taskId == dependsOnTaskId) return;
    final task = _tasksBox.get(taskId);
    if (task == null) return;
    if (task.dependencies.contains(dependsOnTaskId)) return;
    task.dependencies = List<String>.from(task.dependencies)
      ..add(dependsOnTaskId);
    await _tasksBox.put(task.id, task);
  }

  @override
  Future<void> removeDependency(String taskId, String dependsOnTaskId) async {
    final task = _tasksBox.get(taskId);
    if (task == null) return;
    task.dependencies = List<String>.from(task.dependencies)
      ..remove(dependsOnTaskId);
    await _tasksBox.put(task.id, task);
  }
}
