import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
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
}
