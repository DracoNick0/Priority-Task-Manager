import '../models/fixed_event.dart';
import '../models/archive_group.dart';
import '../models/recurrence_rule.dart';
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
    bool isDivisible = false,
    String link = '',
  });

  Future<void> updateTask(TaskItem task);

  /// Permanently deletes for Guests; API-backed sessions move the task to archive.
  Future<void> deleteTask(String taskId);

  /// Archives a task: moves it out of the active list into the
  /// archive (see [getArchivedTasks]). Archive is an online-exclusive
  /// feature (Guests have no access to it, consistent with other
  /// online-only features; see docs/VISION.md).
  Future<void> archiveTask(String taskId);

  /// Archives multiple tasks in one restore group.
  Future<void> archiveTaskGroup(List<String> taskIds);

  Future<void> setCompleted(String taskId, bool isCompleted);

  /// Adds one completion unit to a recurring occurrence.
  Future<void> completeTaskOccurrence(TaskItem occurrence);

  /// Removes the latest completion unit from only the specified occurrence.
  Future<void> undoTaskOccurrenceCompletion(TaskItem occurrence);

  /// Skips an unresolved recurring occurrence.
  Future<void> skipTaskOccurrence(TaskItem occurrence);

  Future<void> addDependency(String taskId, String dependsOnTaskId);

  Future<void> removeDependency(String taskId, String dependsOnTaskId);

  Future<UserProfile> getProfile();

  Future<void> updateProfile(UserProfile profile);

  Future<List<FixedEvent>> getEvents(String listId);

  /// Concrete events for a visible date window; guests have only plain events.
  Future<List<FixedEvent>> getEventOccurrences(
    String listId,
    DateTime from,
    DateTime to,
  );

  /// [recurrenceRule] is only supported by API-backed (Authenticated)
  /// sessions (issue #70); it is ignored by [LocalTaskRepository] (Guests).
  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
    String description = '',
    String link = '',
    RecurrenceRule? recurrenceRule,
  });

  Future<void> updateEvent(FixedEvent event);

  /// Permanently deletes for Guests; API-backed sessions move the event to archive.
  Future<void> deleteEvent(String eventId);

  /// Edits a single occurrence of a recurring series (issue #71). Only
  /// supported by API-backed (Authenticated) sessions; [LocalTaskRepository]
  /// (Guests) never has a [FixedEvent] with a non-null `seriesId` to call
  /// this on, so it fails fast instead.
  Future<void> editOccurrence({
    required String seriesId,
    required DateTime occurrenceDate,
    required String name,
    required DateTime startTime,
    required DateTime endTime,
    String description = '',
    String link = '',
    required RecurrenceEditTarget target,
    RecurrenceRule? recurrenceRule,
  });

  /// Deletes one or more occurrences of a recurring series (issue #71). Same
  /// Authenticated-only support as [editOccurrence].
  Future<void> deleteOccurrence({
    required String seriesId,
    required DateTime occurrenceDate,
    required RecurrenceEditTarget target,
  });

  /// Archive is an online-exclusive feature (Guests have no access to it,
  /// consistent with other online-only features; see docs/VISION.md).
  Future<List<TaskItem>> getArchivedTasks();

  /// Restores an archived task back to its original list, or to
  /// [targetListId] if supplied (required when the original list no longer
  /// exists; see [RestoreTargetListRequiredException]).
  Future<TaskItem> restoreArchivedTask(String taskId, {String? targetListId});

  /// Permanently deletes an archived task.
  Future<void> deleteArchivedTask(String taskId);

  /// Archived events and recurring-series segments for the authenticated account.
  Future<List<FixedEvent>> getArchivedEvents();

  /// Restores an archived event or recurring-series segment.
  Future<void> restoreArchivedEvent(String eventId);

  /// Permanently deletes an event from the archive.
  Future<void> deleteArchivedEvent(String eventId);

  /// Returns archived items grouped by the action or event series that created them.
  Future<List<ArchiveGroup>> getArchiveGroups();

  /// Restores every archived item in a group.
  Future<void> restoreArchiveGroup(ArchiveGroup group, {String? targetListId});

  /// Permanently deletes every archived task and event.
  Future<void> clearArchive();
}
