import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/ui/command_center/inspector_forms/recurrence_picker.dart';

void main() {
  for (final (startDate, dayLabel, weekday) in [
    (DateTime(2026, 10, 1), 'Thu', 4),
    (DateTime(2026, 10, 4), 'Sun', 0),
  ]) {
    testWidgets('enabling recurrence defaults to $dayLabel after 7 times', (
      tester,
    ) async {
      RecurrenceRule? selectedRule;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: RecurrencePicker(
              seriesStartDate: () => startDate,
              onChanged: (rule) => selectedRule = rule,
            ),
          ),
        ),
      );

      await tester.tap(find.byType(SwitchListTile));
      await tester.pumpAndSettle();

      final rule = selectedRule! as WeeklyRecurrenceRule;
      expect(rule.daysOfWeek, [weekday]);
      expect(rule.seriesStartDate, startDate);
      expect(
        (rule.endCondition as AfterOccurrencesEndCondition).occurrenceCount,
        7,
      );
      expect(find.widgetWithText(FilterChip, dayLabel), findsOneWidget);
      expect(
        tester
            .widget<FilterChip>(find.widgetWithText(FilterChip, dayLabel))
            .selected,
        isTrue,
      );
      expect(find.text('After a number of times'), findsOneWidget);
      expect(
        find.widgetWithText(TextField, 'Number of occurrences'),
        findsOneWidget,
      );
      expect(
        tester.widget<TextField>(find.byType(TextField).last).controller!.text,
        '7',
      );
    });
  }
}
