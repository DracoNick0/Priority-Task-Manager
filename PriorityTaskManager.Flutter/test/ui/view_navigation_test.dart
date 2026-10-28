import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/providers/event_providers.dart';
import 'package:priority_task_manager/models/schedule_models.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/session_provider.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/view_provider.dart';
import 'package:priority_task_manager/ui/command_center/command_center_screen.dart';
import 'package:priority_task_manager/ui/command_center/daily_rituals_dialog.dart';
import 'package:priority_task_manager/ui/command_center/center_stage.dart';

class _FakeTaskListsNotifier extends TaskListsNotifier {
  @override
  Future<List<TaskList>> build() async {
    return [TaskList(id: 'list-1', name: 'Work')];
  }
}

class _FakeTasksNotifier extends TasksNotifier {
  @override
  Future<List<TaskItem>> build(String arg) async => [];
}

class _FakeEventsNotifier extends EventsNotifier {
  _FakeEventsNotifier(this.events);

  final List<FixedEvent> events;

  @override
  Future<List<FixedEvent>> build(String arg) async => events;
}

class _AuthenticatedSessionController extends SessionController {
  @override
  Future<SessionState> build() async =>
      const SessionState(status: SessionStatus.authenticated);
}

void main() {
  testWidgets('Top app bar renders view switcher and switches modes', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final container = ProviderContainer(
      overrides: [
        taskListsProvider.overrideWith(_FakeTaskListsNotifier.new),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(DateTime(2026, 10, 24, 10, 0)),
        ),
        scheduleProvider.overrideWith((ref) async => DailySchedule.empty()),
        dailyRitualsStateProvider.overrideWith(
          (ref) => const DailyRitualsState(
            lastMorningDate: '2026-10-24',
            lastEveningDate: '2026-10-24',
          ),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(home: CommandCenterScreen()),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Verify segmented control items exist
    expect(find.text('Today'), findsOneWidget);
    expect(find.text('Horizon'), findsOneWidget);
    expect(find.text('Projects'), findsOneWidget);

    // Initial view is today
    expect(container.read(viewModeProvider), ViewMode.today);

    // Switch to Horizon
    await tester.tap(find.text('Horizon'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    expect(container.read(viewModeProvider), ViewMode.horizon);

    // Switch to Projects
    await tester.tap(find.text('Projects'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    expect(container.read(viewModeProvider), ViewMode.projects);
  });

  testWidgets('Collapses panes on narrow viewport (<720px)', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(600, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final container = ProviderContainer(
      overrides: [
        taskListsProvider.overrideWith(_FakeTaskListsNotifier.new),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(DateTime(2026, 10, 24, 10, 0)),
        ),
        scheduleProvider.overrideWith((ref) async => DailySchedule.empty()),
        dailyRitualsStateProvider.overrideWith(
          (ref) => const DailyRitualsState(
            lastMorningDate: '2026-10-24',
            lastEveningDate: '2026-10-24',
          ),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(home: CommandCenterScreen()),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Hamburger icon should be visible since left rail is not docked
    expect(find.byIcon(Icons.menu), findsOneWidget);
  });

  testWidgets(
    'Projects hides ended events while keeping ongoing and future events',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final now = DateTime(2026, 10, 26, 9, 1);
      final today = DateTime(now.year, now.month, now.day);
      final events = [
        FixedEvent(
          id: 'ended-event',
          listId: 'list-1',
          title: 'Ended Meeting',
          startTime: DateTime(today.year, today.month, today.day, 8),
          endTime: DateTime(today.year, today.month, today.day, 9),
        ),
        FixedEvent(
          id: 'meeting-ending-now',
          listId: 'list-1',
          title: 'Meeting Ending Now',
          startTime: DateTime(today.year, today.month, today.day, 8, 30),
          endTime: now,
        ),
        FixedEvent(
          id: 'ongoing-event',
          listId: 'list-1',
          title: 'Ongoing Meeting',
          startTime: DateTime(today.year, today.month, today.day, 8, 30),
          endTime: DateTime(today.year, today.month, today.day, 9, 30),
        ),
        FixedEvent(
          id: 'future-event',
          listId: 'list-1',
          title: 'Upcoming Meeting',
          startTime: DateTime(today.year, today.month, today.day, 10),
          endTime: DateTime(today.year, today.month, today.day, 11),
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          taskListsProvider.overrideWith(_FakeTaskListsNotifier.new),
          activeListIdProvider.overrideWith((ref) => 'list-1'),
          engineClockProvider.overrideWith((ref) => Stream.value(now)),
          sessionControllerProvider.overrideWith(
            _AuthenticatedSessionController.new,
          ),
          viewModeProvider.overrideWith((ref) => ViewMode.projects),
          tasksProvider.overrideWith(() => _FakeTasksNotifier()),
          eventsProvider.overrideWith(() => _FakeEventsNotifier(events)),
          eventOccurrencesProvider((
            'list-1',
            today,
          )).overrideWith((ref) async => events),
          scheduleProvider.overrideWith((ref) async => DailySchedule.empty()),
        ],
      );
      addTearDown(container.dispose);

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: Scaffold(body: CenterStage(onOpenInspector: () {})),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('Ended Meeting'), findsNothing);
      expect(find.text('Meeting Ending Now'), findsNothing);
      expect(find.text('Ongoing Meeting'), findsOneWidget);
      expect(find.text('Upcoming Meeting'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
