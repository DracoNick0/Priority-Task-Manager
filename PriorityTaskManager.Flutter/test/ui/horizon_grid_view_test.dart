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
import 'package:priority_task_manager/ui/command_center/horizon_grid_view.dart';

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

  int _nextTaskId = 1;
  int _nextEventId = 1;

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
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  testWidgets('HorizonGridView renders hourly ruler, navigation, and time blocks', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final repository = _FakeMemoryTaskRepository();
    // Monday Oct 19, 2026
    final now = DateTime(2026, 10, 19, 10, 0);

    // Seed task
    final task1 = await repository.addTask(
      listId: 'list-1',
      title: 'Architecture Review',
      estimatedDurationMinutes: 90,
      importance: 8,
    );

    // Seed event
    await repository.addEvent(
      listId: 'list-1',
      title: 'Sprint Planning',
      startTime: DateTime(2026, 10, 19, 10, 0),
      endTime: DateTime(2026, 10, 19, 11, 0),
    );

    // Seed scheduled tasks mock
    final mockSchedule = DailySchedule(
      leastSlackTask: 'Architecture Review',
      actualSlack: '2.0h',
      realisticSlack: '1.5h',
      todayTasks: [
        ScheduledTask(
          id: task1.id,
          title: task1.title,
          startTime: DateTime(2026, 10, 19, 11, 0),
          endTime: DateTime(2026, 10, 19, 12, 30),
          dueDate: task1.dueDate,
          chunkHours: 1.5,
        ),
      ],
      futureTasks: [],
      unscheduledTasks: [],
      unscheduledTaskIds: [],
    );

    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(now),
        ),
        scheduleProvider.overrideWith(
          (ref) => Future.value(mockSchedule),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(
            body: HorizonGridView(listId: 'list-1'),
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Navigation bar elements
    expect(find.textContaining('HORIZON — Week of'), findsOneWidget);
    expect(find.text('< Prev Week'), findsOneWidget);
    expect(find.text('Today'), findsOneWidget);
    expect(find.text('Next Week >'), findsOneWidget);

    // Hourly Ruler elements
    expect(find.text('09:00 AM'), findsOneWidget);
    expect(find.text('10:00 AM'), findsOneWidget);
    expect(find.text('11:00 AM'), findsOneWidget);
    expect(find.text('12:00 PM'), findsOneWidget);

    // Day column header
    expect(find.textContaining('MON'), findsOneWidget);

    // Fixed event rendered with diagonal hatch
    expect(find.text('Sprint Planning'), findsOneWidget);
    expect(find.byType(CustomPaint), findsWidgets);

    // Scheduled task block
    expect(find.text('Architecture Review'), findsOneWidget);
    expect(find.text('90m'), findsOneWidget);
  });

  testWidgets('Unscheduled side tray diagnoses unplaced tasks', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final repository = _FakeMemoryTaskRepository();
    final now = DateTime(2026, 10, 19, 10, 0);

    // Prerequisite task
    final prereq = await repository.addTask(
      listId: 'list-1',
      title: 'Database Migration',
      estimatedDurationMinutes: 60,
    );

    // Blocked task
    final blockedTask = await repository.addTask(
      listId: 'list-1',
      title: 'Deploy to Production',
      estimatedDurationMinutes: 60,
      dependencies: [prereq.id],
    );

    // Mock schedule with unscheduled task
    final mockSchedule = DailySchedule(
      leastSlackTask: 'None',
      actualSlack: '0.0h',
      realisticSlack: '0.0h',
      todayTasks: [],
      futureTasks: [],
      unscheduledTasks: [blockedTask],
      unscheduledTaskIds: [blockedTask.id],
    );

    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(now),
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
        child: const MaterialApp(
          home: Scaffold(
            body: HorizonGridView(listId: 'list-1'),
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Verify Unscheduled & Risk side tray
    expect(find.text('UNSCHEDULED & RISK'), findsOneWidget);
    expect(find.text('BLOCKED BY DEPENDENCY'), findsOneWidget);
    expect(find.text('Deploy to Production'), findsOneWidget);
    expect(find.textContaining('Prerequisite "Database Migration" must complete first'), findsOneWidget);
    expect(find.text('Extend Hours'), findsOneWidget);
    expect(find.text('Defer'), findsOneWidget);
  });
}
