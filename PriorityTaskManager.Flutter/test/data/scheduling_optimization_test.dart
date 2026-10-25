import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/models/recurring_schedule_task.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/providers/task_providers.dart';

class _FakeMemoryTaskRepository implements TaskRepository {
  final Map<String, List<TaskItem>> _tasks = {};
  final List<TaskList> _lists = [TaskList(id: 'list-1', name: 'Work')];

  @override
  Future<List<TaskList>> getLists() async => _lists;

  @override
  Future<TaskList> createList({required String name, String? description}) async {
    final list = TaskList(id: 'list-${_lists.length + 1}', name: name);
    _lists.add(list);
    return list;
  }

  @override
  Future<void> updateList(TaskList list) async {
    final index = _lists.indexWhere((l) => l.id == list.id);
    if (index >= 0) _lists[index] = list;
  }

  @override
  Future<void> deleteList(String listId) async {
    _lists.removeWhere((l) => l.id == listId);
  }

  @override
  Future<List<TaskItem>> getTasks(String listId) async =>
      _tasks[listId] ?? [];

  @override
  Future<TaskItem> getTaskOccurrence({
    required String listId,
    required String seriesId,
    required DateTime occurrenceDate,
  }) => throw UnimplementedError();

  @override
  Future<List<RecurringScheduleTask>> getRecurringTasksForScheduling(
    String listId,
  ) async => [];

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
    bool isDivisible = false,
    String link = '',
    RecurrenceRule? recurrenceRule,
    String progressionMode = 'RollForwardKeepBacklog',
    int requiredCompletions = 1,
    bool showMissedIndicator = true,
    bool trackStreak = false,
  }) async {
    final item = TaskItem(
      id: 'task-${DateTime.now().microsecondsSinceEpoch}',
      listId: listId,
      title: title,
      description: description,
      dueDate: dueDate,
      estimatedDurationMinutes: estimatedDurationMinutes,
      importance: importance,
      complexity: complexity,
      notBefore: notBefore,
      isPinned: isPinned,
      isDivisible: isDivisible,
      link: link,
    );
    (_tasks[listId] ??= []).add(item);
    return item;
  }

  @override
  Future<void> updateTask(TaskItem task) async {
    final list = _tasks[task.listId];
    if (list != null) {
      final index = list.indexWhere((t) => t.id == task.id);
      if (index >= 0) list[index] = task;
    }
  }

  @override
  Future<void> deleteTask(String taskId) async {
    for (final list in _tasks.values) {
      list.removeWhere((t) => t.id == taskId);
    }
  }

  @override
  Future<void> setCompleted(String taskId, bool isCompleted) async {
    for (final list in _tasks.values) {
      final index = list.indexWhere((t) => t.id == taskId);
      if (index >= 0) {
        final t = list[index];
        list[index] = TaskItem(
          id: t.id,
          listId: t.listId,
          title: t.title,
          isCompleted: isCompleted,
          estimatedDurationMinutes: t.estimatedDurationMinutes,
          importance: t.importance,
          dueDate: t.dueDate,
        );
      }
    }
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  test('Cosmetic task edits do not bump scheduling trigger', () async {
    final repository = _FakeMemoryTaskRepository();
    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
      ],
    );
    addTearDown(container.dispose);

    const listId = 'list-1';
    final initialTrigger = container.read(schedulingTriggerProvider(listId));
    expect(initialTrigger, 0);

    // Add a task
    final taskNotifier = container.read(tasksProvider(listId).notifier);
    final task = await taskNotifier.addTask(
      title: 'Original Title',
      description: 'Original Description',
      estimatedDurationMinutes: 60,
    );

    // Adding a task bumps the trigger
    final triggerAfterAdd = container.read(schedulingTriggerProvider(listId));
    expect(triggerAfterAdd, 1);

    // Cosmetic update: only description and link change
    final updatedTask = TaskItem(
      id: task.id,
      listId: task.listId,
      title: task.title,
      description: 'Updated Description',
      link: 'https://example.com',
      estimatedDurationMinutes: task.estimatedDurationMinutes,
      importance: task.importance,
      complexity: task.complexity,
      dueDate: task.dueDate,
    );
    await taskNotifier.updateTask(updatedTask);

    // Trigger should NOT have incremented
    final triggerAfterCosmetic = container.read(schedulingTriggerProvider(listId));
    expect(triggerAfterCosmetic, 1);

    // Scheduling-affecting update: duration changes
    final schedulingTask = TaskItem(
      id: task.id,
      listId: task.listId,
      title: task.title,
      description: 'Updated Description',
      link: 'https://example.com',
      estimatedDurationMinutes: 120, // changed
      importance: task.importance,
      complexity: task.complexity,
      dueDate: task.dueDate,
    );
    await taskNotifier.updateTask(schedulingTask);

    // Trigger SHOULD increment
    final triggerAfterScheduling =
        container.read(schedulingTriggerProvider(listId));
    expect(triggerAfterScheduling, 2);
  });

  test('Cosmetic list edits do not bump scheduling trigger', () async {
    final repository = _FakeMemoryTaskRepository();
    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
      ],
    );
    addTearDown(container.dispose);

    const listId = 'list-1';
    final listsNotifier = container.read(taskListsProvider.notifier);
    await container.read(taskListsProvider.future);

    final initialTrigger = container.read(schedulingTriggerProvider(listId));
    expect(initialTrigger, 0);

    // Cosmetic list edit: change name only
    await listsNotifier.updateList(
      TaskList(id: listId, name: 'Renamed Work List'),
    );

    // Trigger should remain unchanged
    expect(container.read(schedulingTriggerProvider(listId)), 0);

    // Scheduling-relevant list edit: change work hours
    await listsNotifier.updateList(
      TaskList(
        id: listId,
        name: 'Renamed Work List',
        workStartMinutes: 480, // 8:00 AM
        workEndMinutes: 960, // 4:00 PM
      ),
    );

    // Trigger should increment
    expect(container.read(schedulingTriggerProvider(listId)), 1);
  });
}
