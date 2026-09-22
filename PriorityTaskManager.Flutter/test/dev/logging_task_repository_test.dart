// Unit tests for LoggingTaskRepository (issue #59): verifies calls are
// logged as domain events and delegate correctly to the inner repository,
// including on failure.

import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/dev/dev_log_entry.dart';
import 'package:priority_task_manager/dev/dev_log_sink.dart';
import 'package:priority_task_manager/dev/logging_task_repository.dart';
import 'package:priority_task_manager/models/fixed_event.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/models/user_profile.dart';

class _FakeTaskRepository implements TaskRepository {
  bool throwOnGetLists = false;

  @override
  Future<List<TaskList>> getLists() async {
    if (throwOnGetLists) {
      throw StateError('boom');
    }
    return [TaskList(id: 'list-1', name: 'Work')];
  }

  @override
  Future<TaskList> createList({required String name, String? description}) =>
      throw UnimplementedError();

  @override
  Future<void> updateList(TaskList list) async {}

  @override
  Future<void> deleteList(String listId) async {}

  @override
  Future<List<TaskItem>> getTasks(String listId) async => [];

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
  }) => throw UnimplementedError();

  @override
  Future<void> updateTask(TaskItem task) async {}

  @override
  Future<void> deleteTask(String taskId) async {}

  @override
  Future<void> setCompleted(String taskId, bool isCompleted) async {}

  @override
  Future<void> addDependency(String taskId, String dependsOnTaskId) async {}

  @override
  Future<void> removeDependency(String taskId, String dependsOnTaskId) async {}

  @override
  Future<UserProfile> getProfile() async => UserProfile();

  @override
  Future<void> updateProfile(UserProfile profile) async {}

  @override
  Future<List<FixedEvent>> getEvents(String listId) async => [];

  @override
  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
  }) => throw UnimplementedError();

  @override
  Future<void> updateEvent(FixedEvent event) async {}

  @override
  Future<void> deleteEvent(String eventId) async {}
}

void main() {
  group('LoggingTaskRepository', () {
    test('logs a successful call and returns the delegate result', () async {
      final sink = DevLogSink();
      final repository = LoggingTaskRepository(
        _FakeTaskRepository(),
        sink: sink,
      );

      final lists = await repository.getLists();

      expect(lists, hasLength(1));
      final entry = sink.entries.single;
      expect(entry.kind, DevLogKind.domainEvent);
      expect(entry.isError, isFalse);
      expect(entry.label, 'getLists()');
    });

    test('logs and rethrows on failure', () async {
      final sink = DevLogSink();
      final fake = _FakeTaskRepository()..throwOnGetLists = true;
      final repository = LoggingTaskRepository(fake, sink: sink);

      await expectLater(repository.getLists(), throwsStateError);

      final entry = sink.entries.single;
      expect(entry.isError, isTrue);
      expect(entry.detail, contains('boom'));
    });
  });
}
