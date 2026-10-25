import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/view_provider.dart';
import 'package:priority_task_manager/ui/command_center/command_center_screen.dart';
import 'package:priority_task_manager/ui/command_center/daily_rituals_dialog.dart';

class _FakeTaskListsNotifier extends TaskListsNotifier {
  @override
  Future<List<TaskList>> build() async {
    return [
      TaskList(
        id: 'list-1',
        name: 'Work',
      ),
    ];
  }
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
        child: const MaterialApp(
          home: CommandCenterScreen(),
        ),
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
        child: const MaterialApp(
          home: CommandCenterScreen(),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    // Hamburger icon should be visible since left rail is not docked
    expect(find.byIcon(Icons.menu), findsOneWidget);
  });
}
