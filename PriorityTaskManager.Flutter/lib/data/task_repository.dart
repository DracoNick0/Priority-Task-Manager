import '../models/task_item.dart';
import '../models/task_list.dart';

/// Client-side abstraction over task/list persistence and mutation.
///
/// This is the seam a future API-backed implementation (see issue #44) plugs
/// into; UI and state-management code must depend only on this interface,
/// never on a concrete storage technology.
abstract class TaskRepository {
  Future<List<TaskList>> getLists();

  Future<TaskList> createList({required String name, String? description});

  Future<void> updateList(TaskList list);

  Future<void> deleteList(String listId);

  Future<List<TaskItem>> getTasks(String listId);

  Future<TaskItem> addTask({
    required String listId,
    required String title,
    String description = '',
    DateTime? dueDate,
    int estimatedDurationMinutes = 60,
  });

  Future<void> updateTask(TaskItem task);

  Future<void> deleteTask(String taskId);

  Future<void> setCompleted(String taskId, bool isCompleted);

  Future<void> addDependency(String taskId, String dependsOnTaskId);

  Future<void> removeDependency(String taskId, String dependsOnTaskId);
}
