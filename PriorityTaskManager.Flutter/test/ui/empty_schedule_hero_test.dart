import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/providers/selection_provider.dart';
import 'package:priority_task_manager/ui/command_center/empty_schedule_hero.dart';

void main() {
  Widget buildTestWidget({
    required Widget child,
    List<Override> overrides = const [],
  }) {
    return ProviderScope(
      overrides: overrides,
      child: MaterialApp(
        home: Scaffold(
          body: child,
        ),
      ),
    );
  }

  testWidgets('EmptyScheduleHero renders welcome text and action cards', (
    tester,
  ) async {
    await tester.pumpWidget(
      buildTestWidget(
        child: const EmptyScheduleHero(listId: 'test-list-id'),
      ),
    );
    await tester.pump();

    expect(find.text('Welcome to Priority Task Manager'), findsOneWidget);
    expect(find.text('Take a 60-Sec Tour'), findsOneWidget);
    expect(find.text('Add Your First Task'), findsOneWidget);
    expect(find.text('Add a Fixed Event'), findsOneWidget);
    expect(find.text('Or load a 1-day sample schedule'), findsOneWidget);
  });

  testWidgets('Tour dialog navigates through all 3 steps and closes', (
    tester,
  ) async {
    await tester.pumpWidget(
      buildTestWidget(
        child: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () => showOnboardingTourDialog(context),
            child: const Text('Open Tour'),
          ),
        ),
      ),
    );
    await tester.pump();

    await tester.tap(find.text('Open Tour'));
    await tester.pumpAndSettle();

    // Step 1
    expect(find.text('Effort Over Backlogs'), findsOneWidget);
    expect(find.text('Next: Work Splitting'), findsOneWidget);

    // Navigate to Step 2
    await tester.tap(find.text('Next: Work Splitting'));
    await tester.pumpAndSettle();

    expect(find.text('Intelligent Work Splitting'), findsOneWidget);
    expect(find.text('Feature Architecture: Part 1 (1.5h)'), findsOneWidget);
    expect(find.text('Next: Honest Slack'), findsOneWidget);

    // Navigate to Step 3
    await tester.tap(find.text('Next: Honest Slack'));
    await tester.pumpAndSettle();

    expect(find.text('Honest Slack & Risk Warnings'), findsOneWidget);
    expect(find.text('Get Started'), findsOneWidget);

    // Close on step 3
    await tester.tap(find.text('Get Started'));
    await tester.pumpAndSettle();

    expect(find.byType(OnboardingTourDialog), findsNothing);
  });

  testWidgets('Action cards set appropriate inspector targets', (
    tester,
  ) async {
    final container = ProviderContainer();
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(
          home: Scaffold(
            body: EmptyScheduleHero(listId: 'test-list-id'),
          ),
        ),
      ),
    );
    await tester.pump();

    // Tap + New Task
    await tester.tap(find.text('+ New Task'));
    await tester.pump();

    expect(
      container.read(selectedInspectorProvider),
      const InspectorTarget(kind: InspectorKind.task, id: null),
    );

    // Tap + New Event
    await tester.tap(find.text('+ New Event'));
    await tester.pump();

    expect(
      container.read(selectedInspectorProvider),
      const InspectorTarget(kind: InspectorKind.event, id: null),
    );
  });
}
