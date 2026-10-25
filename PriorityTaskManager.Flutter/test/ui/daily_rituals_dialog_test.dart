import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/recurring_schedule_task.dart';
import 'package:priority_task_manager/models/schedule_models.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/event_providers.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/view_provider.dart';
import 'package:priority_task_manager/ui/command_center/daily_rituals_dialog.dart';

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

  int _nextTaskId = 1;
  int _nextEventId = 1;

  @override
  Future<List<TaskList>> getLists() async => _lists;

  @override
  Future<TaskList> createList({required String name, String? description}) async {
    final list = TaskList(id: 'list-${_lists.length + 1}', name: name);
    _lists.add(list);
    return list;
  }

  @override
  Future<void> updateList(TaskList list) async {}

  @override
  Future<void> deleteList(String listId) async {}

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
      id: 'task-${_nextTaskId++}',
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
      dependencies: dependencies,
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
      id: 'event-${_nextEventId++}',
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
  testWidgets('Morning Kickoff dialog renders workload snapshot and least slack task', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository();
    final morning = DateTime(2026, 10, 24, 9, 15); // 9:15 AM
    final today = DateTime(2026, 10, 24);

    final task1 = await repository.addTask(
      listId: 'list-1',
      title: 'Draft Architecture Proposal',
      estimatedDurationMinutes: 120,
      importance: 9,
      dueDate: DateTime(today.year, today.month, today.day, 17, 0),
    );

    await repository.addEvent(
      listId: 'list-1',
      title: 'Sprint Planning',
      startTime: DateTime(today.year, today.month, today.day, 10, 0),
      endTime: DateTime(today.year, today.month, today.day, 11, 30),
    );

    final mockSchedule = DailySchedule(
      leastSlackTask: 'Draft Architecture Proposal',
      actualSlack: '1.0h',
      realisticSlack: '0.5h',
      todayTasks: [
        ScheduledTask(
          id: task1.id,
          title: task1.title,
          startTime: DateTime(today.year, today.month, today.day, 13, 0),
          endTime: DateTime(today.year, today.month, today.day, 15, 0),
          dueDate: task1.dueDate,
          chunkHours: 2.0,
        ),
      ],
      futureTasks: [],
    );

    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(morning),
        ),
        scheduleProvider.overrideWith(
          (ref) => Future.value(mockSchedule),
        ),
      ],
    );
    addTearDown(container.dispose);
    await container.read(tasksProvider('list-1').future);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => showDailyRitualDialog(
                  context,
                  type: DailyRitualType.morningKickoff,
                  listId: 'list-1',
                ),
                child: const Text('Open Morning'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    // Open morning ritual
    await tester.tap(find.text('Open Morning'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    // Verify dialog content
    expect(find.text('Good Morning! Here is your plan for today'), findsOneWidget);
    expect(find.text("TODAY'S WORKLOAD SNAPSHOT:"), findsOneWidget);
    expect(find.textContaining('Scheduled Work: 2.0 hours'), findsOneWidget);
    expect(find.textContaining('Fixed Events: 1.5 hours (1 meeting(s))'), findsOneWidget);

    // Verify least slack attention
    expect(find.text('LEAST SLACK ATTENTION:'), findsOneWidget);
    expect(find.textContaining('Draft Architecture Proposal'), findsOneWidget);

    // Verify action buttons
    expect(find.text('Start My Day'), findsOneWidget);
    expect(find.text('Adjust Hours'), findsOneWidget);
    expect(find.text('Defer Low Priority'), findsOneWidget);

    // Lock in day
    await tester.tap(find.text('Start My Day'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.byType(DailyRitualDialog), findsNothing);
  });

  testWidgets('Evening Wind-Down handles midnight deadlines and flexible items', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final repository = _FakeMemoryTaskRepository();
    final evening = DateTime(2026, 10, 24, 18, 0); // 6:00 PM
    final today = DateTime(2026, 10, 24);

    // Seed completed task
    final completed = await repository.addTask(
      listId: 'list-1',
      title: 'Morning Research Report',
      estimatedDurationMinutes: 90,
    );
    await repository.setCompleted(completed.id, true);

    // Seed task due tonight before midnight
    await repository.addTask(
      listId: 'list-1',
      title: 'Problem Set #4 due 11:59pm',
      estimatedDurationMinutes: 60,
      dueDate: DateTime(today.year, today.month, today.day, 23, 59),
    );

    // Seed flexible task due later
    await repository.addTask(
      listId: 'list-1',
      title: 'Next Week Preparation',
      estimatedDurationMinutes: 45,
      dueDate: DateTime(today.year, today.month, today.day + 3, 17, 0),
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
    await container.read(tasksProvider('list-1').future);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => showDailyRitualDialog(
                  context,
                  type: DailyRitualType.eveningWindDown,
                  listId: 'list-1',
                ),
                child: const Text('Open Evening'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    // Open evening ritual
    await tester.tap(find.text('Open Evening'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    // Accomplishments summary
    expect(find.text('Workday Complete — Evening Wrap-Up'), findsOneWidget);
    expect(find.text("TODAY'S ACCOMPLISHMENTS:"), findsOneWidget);
    expect(find.textContaining('Completed 1 task(s)'), findsOneWidget);

    // Midnight deadline categorization
    expect(find.text('! DEADLINE ATTENTION (Due Tonight Before Midnight):'), findsOneWidget);
    expect(find.text('Problem Set #4 due 11:59pm'), findsOneWidget);
    expect(find.text('Complete Tonight (+2h Window)'), findsOneWidget);
    expect(find.text('Mark Done'), findsOneWidget);
    expect(find.text('Postpone (Late)'), findsOneWidget);

    // Flexible items categorization
    expect(find.textContaining('FLEXIBLE WORK'), findsOneWidget);
    expect(find.text('Push to Tomorrow Morning'), findsOneWidget);
    expect(find.text('Move to Backlog'), findsOneWidget);

    // Wrap up
    await tester.tap(find.text('Save & Wrap Up'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.byType(DailyRitualDialog), findsNothing);
  });
}
