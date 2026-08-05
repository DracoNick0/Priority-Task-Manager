import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/local_task_repository.dart';
import '../data/task_repository.dart';
import '../models/task_item.dart';
import '../models/task_list.dart';

/// Provides the active [TaskRepository] implementation.
///
/// Only [LocalTaskRepository] is wired for this offline/guest MVP shell; an
/// API-backed implementation is expected to be swapped in here for #44
/// without changing any consuming provider or widget.
final taskRepositoryProvider = FutureProvider<TaskRepository>((ref) async {
  return LocalTaskRepository.open();
});

/// All task lists known to the active repository.
final taskListsProvider =
    AsyncNotifierProvider<TaskListsNotifier, List<TaskList>>(
      TaskListsNotifier.new,
    );

class TaskListsNotifier extends AsyncNotifier<List<TaskList>> {
  @override
  Future<List<TaskList>> build() async {
    final repository = await ref.watch(taskRepositoryProvider.future);
    return repository.getLists();
  }

  Future<void> createList(String name, {String? description}) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.createList(name: name, description: description);
    ref.invalidateSelf();
    await future;
  }

  Future<void> deleteList(String listId) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.deleteList(listId);
    ref.invalidateSelf();
    await future;
    ref.invalidate(tasksProvider(listId));
  }
}

/// The currently selected list id, driving which tasks are shown.
final activeListIdProvider = StateProvider<String?>((ref) => null);

/// Tasks belonging to a specific list id.
final tasksProvider =
    AsyncNotifierProvider.family<TasksNotifier, List<TaskItem>, String>(
      TasksNotifier.new,
    );

class TasksNotifier extends FamilyAsyncNotifier<List<TaskItem>, String> {
  @override
  Future<List<TaskItem>> build(String arg) async {
    final repository = await ref.watch(taskRepositoryProvider.future);
    return repository.getTasks(arg);
  }

  Future<TaskItem> addTask({
    required String title,
    String description = '',
    DateTime? dueDate,
    int estimatedDurationMinutes = 60,
  }) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    final created = await repository.addTask(
      listId: arg,
      title: title,
      description: description,
      dueDate: dueDate,
      estimatedDurationMinutes: estimatedDurationMinutes,
    );
    ref.invalidateSelf();
    await future;
    return created;
  }

  Future<void> updateTask(TaskItem task) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.updateTask(task);
    ref.invalidateSelf();
    await future;
  }

  Future<void> deleteTask(String taskId) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.deleteTask(taskId);
    ref.invalidateSelf();
    await future;
  }

  Future<void> setCompleted(String taskId, bool isCompleted) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.setCompleted(taskId, isCompleted);
    ref.invalidateSelf();
    await future;
  }

  Future<void> addDependency(String taskId, String dependsOnTaskId) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.addDependency(taskId, dependsOnTaskId);
    ref.invalidateSelf();
    await future;
  }

  Future<void> removeDependency(String taskId, String dependsOnTaskId) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.removeDependency(taskId, dependsOnTaskId);
    ref.invalidateSelf();
    await future;
  }
}
