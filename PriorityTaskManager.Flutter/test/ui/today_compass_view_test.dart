import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/recurring_schedule_task.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/event_providers.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/ui/command_center/today_compass_view.dart';

class _FakeMemoryTaskRepository implements TaskRepository {
  final Map<String, List<TaskItem>> _tasks = {};
  final List<TaskList> _lists = [
    TaskList(
      id: 'list-1',
      name: 'Work',
      workStartMinutes: 9 * 60,
      workEndMinutes: 17 * 60,
    ),
  ];
  final Map<String, List<FixedEvent>> _events = {};

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
  Future<List<TaskItem>> getTasks(String listId) async => _tasks[listId] ?? [];

  @override
  Future<List<FixedEvent>> getEvents(String listId) async => _events[listId] ?? [];

  @override
  Future<List<FixedEvent>> getEventOccurrences(
    String listId,
    DateTime start,
    DateTime end,
  ) async => _events[listId] ?? [];

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
  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
    String description = '',
    String link = '',
    RecurrenceRule? recurrenceRule,
  }) async {
    final event = FixedEvent(
      id: 'event-${DateTime.now().microsecondsSinceEpoch}',
      listId: listId,
      title: title,
      startTime: startTime,
      endTime: endTime,
      description: description,
      link: link,
    );
    (_events[listId] ??= []).add(event);
    return event;
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
  testWidgets('TodayCompassView renders capacity bar, sections, and completes task', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository();
    final now = DateTime(2026, 10, 24, 10, 0); // 10:00 AM (morning)
    final today = DateTime(2026, 10, 24);

    // Seed task 1: High urgency
    await repository.addTask(
      listId: 'list-1',
      title: 'Math Homework Problem Set',
      estimatedDurationMinutes: 90,
      importance: 9,
      dueDate: DateTime(today.year, today.month, today.day, 17, 0),
    );

    // Seed task 2: Next up
    await repository.addTask(
      listId: 'list-1',
      title: 'History Essay Outline',
      estimatedDurationMinutes: 120,
      importance: 7,
      dueDate: DateTime(today.year, today.month, today.day + 1, 17, 0),
    );

    // Seed task 3: Quick win
    await repository.addTask(
      listId: 'list-1',
      title: 'Reply to internship inquiries',
      estimatedDurationMinutes: 30,
      importance: 5,
    );

    // Seed event
    await repository.addEvent(
      listId: 'list-1',
      title: 'Biology Lab Lecture',
      startTime: DateTime(today.year, today.month, today.day, 11, 30),
      endTime: DateTime(today.year, today.month, today.day, 12, 30),
    );

    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(now),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(
            body: TodayCompassView(listId: 'list-1'),
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Verify workload and capacity summary
    expect(find.textContaining("TODAY'S WORKLOAD:"), findsOneWidget);
    expect(find.textContaining('CAPACITY:'), findsOneWidget);

    // Verify sections
    expect(find.text('1. HIGHEST URGENCY (Least Slack)'), findsOneWidget);
    expect(find.text('Math Homework Problem Set'), findsOneWidget);

    expect(find.text('FIXED CALENDAR COMMITMENTS'), findsOneWidget);
    expect(find.text('Biology Lab Lecture'), findsOneWidget);

    expect(find.text('2. NEXT UP'), findsOneWidget);
    expect(find.text('History Essay Outline'), findsOneWidget);

    expect(find.text('3. LOW EFFORT / FLEXIBLE'), findsOneWidget);
    expect(find.text('Reply to internship inquiries'), findsOneWidget);

    // Complete a task using 1-click Done button
    await tester.tap(find.widgetWithText(FilledButton, 'Done').first);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 1100));

    // Undo snackbar appears
    expect(find.text('Undo'), findsOneWidget);
  });

  testWidgets('TodayCompassView renders evening accomplishments past work hours', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository();
    final evening = DateTime(2026, 10, 24, 18, 30); // 6:30 PM (past 5 PM work end)
    final today = DateTime(2026, 10, 24);

    // Seed completed task
    final completed = await repository.addTask(
      listId: 'list-1',
      title: 'Finished Project Proposal',
      estimatedDurationMinutes: 60,
    );
    await repository.setCompleted(completed.id, true);

    // Seed task due tonight
    await repository.addTask(
      listId: 'list-1',
      title: 'Calculus assignment due 11:59pm',
      estimatedDurationMinutes: 90,
      dueDate: DateTime(today.year, today.month, today.day, 23, 59),
    );

    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(evening),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(
            body: TodayCompassView(listId: 'list-1'),
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Evening wrap-up UI is visible
    expect(find.text('Workday Complete — Evening Wrap-Up'), findsOneWidget);
    expect(find.text("TODAY'S ACCOMPLISHMENTS:"), findsOneWidget);
    expect(find.textContaining('Completed 1 task(s)'), findsOneWidget);

    // Deadline attention for tonight's unfinished work
    expect(find.text('! DEADLINE ATTENTION (Due Tonight Before Midnight):'), findsOneWidget);
    expect(find.text('Calculus assignment due 11:59pm'), findsOneWidget);
    expect(find.text('Complete Tonight (+2h Window)'), findsOneWidget);
  });
}
