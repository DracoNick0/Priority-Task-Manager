import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/recurring_schedule_task.dart';
import 'package:priority_task_manager/models/schedule_models.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/models/user_profile.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/event_providers.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/view_provider.dart';
import 'package:priority_task_manager/ui/command_center/horizon_event_style.dart';
import 'package:priority_task_manager/ui/command_center/horizon_grid_view.dart';

class _FakeMemoryTaskRepository implements TaskRepository {
  _FakeMemoryTaskRepository({
    int? workStartMinutes = 9 * 60,
    int? workEndMinutes = 17 * 60,
    int profileWorkStartMinutes = 9 * 60,
    int profileWorkEndMinutes = 17 * 60,
    List<int>? listWorkDays,
    List<int> profileWorkDays = const [1, 2, 3, 4, 5],
  }) : _profile = UserProfile(
         workStartMinutes: profileWorkStartMinutes,
         workEndMinutes: profileWorkEndMinutes,
         workDays: profileWorkDays,
       ),
       _lists = [
         TaskList(
           id: 'list-1',
           name: 'Work',
           workStartMinutes: workStartMinutes,
           workEndMinutes: workEndMinutes,
           workDays: listWorkDays,
         ),
       ];

  final Map<String, List<TaskItem>> _tasks = {};
  final UserProfile _profile;
  final List<TaskList> _lists;
  final Map<String, List<FixedEvent>> _events = {};

  @override
  Future<List<TaskList>> getLists() async => _lists;

  @override
  Future<UserProfile> getProfile() async => _profile;

