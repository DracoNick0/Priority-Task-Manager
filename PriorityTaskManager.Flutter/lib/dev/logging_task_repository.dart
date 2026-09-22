import '../models/fixed_event.dart';
import '../models/task_item.dart';
import '../models/task_list.dart';
import '../models/user_profile.dart';
import 'dev_log_entry.dart';
import 'dev_log_sink.dart';
import '../data/task_repository.dart';

/// [TaskRepository] decorator that logs every call as a [DevLogEntry]
/// (issue #59). Wraps both [LocalTaskRepository] (Guests, the only logging
/// those calls get since there's no HTTP layer underneath) and
/// [ApiTaskRepository] (Authenticated, in addition to the raw HTTP-level
/// logging [LoggingHttpClient] already provides) so the higher-level call
/// (e.g. `addTask(...)`) is always visible regardless of auth state.
/// Delegates every method to [_inner] unchanged; only used when
/// `kDebugMode` is true (see `taskRepositoryProvider`).
class LoggingTaskRepository implements TaskRepository {
  LoggingTaskRepository(this._inner, {DevLogSink? sink})
    : _sink = sink ?? DevLogSink.instance;

  final TaskRepository _inner;
  final DevLogSink _sink;

  Future<T> _logged<T>(String label, Future<T> Function() action) async {
    final stopwatch = Stopwatch()..start();
    try {
      final result = await action();
      stopwatch.stop();
      _sink.log(
        DevLogEntry(
          kind: DevLogKind.domainEvent,
          label: label,
          timestamp: DateTime.now(),
          isError: false,
          durationMs: stopwatch.elapsedMilliseconds,
        ),
      );
      return result;
    } catch (error) {
      stopwatch.stop();
      _sink.log(
        DevLogEntry(
          kind: DevLogKind.domainEvent,
          label: label,
          timestamp: DateTime.now(),
          isError: true,
          durationMs: stopwatch.elapsedMilliseconds,
          detail: error.toString(),
        ),
      );
      rethrow;
    }
  }

  @override
  Future<List<TaskList>> getLists() => _logged('getLists()', _inner.getLists);

  @override
  Future<TaskList> createList({required String name, String? description}) =>
      _logged(
        'createList(name: $name)',
        () => _inner.createList(name: name, description: description),
      );

  @override
  Future<void> updateList(TaskList list) =>
      _logged('updateList(${list.id})', () => _inner.updateList(list));

  @override
  Future<void> deleteList(String listId) =>
      _logged('deleteList($listId)', () => _inner.deleteList(listId));

  @override
  Future<List<TaskItem>> getTasks(String listId) =>
      _logged('getTasks($listId)', () => _inner.getTasks(listId));

  @override
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
  }) => _logged(
    'addTask(listId: $listId, title: $title)',
    () => _inner.addTask(
      listId: listId,
      title: title,
      description: description,
      dueDate: dueDate,
      estimatedDurationMinutes: estimatedDurationMinutes,
      dependencies: dependencies,
      importance: importance,
      complexity: complexity,
      notBefore: notBefore,
      isPinned: isPinned,
      isDivisible: isDivisible,
    ),
  );

  @override
  Future<void> updateTask(TaskItem task) =>
      _logged('updateTask(${task.id})', () => _inner.updateTask(task));

  @override
  Future<void> deleteTask(String taskId) =>
      _logged('deleteTask($taskId)', () => _inner.deleteTask(taskId));

  @override
  Future<void> setCompleted(String taskId, bool isCompleted) => _logged(
    'setCompleted($taskId, $isCompleted)',
    () => _inner.setCompleted(taskId, isCompleted),
  );

  @override
  Future<void> addDependency(String taskId, String dependsOnTaskId) => _logged(
    'addDependency($taskId -> $dependsOnTaskId)',
    () => _inner.addDependency(taskId, dependsOnTaskId),
  );

  @override
  Future<void> removeDependency(String taskId, String dependsOnTaskId) =>
      _logged(
        'removeDependency($taskId -> $dependsOnTaskId)',
        () => _inner.removeDependency(taskId, dependsOnTaskId),
      );

  @override
  Future<UserProfile> getProfile() =>
      _logged('getProfile()', _inner.getProfile);

  @override
  Future<void> updateProfile(UserProfile profile) =>
      _logged('updateProfile()', () => _inner.updateProfile(profile));

  @override
  Future<List<FixedEvent>> getEvents(String listId) =>
      _logged('getEvents($listId)', () => _inner.getEvents(listId));

  @override
  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
  }) => _logged(
    'addEvent(listId: $listId, title: $title)',
    () => _inner.addEvent(
      listId: listId,
      title: title,
      startTime: startTime,
      endTime: endTime,
    ),
  );

  @override
  Future<void> updateEvent(FixedEvent event) =>
      _logged('updateEvent(${event.id})', () => _inner.updateEvent(event));

  @override
  Future<void> deleteEvent(String eventId) =>
      _logged('deleteEvent($eventId)', () => _inner.deleteEvent(eventId));
}
