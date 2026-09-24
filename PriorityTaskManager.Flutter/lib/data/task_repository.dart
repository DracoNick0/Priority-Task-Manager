import '../models/fixed_event.dart';
import '../models/task_item.dart';
import '../models/task_list.dart';
import '../models/user_profile.dart';

/// Thrown by [TaskRepository.restoreArchivedTask] when the archived task's
/// original list no longer exists and no [targetListId] argument was
/// supplied, so the caller must prompt the user to choose a list and retry.
class RestoreTargetListRequiredException implements Exception {}

/// Client-side abstraction over task/list/event/profile persistence and
/// mutation.
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
    List<String>? dependencies,
    int importance = 5,
    int complexity = 1,
    DateTime? notBefore,
    bool isPinned = false,
    bool isDivisible = true,
  });

  Future<void> updateTask(TaskItem task);

  Future<void> deleteTask(String taskId);

  /// Archives a completed task: moves it out of the active list into the
  /// archive (see [getArchivedTasks]). Archive is an online-exclusive
  /// feature (Guests have no access to it, consistent with other
  /// online-only features; see docs/VISION.md).
  Future<void> archiveTask(String taskId);

  Future<void> setCompleted(String taskId, bool isCompleted);

  Future<void> addDependency(String taskId, String dependsOnTaskId);

  Future<void> removeDependency(String taskId, String dependsOnTaskId);

  Future<UserProfile> getProfile();

  Future<void> updateProfile(UserProfile profile);

  Future<List<FixedEvent>> getEvents(String listId);

  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
  });

  Future<void> updateEvent(FixedEvent event);

  Future<void> deleteEvent(String eventId);

  /// Archive is an online-exclusive feature (Guests have no access to it,
  /// consistent with other online-only features; see docs/VISION.md).
  Future<List<TaskItem>> getArchivedTasks();

  /// Restores an archived task back to its original list, or to
  /// [targetListId] if supplied (required when the original list no longer
  /// exists; see [RestoreTargetListRequiredException]).
  Future<TaskItem> restoreArchivedTask(String taskId, {String? targetListId});

  /// Permanently deletes an archived task.
  Future<void> deleteArchivedTask(String taskId);
}