  @override
  Future<TaskList> createList({
    required String name,
    String? description,
  }) async {
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
  Future<List<FixedEvent>> getEvents(String listId) async =>
      _events[listId] ?? [];

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
    final id = 'event-${_nextEventId++}';
    final event = FixedEvent(
      id: id,
      listId: listId,
      title: title,
      startTime: startTime,
      endTime: endTime,
      description: description,
      link: link,
      recurrenceRule: recurrenceRule?.toJson(),
      seriesId: recurrenceRule == null ? null : id,
    );
    (_events[listId] ??= []).add(event);
    return event;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  testWidgets(
    'HorizonGridView renders hourly ruler, navigation, and time blocks',
    (WidgetTester tester) async {
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
      final shortTask = await repository.addTask(
        listId: 'list-1',
        title: 'Review the architecture and update the implementation notes',
        estimatedDurationMinutes: 25,
      );
      final splitTask = await repository.addTask(
        listId: 'list-1',
        title: 'Implement and verify compact rendering for split task blocks',
        estimatedDurationMinutes: 40,
      );
      final longTask = await repository.addTask(
        listId: 'list-1',
        title: 'Long duration task',
        estimatedDurationMinutes: 449,
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
          ScheduledTask(
            id: shortTask.id,
            title: shortTask.title,
            startTime: DateTime(2026, 10, 19, 13, 0),
            endTime: DateTime(2026, 10, 19, 13, 25),
            dueDate: shortTask.dueDate,
            chunkHours: 25 / 60,
          ),
          ScheduledTask(
            id: splitTask.id,
            title: splitTask.title,
            startTime: DateTime(2026, 10, 19, 14, 0),
            endTime: DateTime(2026, 10, 19, 14, 20),
            dueDate: splitTask.dueDate,
            chunkHours: 20 / 60,
          ),
        ],
        futureTasks: [
          ScheduledTask(
            id: splitTask.id,
            title: splitTask.title,
            startTime: DateTime(2026, 10, 20, 14, 0),
            endTime: DateTime(2026, 10, 20, 14, 20),
            dueDate: splitTask.dueDate,
            chunkHours: 20 / 60,
          ),
          ScheduledTask(
            id: longTask.id,
            title: longTask.title,
            startTime: DateTime(2026, 10, 21, 9, 0),
            endTime: DateTime(2026, 10, 21, 16, 29),
            dueDate: longTask.dueDate,
            chunkHours: 449 / 60,
          ),
        ],
        unscheduledTasks: [],
        unscheduledTaskIds: [],
      );

      final container = ProviderContainer(
        overrides: [
          taskRepositoryProvider.overrideWith((ref) => repository),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          scheduleProvider.overrideWith((ref) => Future.value(mockSchedule)),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(tester.takeException(), isNull);

      // Navigation bar elements
      expect(find.textContaining('HORIZON — Week of'), findsOneWidget);
      expect(find.text('< Prev Week'), findsOneWidget);
      expect(find.text('Today'), findsOneWidget);
      expect(find.text('Next Week >'), findsOneWidget);

      // Hourly Ruler elements
      expect(find.text('12:00 AM'), findsOneWidget);
      expect(find.text('09:00 AM'), findsOneWidget);
      expect(find.text('10:00 AM'), findsOneWidget);
      expect(find.text('11:00 AM'), findsOneWidget);
      expect(find.text('12:00 PM'), findsOneWidget);
      expect(find.text('11:00 PM'), findsOneWidget);

      // Day column header
      expect(find.textContaining('MON'), findsOneWidget);

      // Fixed event rendered with diagonal hatch
      expect(find.text('Sprint Planning'), findsOneWidget);
      expect(find.byType(CustomPaint), findsWidgets);
      expect(find.textContaining('fixed commitment'), findsNothing);

      final eventHeader = find
          .ancestor(
            of: find.text('Sprint Planning'),
            matching: find.byType(Row),
          )
          .first;
      final eventDuration = find.descendant(
        of: eventHeader,
        matching: find.text('60m'),
      );
      expect(eventDuration, findsOneWidget);
      expect(
        tester.getTopLeft(find.text('Sprint Planning')).dx,
        lessThan(tester.getTopLeft(eventDuration).dx),
      );
      expect(
        tester.getCenter(find.text('Sprint Planning')).dy,
        closeTo(tester.getCenter(eventDuration).dy, 1),
      );

      // Scheduled task block
      expect(find.text('Architecture Review'), findsOneWidget);
      expect(find.text('90m'), findsOneWidget);
      expect(find.text(shortTask.title), findsOneWidget);
      expect(find.text('P1/2'), findsOneWidget);
      expect(find.text('P2/2'), findsOneWidget);
      expect(find.byTooltip('Part 1 of 2'), findsOneWidget);

      final longTaskBlock = find.ancestor(
        of: find.text(longTask.title),
        matching: find.byType(Positioned),
      );
      expect(longTaskBlock, findsOneWidget);
      final longTaskBlockHeight = tester
          .widget<Positioned>(longTaskBlock)
          .height;
      expect(longTaskBlockHeight, greaterThan(300.0));
      expect(longTaskBlockHeight, closeTo(449 / 60 * 64, 0.1));
    },
  );

  testWidgets(
    'Horizon shows seven days, expands wide columns, and omits non-workday capacity',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository(
        workStartMinutes: null,
        workEndMinutes: null,
        profileWorkStartMinutes: 8 * 60 + 30,
        profileWorkEndMinutes: 17 * 60 + 30,
        listWorkDays: const [DateTime.monday, DateTime.wednesday],
      );
      final now = DateTime(2026, 10, 19, 10);
      await repository.addEvent(
        listId: 'list-1',
        title: 'Monday meeting',
        startTime: DateTime(2026, 10, 19, 10),
        endTime: DateTime(2026, 10, 19, 11),
      );
      await repository.addEvent(
        listId: 'list-1',
        title: 'Saturday event',
        startTime: DateTime(2026, 10, 24, 14),
        endTime: DateTime(2026, 10, 24, 15),
      );

      final container = ProviderContainer(
        overrides: [
          taskRepositoryProvider.overrideWith((ref) => repository),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          scheduleProvider.overrideWith(
            (ref) => Future.value(DailySchedule.empty()),
          ),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      for (var dayOffset = 0; dayOffset < DateTime.sunday; dayOffset++) {
        final day = DateTime(2026, 10, 19 + dayOffset);
        expect(
          find.byKey(
            ValueKey('horizon-day-header-${day.year}-${day.month}-${day.day}'),
          ),
          findsOneWidget,
        );
      }

      final headerScroll = find.byKey(
        const ValueKey('horizon-header-horizontal-scroll'),
      );
      final headerViewportWidth = tester.getSize(headerScroll).width;
      final mondayHeader = find.byKey(
        const ValueKey('horizon-day-header-2026-10-19'),
      );
      final sundayHeader = find.byKey(
        const ValueKey('horizon-day-header-2026-10-25'),
      );
      final dayColumnWidth = tester.getSize(mondayHeader).width;
      expect(dayColumnWidth, greaterThan(160));
      expect(
        dayColumnWidth,
        closeTo((headerViewportWidth - 64 - 8) / DateTime.sunday, 0.1),
      );
      expect(
        tester.getTopRight(sundayHeader).dx,
        closeTo(tester.getTopRight(headerScroll).dx - 1, 0.1),
      );

      final mondayWorkBand = find.byKey(
        const ValueKey('horizon-working-hours-2026-10-19'),
      );
      final mondayWorkBandPosition = tester.widget<Positioned>(
        find
            .ancestor(of: mondayWorkBand, matching: find.byType(Positioned))
            .first,
      );
      expect(mondayWorkBandPosition.top, closeTo(8.5 * 64, 0.1));
      expect(mondayWorkBandPosition.height, closeTo(9 * 64, 0.1));
      expect(
        tester.widget<ColoredBox>(mondayWorkBand).color,
        Theme.of(
          tester.element(mondayWorkBand),
        ).colorScheme.primaryContainer.withAlpha(24),
      );
      expect(
        find.byKey(const ValueKey('horizon-working-hours-2026-10-20')),
        findsNothing,
      );
      expect(
        find.byKey(const ValueKey('horizon-working-hours-2026-10-21')),
        findsOneWidget,
      );
      final saturdayHeader = find.byKey(
        const ValueKey('horizon-day-header-2026-10-24'),
      );
      expect(find.text('Monday meeting'), findsOneWidget);
      expect(find.text('Saturday event'), findsOneWidget);
      expect(
        find.byKey(const ValueKey('horizon-working-hours-2026-10-24')),
        findsNothing,
      );
      expect(
        find.byKey(const ValueKey('horizon-capacity-meter-2026-10-24')),
        findsNothing,
      );
      expect(
        find.descendant(
          of: saturdayHeader,
          matching: find.textContaining('h / '),
        ),
        findsNothing,
      );
      expect(
        find.byKey(const ValueKey('horizon-capacity-meter-2026-10-19')),
        findsOneWidget,
      );
      expect(
        find.byKey(const ValueKey('horizon-capacity-meter-2026-10-20')),
        findsNothing,
      );
      expect(
        find.byKey(const ValueKey('horizon-capacity-meter-2026-10-21')),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Horizon shows the full day with pinned, synchronized headers and work-start scroll',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(700, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository(
        workStartMinutes: 20 * 60 + 30,
        workEndMinutes: 22 * 60 + 30,
      );
      final now = DateTime(2026, 10, 19, 23, 30);
      await repository.addEvent(
        listId: 'list-1',
        title: 'Late event',
        startTime: DateTime(2026, 10, 19, 23),
        endTime: DateTime(2026, 10, 19, 23, 30),
      );

      final container = ProviderContainer(
        overrides: [
          taskRepositoryProvider.overrideWith((ref) => repository),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          scheduleProvider.overrideWith(
            (ref) => Future.value(DailySchedule.empty()),
          ),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('12:00 AM'), findsOneWidget);
      expect(find.text('11:00 PM'), findsOneWidget);

      const verticalScrollKey = ValueKey('horizon-timeline-vertical-scroll');
      const headerScrollKey = ValueKey('horizon-header-horizontal-scroll');
      const timelineScrollKey = ValueKey('horizon-timeline-horizontal-scroll');
      final verticalScroll = tester.widget<SingleChildScrollView>(
        find.byKey(verticalScrollKey),
      );
      expect(verticalScroll.controller!.offset, closeTo(20.5 * 64.0, 0.1));

      const mondayHeaderKey = ValueKey('horizon-day-header-2026-10-19');
      const mondayTimelineKey = ValueKey('horizon-timeline-2026-10-19');
      final mondayHeader = find.byKey(mondayHeaderKey);
      final mondayTimeline = find.byKey(mondayTimelineKey);
      expect(tester.getSize(mondayHeader).width, closeTo(160, 0.1));
      final headerTop = tester.getTopLeft(mondayHeader).dy;

      final lateEventBlock = find
          .ancestor(
            of: find.text('Late event'),
            matching: find.byType(Positioned),
          )
          .first;
      expect(find.text('Late event'), findsOneWidget);
      expect(
        tester.widget<Positioned>(lateEventBlock).top!,
        closeTo(23 * 64.0, 0.1),
      );

      final currentTimeIndicator = find.byKey(
        const ValueKey('horizon-current-time-indicator'),
      );
      expect(currentTimeIndicator, findsOneWidget);
      expect(
        tester.getTopLeft(currentTimeIndicator).dy -
            tester.getTopLeft(mondayTimeline).dy,
        closeTo(23.5 * 64.0, 0.1),
      );

      await tester.drag(find.byKey(verticalScrollKey), const Offset(0, 100));
      await tester.pumpAndSettle();
      expect(tester.getTopLeft(mondayHeader).dy, closeTo(headerTop, 0.1));

      final headerHorizontalScroll = tester.widget<SingleChildScrollView>(
        find.byKey(headerScrollKey),
      );
      final timelineHorizontalScroll = tester.widget<SingleChildScrollView>(
        find.byKey(timelineScrollKey),
      );
      final timelineViewport = tester.getRect(find.byKey(verticalScrollKey));
      await tester.dragFrom(timelineViewport.center, const Offset(-240, 0));
      await tester.pumpAndSettle();

      expect(timelineHorizontalScroll.controller!.offset, greaterThan(0));
      expect(
        headerHorizontalScroll.controller!.offset,
        closeTo(timelineHorizontalScroll.controller!.offset, 0.1),
      );
      expect(
        tester.getTopLeft(mondayHeader).dx,
        closeTo(tester.getTopLeft(mondayTimeline).dx, 0.1),
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'HorizonGridView expands recurring events across the displayed week',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository();
      final now = DateTime(2026, 10, 19, 10);
      final recurringEvent = await repository.addEvent(
        listId: 'list-1',
        title: 'Database Class',
        startTime: DateTime(2026, 10, 19, 9),
        endTime: DateTime(2026, 10, 19, 10),
        recurrenceRule: WeeklyRecurrenceRule(
          seriesStartDate: DateTime(2026, 10, 19, 9),
          endCondition: NeverEndCondition(),
          daysOfWeek: const [1, 3, 5],
        ),
      );

      List<FixedEvent> occurrencesForWeek(DateTime monday, String title) => [
        for (final dayOffset in const [0, 2, 4])
          FixedEvent(
            id: '${recurringEvent.id}_${monday.add(Duration(days: dayOffset))}',
            listId: 'list-1',
            title: title,
            startTime: DateTime(
              monday.year,
              monday.month,
              monday.day + dayOffset,
              9,
            ),
            endTime: DateTime(
              monday.year,
              monday.month,
              monday.day + dayOffset,
              10,
            ),
            seriesId: recurringEvent.id,
            originalOccurrenceDate: DateTime(
              monday.year,
              monday.month,
              monday.day + dayOffset,
            ),
          ),
      ];

      final container = ProviderContainer(
        overrides: [
          taskRepositoryProvider.overrideWith((ref) => repository),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          eventOccurrencesProvider((
            'list-1',
            DateTime(2026, 10, 19),
          )).overrideWith(
            (ref) => Future.value(
              occurrencesForWeek(DateTime(2026, 10, 19), 'Database Class'),
            ),
          ),
          eventOccurrencesProvider((
            'list-1',
            DateTime(2026, 10, 26),
          )).overrideWith(
            (ref) => Future.value(
              occurrencesForWeek(DateTime(2026, 10, 26), 'Next Week Class'),
            ),
          ),
          scheduleProvider.overrideWith(
            (ref) => Future.value(DailySchedule.empty()),
          ),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('Unscheduled & Risk'), findsOneWidget);
      final neutralButtonFinder = find.ancestor(
        of: find.text('Unscheduled & Risk'),
        matching: find.byType(TextButton),
      );
      final neutralButton = tester.widget<TextButton>(neutralButtonFinder);
      expect(neutralButton.style?.foregroundColor, isNull);

      expect(find.text('Database Class'), findsNWidgets(3));

      await tester.tap(find.text('Next Week >'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('Database Class'), findsNothing);
      expect(find.text('Next Week Class'), findsNWidgets(3));
    },
  );

  testWidgets('Unscheduled bottom tray diagnoses and resizes at all widths', (
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
        engineClockProvider.overrideWith((ref) => Stream.value(now)),
        scheduleProvider.overrideWith((ref) => Future.value(mockSchedule)),
      ],
    );
    addTearDown(container.dispose);
    await container.read(tasksProvider('list-1').future);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(body: HorizonGridView(listId: 'list-1')),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // The tray starts collapsed and signals the number of tray tasks.
    expect(find.byKey(const ValueKey('unscheduled-tray')), findsNothing);
    expect(find.text('UNSCHEDULED & RISK'), findsNothing);
    expect(find.text('Unscheduled & Risk (1)'), findsOneWidget);
    final warningButtonFinder = find.ancestor(
      of: find.text('Unscheduled & Risk (1)'),
      matching: find.byType(TextButton),
    );
    final warningButton = tester.widget<TextButton>(warningButtonFinder);
    expect(
      warningButton.style?.foregroundColor?.resolve({}),
      Theme.of(tester.element(warningButtonFinder)).colorScheme.error,
    );

    await tester.tap(find.text('Unscheduled & Risk (1)'));
    await tester.pumpAndSettle();
    final trayFinder = find.byKey(const ValueKey('unscheduled-tray'));
    expect(trayFinder, findsOneWidget);
    expect(tester.getSize(trayFinder).width, 1280);
    expect(find.text('UNSCHEDULED & RISK'), findsOneWidget);
    expect(find.text('BLOCKED BY DEPENDENCY'), findsOneWidget);
    expect(find.text('Deploy to Production'), findsOneWidget);
    expect(
      find.textContaining(
        'Prerequisite "Database Migration" must complete first',
      ),
      findsOneWidget,
    );
    expect(find.text('Extend Hours'), findsOneWidget);
    expect(find.text('Defer'), findsOneWidget);

    final wideInitialTrayHeight = tester.getSize(trayFinder).height;
    await tester.drag(
      find.byKey(const ValueKey('unscheduled-tray-divider')),
      const Offset(0, -80),
    );
    await tester.pumpAndSettle();
    expect(
      tester.getSize(trayFinder).height,
      greaterThan(wideInitialTrayHeight),
    );
    expect(tester.getSize(trayFinder).width, 1280);
    expect(tester.takeException(), isNull);

    await tester.tap(find.text('Unscheduled & Risk (1)'));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('unscheduled-tray')), findsNothing);
    expect(find.text('UNSCHEDULED & RISK'), findsNothing);

    tester.view.physicalSize = const Size(800, 800);
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.text('UNSCHEDULED & RISK'), findsNothing);
    expect(find.text('Unscheduled & Risk (1)'), findsOneWidget);

    tester.view.physicalSize = const Size(480, 800);
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('UNSCHEDULED & RISK'), findsNothing);
    expect(find.text('Unscheduled & Risk (1)'), findsOneWidget);

    await tester.tap(find.text('Unscheduled & Risk (1)'));
    await tester.pumpAndSettle();

    expect(find.text('UNSCHEDULED & RISK'), findsOneWidget);
    expect(find.text('BLOCKED BY DEPENDENCY'), findsOneWidget);
    expect(find.text('Deploy to Production'), findsOneWidget);
    expect(find.text('Extend Hours'), findsOneWidget);
    expect(find.text('Defer'), findsOneWidget);

    final initialTraySize = tester.getSize(trayFinder);
    expect(initialTraySize.width, 480);

    await tester.drag(
      find.byKey(const ValueKey('unscheduled-tray-divider')),
      const Offset(0, -80),
    );
    await tester.pumpAndSettle();
    expect(
      tester.getSize(trayFinder).height,
      greaterThan(initialTraySize.height),
    );
    expect(tester.getSize(trayFinder).width, 480);
    expect(tester.takeException(), isNull);

    await tester.tap(find.byTooltip('Close Unscheduled & Risk'));
    await tester.pumpAndSettle();
    expect(find.text('UNSCHEDULED & RISK'), findsNothing);
    expect(find.text('Unscheduled & Risk (1)'), findsOneWidget);
  });

  testWidgets(
    'Horizon capacity uses remaining hours today and full capacity on future days',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository();
      final now = DateTime(2026, 10, 19, 12, 30);
      final pastTask = await repository.addTask(
        listId: 'list-1',
        title: 'Earlier task',
        estimatedDurationMinutes: 60,
      );
      final currentTask = await repository.addTask(
        listId: 'list-1',
        title: 'Current task',
        estimatedDurationMinutes: 60,
      );
      final futureTask = await repository.addTask(
        listId: 'list-1',
        title: 'Later task',
        estimatedDurationMinutes: 120,
      );
      final nextDayTask = await repository.addTask(
        listId: 'list-1',
        title: 'Tomorrow task',
        estimatedDurationMinutes: 480,
      );
      await repository.addEvent(
        listId: 'list-1',
        title: 'Earlier meeting',
        startTime: DateTime(2026, 10, 19, 10),
        endTime: DateTime(2026, 10, 19, 11),
      );
      await repository.addEvent(
        listId: 'list-1',
        title: 'Upcoming meeting',
        startTime: DateTime(2026, 10, 19, 13, 30),
        endTime: DateTime(2026, 10, 19, 14),
      );

      final schedule = DailySchedule(
        leastSlackTask: 'Current task',
        actualSlack: '0.0h',
        realisticSlack: '0.0h',
        todayTasks: [
          ScheduledTask(
            id: pastTask.id,
            title: pastTask.title,
            startTime: DateTime(2026, 10, 19, 9),
            endTime: DateTime(2026, 10, 19, 10),
            dueDate: null,
            chunkHours: 1,
          ),
          ScheduledTask(
            id: currentTask.id,
            title: currentTask.title,
            startTime: DateTime(2026, 10, 19, 12),
            endTime: DateTime(2026, 10, 19, 13),
            dueDate: null,
            chunkHours: 1,
          ),
          ScheduledTask(
            id: futureTask.id,
            title: futureTask.title,
            startTime: DateTime(2026, 10, 19, 14),
            endTime: DateTime(2026, 10, 19, 16),
            dueDate: null,
            chunkHours: 2,
          ),
        ],
        futureTasks: [
          ScheduledTask(
            id: nextDayTask.id,
            title: nextDayTask.title,
            startTime: DateTime(2026, 10, 20, 9),
            endTime: DateTime(2026, 10, 20, 17),
            dueDate: null,
            chunkHours: 8,
          ),
        ],
        unscheduledTasks: [],
        unscheduledTaskIds: [],
      );
      final container = ProviderContainer(
        overrides: [
          taskRepositoryProvider.overrideWith((ref) => repository),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          scheduleProvider.overrideWith((ref) => Future.value(schedule)),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('3.0h / 4.5h'), findsOneWidget);
      expect(find.text('67%'), findsOneWidget);
      expect(find.text('8.0h / 8.0h'), findsOneWidget);
      expect(find.text('100%'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Horizon capacity clips and merges event time within work hours',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository();
      final now = DateTime(2026, 10, 19, 8);
      final task = await repository.addTask(
        listId: 'list-1',
        title: 'Scheduled work',
        estimatedDurationMinutes: 120,
      );
      await repository.addEvent(
        listId: 'list-1',
        title: 'Before-hours event',
        startTime: DateTime(2026, 10, 19, 7),
        endTime: DateTime(2026, 10, 19, 10),
      );
      await repository.addEvent(
        listId: 'list-1',
        title: 'Overlapping work-hours event',
        startTime: DateTime(2026, 10, 19, 9, 30),
        endTime: DateTime(2026, 10, 19, 10, 30),
      );
      await repository.addEvent(
        listId: 'list-1',
        title: 'After-hours event',
        startTime: DateTime(2026, 10, 19, 17),
        endTime: DateTime(2026, 10, 19, 19),
      );

      final schedule = DailySchedule(
        leastSlackTask: task.title,
        actualSlack: '0.0h',
        realisticSlack: '0.0h',
        todayTasks: [
          ScheduledTask(
            id: task.id,
            title: task.title,
            startTime: DateTime(2026, 10, 19, 12, 30),
            endTime: DateTime(2026, 10, 19, 14, 30),
            dueDate: null,
            chunkHours: 2,
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
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          scheduleProvider.overrideWith((ref) => Future.value(schedule)),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('3.5h / 8.0h'), findsOneWidget);
      expect(find.text('44%'), findsOneWidget);
      expect(find.text('Before-hours event'), findsOneWidget);
      expect(find.text('After-hours event'), findsOneWidget);

      final beforeHoursBlock = find
          .ancestor(
            of: find.text('Before-hours event'),
            matching: find.byType(Positioned),
          )
          .first;
      final afterHoursBlock = find
          .ancestor(
            of: find.text('After-hours event'),
            matching: find.byType(Positioned),
          )
          .first;
      expect(
        tester.widget<Positioned>(beforeHoursBlock).top!,
        closeTo(7 * 64.0, 0.1),
      );
      expect(
        tester.widget<Positioned>(afterHoursBlock).top!,
        closeTo(17 * 64.0, 0.1),
      );

      final meterFinder = find.byKey(
        const ValueKey('horizon-capacity-meter-2026-10-19'),
      );
      final eventSegmentFinder = find.descendant(
        of: meterFinder,
        matching: find.byKey(const ValueKey('capacity-meter-events')),
      );
      final taskSegmentFinder = find.descendant(
        of: meterFinder,
        matching: find.byKey(const ValueKey('capacity-meter-tasks')),
      );
      final freeSegmentFinder = find.descendant(
        of: meterFinder,
        matching: find.byKey(const ValueKey('capacity-meter-free')),
      );
      final meterWidth = tester.getSize(meterFinder).width;
      final meterHeight = tester.getSize(meterFinder).height;

      expect(meterWidth, greaterThan(100));
      expect(
        tester.getSize(eventSegmentFinder).width,
        closeTo(meterWidth * (1.5 / 8), 0.5),
      );
      expect(tester.getSize(eventSegmentFinder).height, meterHeight);
      expect(
        tester.getSize(taskSegmentFinder).width,
        closeTo(meterWidth * (2 / 8), 0.5),
      );
      expect(tester.getSize(taskSegmentFinder).height, meterHeight);
      expect(
        tester.getSize(freeSegmentFinder).width,
        closeTo(meterWidth * (4.5 / 8), 0.5),
      );
      expect(tester.getSize(freeSegmentFinder).height, meterHeight);
      final eventColor = tester.widget<ColoredBox>(eventSegmentFinder).color;
      final taskColor = tester.widget<ColoredBox>(taskSegmentFinder).color;
      expect(eventColor, horizonEventBlockColor);
      final eventHatchFinder = find.descendant(
        of: meterFinder,
        matching: find.byKey(const ValueKey('capacity-meter-events-hatch')),
      );
      final eventHatch = tester
          .widget<CustomPaint>(eventHatchFinder)
          .foregroundPainter;
      expect(eventHatch, isA<DiagonalHatchPainter>());
      final hatchPainter = eventHatch! as DiagonalHatchPainter;
      expect(hatchPainter.lineColor, horizonEventHatchLineColor);
      expect(hatchPainter.spacing, horizonEventHatchSpacing);
      expect(taskColor, Colors.green);
      expect(eventColor, isNot(taskColor));
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Horizon shows zero remaining capacity after work hours without a percent',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository();
      final now = DateTime(2026, 10, 19, 18);
      final pastTask = await repository.addTask(
        listId: 'list-1',
        title: 'Completed work window',
        estimatedDurationMinutes: 60,
      );
      final schedule = DailySchedule(
        leastSlackTask: 'None',
        actualSlack: '0.0h',
        realisticSlack: '0.0h',
        todayTasks: [
          ScheduledTask(
            id: pastTask.id,
            title: pastTask.title,
            startTime: DateTime(2026, 10, 19, 9),
            endTime: DateTime(2026, 10, 19, 10),
            dueDate: null,
            chunkHours: 1,
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
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          scheduleProvider.overrideWith((ref) => Future.value(schedule)),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('0h left'), findsOneWidget);
      expect(find.text('0.0h remaining'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Horizon positions and updates the current-time indicator across the full day',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository();
      final now = DateTime(2026, 10, 26, 10, 30);
      await repository.addEvent(
        listId: 'list-1',
        title: 'Tomorrow Event',
        startTime: DateTime(2026, 10, 27, 11),
        endTime: DateTime(2026, 10, 27, 12),
      );

      final clock = StreamController<DateTime>()..add(now);
      addTearDown(clock.close);
      final container = ProviderContainer(
        overrides: [
          taskRepositoryProvider.overrideWith((ref) => repository),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => clock.stream),
          scheduleProvider.overrideWith(
            (ref) => Future.value(DailySchedule.empty()),
          ),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      final indicator = find.byKey(
        const ValueKey('horizon-current-time-indicator'),
      );
      final todayTimeline = find.byKey(
        const ValueKey('horizon-timeline-2026-10-26'),
      );
      expect(indicator, findsOneWidget);

      double indicatorTop() =>
          tester.getTopLeft(indicator).dy - tester.getTopLeft(todayTimeline).dy;

      expect(indicatorTop(), closeTo(10.5 * 64, 0.1));

      clock.add(DateTime(2026, 10, 26, 10, 31));
      await tester.pump();
      await tester.pump();
      expect(indicatorTop(), closeTo(10.5166667 * 64, 0.1));

      clock.add(DateTime(2026, 10, 26, 8, 59));
      await tester.pump();
      await tester.pump();
      expect(indicator, findsOneWidget);
      expect(indicatorTop(), closeTo((8 + 59 / 60) * 64, 0.1));
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('Horizon hides the previous schedule while recalculating', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final repository = _FakeMemoryTaskRepository();
    final now = DateTime(2026, 10, 12, 14, 37);
    final task = await repository.addTask(
      listId: 'list-1',
      title: 'Intro to AI Questionnaire',
      estimatedDurationMinutes: 10,
    );
    final schedule = DailySchedule(
      leastSlackTask: task.title,
      actualSlack: '1h',
      realisticSlack: '1h',
      todayTasks: [
        ScheduledTask(
          id: task.id,
          title: task.title,
          startTime: DateTime(2026, 10, 12, 14, 39),
          endTime: DateTime(2026, 10, 12, 14, 49),
          dueDate: DateTime(2026, 10, 13, 23, 59),
          chunkHours: 10 / 60,
        ),
      ],
      futureTasks: [],
      unscheduledTasks: [],
      unscheduledTaskIds: [],
    );
    final refresh = Completer<DailySchedule>();
    var isRefreshing = false;
    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith((ref) => Stream.value(now)),
        scheduleProvider.overrideWith(
          (ref) => isRefreshing ? refresh.future : Future.value(schedule),
        ),
      ],
    );
    addTearDown(container.dispose);
    await container.read(scheduleProvider.future);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(body: HorizonGridView(listId: 'list-1')),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text(task.title), findsOneWidget);

    isRefreshing = true;
    container.invalidate(scheduleProvider);
    final refreshingSchedule = container.read(scheduleProvider.future);
    await tester.pump();

    expect(container.read(scheduleProvider).isLoading, isTrue);
    expect(find.text(task.title), findsNothing);
    expect(find.text('Updating schedule...'), findsOneWidget);
    expect(tester.takeException(), isNull);

    refresh.complete(schedule);
    await refreshingSchedule;
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    expect(find.text(task.title), findsOneWidget);
  });

  testWidgets('Horizon reports refresh errors without showing stale tasks', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final repository = _FakeMemoryTaskRepository();
    final task = await repository.addTask(
      listId: 'list-1',
      title: 'Task awaiting schedule',
      estimatedDurationMinutes: 30,
    );
    final schedule = DailySchedule(
      leastSlackTask: task.title,
      actualSlack: '1h',
      realisticSlack: '1h',
      todayTasks: [
        ScheduledTask(
          id: task.id,
          title: task.title,
          startTime: DateTime(2026, 10, 12, 14),
          endTime: DateTime(2026, 10, 12, 14, 30),
          dueDate: null,
          chunkHours: 0.5,
        ),
      ],
      futureTasks: [],
      unscheduledTasks: [],
      unscheduledTaskIds: [],
    );
    final now = DateTime(2026, 10, 12, 14, 37);
    var shouldFail = false;
    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith((ref) => Stream.value(now)),
        scheduleProvider.overrideWith((ref) async {
          if (shouldFail) {
            throw StateError('scheduling API unavailable');
          }
          return schedule;
        }),
      ],
    );
    addTearDown(container.dispose);
    await container.read(scheduleProvider.future);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(body: HorizonGridView(listId: 'list-1')),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text(task.title), findsOneWidget);

    shouldFail = true;
    container.invalidate(scheduleProvider);
    final failingSchedule = container.read(scheduleProvider.future);
    await expectLater(failingSchedule, throwsStateError);
    await tester.pump();

    expect(find.textContaining('Could not compute schedule:'), findsOneWidget);
    expect(find.text(task.title), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'HorizonGridView keeps Monday on far left and Sunday on far right across week transitions and DST',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final repository = _FakeMemoryTaskRepository();
      // Wednesday Oct 28, 2026 (DST ends Sunday Nov 1, 2026)
      final now = DateTime(2026, 10, 28, 14, 0);

      await repository.addEvent(
        listId: 'list-1',
        title: 'Team Sync',
        startTime: DateTime(2026, 10, 28, 10, 0),
        endTime: DateTime(2026, 10, 28, 11, 0),
      );

      final container = ProviderContainer(
        overrides: [
          taskRepositoryProvider.overrideWith((ref) => repository),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          scheduleProvider.overrideWith(
            (ref) => Future.value(DailySchedule.empty()),
          ),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: Scaffold(body: HorizonGridView(listId: 'list-1')),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      // Current week: Oct 26 (Mon) to Nov 1 (Sun)
      expect(find.text('MON Oct 26'), findsOneWidget);
      expect(find.text('SUN Nov 1'), findsOneWidget);
      expect(
        tester.getTopLeft(find.text('MON Oct 26')).dx,
        lessThan(tester.getTopLeft(find.text('SUN Nov 1')).dx),
      );

      // Navigate to Next Week: Nov 2 (Mon) to Nov 8 (Sun)
      await tester.tap(find.text('Next Week >'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('MON Nov 2'), findsOneWidget);
      expect(find.text('TUE Nov 3'), findsOneWidget);
      expect(find.text('WED Nov 4'), findsOneWidget);
      expect(find.text('THU Nov 5'), findsOneWidget);
      expect(find.text('FRI Nov 6'), findsOneWidget);
      expect(find.text('SAT Nov 7'), findsOneWidget);
      expect(find.text('SUN Nov 8'), findsOneWidget);
      expect(find.text('SUN Nov 1'), findsNothing);
      expect(
        tester.getTopLeft(find.text('MON Nov 2')).dx,
        lessThan(tester.getTopLeft(find.text('SUN Nov 8')).dx),
      );
      expect(find.text('HORIZON — Week of Nov 2 – Nov 8, 2026'), findsOneWidget);

      // Navigate to Previous Week twice (back to current week, then prior week: Oct 19 - Oct 25)
      await tester.tap(find.text('< Prev Week'));
      await tester.pump();
      await tester.tap(find.text('< Prev Week'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('MON Oct 19'), findsOneWidget);
      expect(find.text('SUN Oct 25'), findsOneWidget);
      expect(
        find.text('HORIZON — Week of Oct 19 – Oct 25, 2026'),
        findsOneWidget,
      );
    },
  );
}
