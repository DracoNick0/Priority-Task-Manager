import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/intl.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/ui/command_center/task_card.dart';

void main() {
  testWidgets('shows a completed task due date when requested', (
    WidgetTester tester,
  ) async {
    final task = TaskItem(
      id: 'task-1',
      listId: 'list-1',
      title: 'Prepare presentation',
      isCompleted: true,
      dueDate: DateTime(2026, 10, 10),
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: TaskCard(
            task: task,
            showDueDate: true,
            isBlocked: false,
            onToggleCompleted: (_) {},
            onTap: () {},
          ),
        ),
      ),
    );

    expect(find.text('Prepare presentation'), findsOneWidget);
    expect(find.text('Due Oct 10, 2026'), findsOneWidget);
  });

  testWidgets('shows when a completed task has no due date', (
    WidgetTester tester,
  ) async {
    final task = TaskItem(
      id: 'task-1',
      listId: 'list-1',
      title: 'Review notes',
      isCompleted: true,
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: TaskCard(
            task: task,
            showDueDate: true,
            isBlocked: false,
            onToggleCompleted: (_) {},
            onTap: () {},
          ),
        ),
      ),
    );

    expect(find.text('Review notes'), findsOneWidget);
    expect(find.text('No due date'), findsOneWidget);
  });

  testWidgets('shows the scheduled time and part number for split tasks', (
    WidgetTester tester,
  ) async {
    final task = TaskItem(
      id: 'task-1',
      listId: 'list-1',
      title: 'Prepare presentation',
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: TaskCard(
            task: task,
            startTime: DateTime(2026, 10, 8, 9),
            endTime: DateTime(2026, 10, 8, 10),
            isBlocked: false,
            fragmentIndex: 1,
            fragmentTotal: 2,
            onToggleCompleted: (_) {},
            onTap: () {},
          ),
        ),
      ),
    );

    final expectedTimeRange =
        '${DateFormat.jm().format(DateTime(2026, 10, 8, 9))} \u2013 '
        '${DateFormat.jm().format(DateTime(2026, 10, 8, 10))}';
    expect(find.text(expectedTimeRange), findsOneWidget);
    expect(find.text('Part 1 of 2'), findsOneWidget);
  });

  testWidgets('shows recurring progress, missed state, streaks, and actions', (
    WidgetTester tester,
  ) async {
    final task = TaskItem(
      id: 'occurrence-1',
      listId: 'list-1',
      title: 'Take medication',
      seriesId: 'series-1',
      occurrenceDate: DateTime(2026, 10, 8),
      completionCount: 2,
      requiredCompletions: 3,
      isMissed: true,
      hasMissedOccurrence: true,
      trackStreak: true,
      currentStreak: 4,
      bestStreak: 7,
    );
    var skipped = false;
    var undone = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: TaskCard(
            task: task,
            isBlocked: false,
            onToggleCompleted: (_) {},
            onUndoOccurrenceUnit: () => undone = true,
            onSkipOccurrence: () => skipped = true,
            onTap: () {},
          ),
        ),
      ),
    );

    expect(find.text('2/3 completions'), findsOneWidget);
    expect(find.text('Missed occurrence'), findsOneWidget);
    expect(find.text('Streak 4 \u2022 Best 7'), findsOneWidget);
    await tester.tap(find.text('Undo last step'));
    await tester.tap(find.text('Skip occurrence'));
    expect(undone, isTrue);
    expect(skipped, isTrue);
  });
}
