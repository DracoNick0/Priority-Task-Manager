import 'package:hive_ce_flutter/hive_flutter.dart';
import 'package:uuid/uuid.dart';

import '../models/fixed_event.dart';
import '../models/recurrence_rule.dart';
import '../models/task_item.dart';
import '../models/task_list.dart';
import '../models/user_profile.dart';
import 'task_repository.dart';

const String taskListsBoxName = 'task_lists';
const String tasksBoxName = 'tasks';
const String userProfileBoxName = 'user_profile';
const String eventsBoxName = 'events';
const String _profileKey = 'profile';

/// Local, on-device [TaskRepository] implementation backed by Hive boxes.
///
/// This is the only wired implementation for the offline/guest MVP shell;
/// an API-backed implementation is out of scope here (see issue #44).
class LocalTaskRepository implements TaskRepository {
  LocalTaskRepository(
    this._listsBox,
    this._tasksBox,
    this._profileBox,
    this._eventsBox,
  );

  final Box<TaskList> _listsBox;
  final Box<TaskItem> _tasksBox;
  final Box<UserProfile> _profileBox;
  final Box<FixedEvent> _eventsBox;
  final Uuid _uuid = const Uuid();

  static Future<LocalTaskRepository> open() async {
    final listsBox = await Hive.openBox<TaskList>(taskListsBoxName);
    final tasksBox = await Hive.openBox<TaskItem>(tasksBoxName);
    final profileBox = await Hive.openBox<UserProfile>(userProfileBoxName);
    final eventsBox = await Hive.openBox<FixedEvent>(eventsBoxName);

    if (listsBox.isEmpty) {
      final defaultList = TaskList(id: const Uuid().v4(), name: 'General');
      await listsBox.put(defaultList.id, defaultList);
    }
    if (profileBox.isEmpty) {
      await profileBox.put(_profileKey, UserProfile());
    }

    return LocalTaskRepository(listsBox, tasksBox, profileBox, eventsBox);
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
    final eventsToRemove = _eventsBox.values
        .where((event) => event.listId == listId)
        .map((event) => event.id)
        .toList();
    for (final eventId in eventsToRemove) {
      await _eventsBox.delete(eventId);
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
    List<String>? dependencies,
    int importance = 5,
    int complexity = 1,
    DateTime? notBefore,
    bool isPinned = false,
    bool isDivisible = true,
  }) async {
    final task = TaskItem(
      id: _uuid.v4(),
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
  Future<void> archiveTask(String taskId) {
    throw UnsupportedError(
      'Archive is an online-exclusive feature; Guests do not have access to it.',
    );
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

  @override
  Future<UserProfile> getProfile() async =>
      _profileBox.get(_profileKey) ?? UserProfile();

  @override
  Future<void> updateProfile(UserProfile profile) async {
    await _profileBox.put(_profileKey, profile);
  }

  @override
  Future<List<FixedEvent>> getEvents(String listId) async =>
      _eventsBox.values.where((event) => event.listId == listId).toList();

  @override
  Future<List<FixedEvent>> getEventOccurrences(
    String listId,
    DateTime from,
    DateTime to,
  ) async => _eventsBox.values
      .where(
        (event) =>
            event.listId == listId &&
            event.endTime.isAfter(DateTime(from.year, from.month, from.day)) &&
            event.startTime.isBefore(
              DateTime(to.year, to.month, to.day).add(const Duration(days: 1)),
            ),
      )
      .toList();

  @override
  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
    RecurrenceRule? recurrenceRule,
  }) async {
    // Guests have no server-side series concept; the UI never passes a
    // non-null recurrenceRule here (recurrence is Authenticated-only), so
    // this is ignored rather than persisted.
    final event = FixedEvent(
      id: _uuid.v4(),
      listId: listId,
      title: title,
      startTime: startTime,
      endTime: endTime,
    );
    await _eventsBox.put(event.id, event);
    return event;
  }

  @override
  Future<void> updateEvent(FixedEvent event) async {
    await _eventsBox.put(event.id, event);
  }

  @override
  Future<void> deleteEvent(String eventId) async {
    await _eventsBox.delete(eventId);
  }

  // Guests never have a recurring FixedEvent (no server-side series
  // concept; see addEvent above), so the UI never calls these for a Guest
  // session — same fail-fast pattern as the archive methods below.

  @override
  Future<void> editOccurrence({
    required String seriesId,
    required DateTime occurrenceDate,
    required String name,
    required DateTime startTime,
    required DateTime endTime,
    required RecurrenceEditTarget target,
    RecurrenceRule? recurrenceRule,
  }) {
    throw UnsupportedError(
      'Recurring events are an online-exclusive feature; Guests do not have access to it.',
    );
  }

  @override
  Future<void> deleteOccurrence({
    required String seriesId,
    required DateTime occurrenceDate,
    required RecurrenceEditTarget target,
  }) {
    throw UnsupportedError(
      'Recurring events are an online-exclusive feature; Guests do not have access to it.',
    );
  }

  // ---- Archive ----
  //
  // Archive is an online-exclusive feature (Guests have no access to it,
  // consistent with other online-only features; see docs/VISION.md). The UI
  // never calls these for a Guest session, so they fail fast if reached.

  @override
  Future<List<TaskItem>> getArchivedTasks() {
    throw UnsupportedError(
      'Archive is an online-exclusive feature; Guests do not have access to it.',
    );
  }

  @override
  Future<TaskItem> restoreArchivedTask(String taskId, {String? targetListId}) {
    throw UnsupportedError(
      'Archive is an online-exclusive feature; Guests do not have access to it.',
    );
  }

  @override
  Future<void> deleteArchivedTask(String taskId) {
    throw UnsupportedError(
      'Archive is an online-exclusive feature; Guests do not have access to it.',
    );
  }
}
