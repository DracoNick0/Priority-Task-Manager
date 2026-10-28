import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/ui/command_center/inspector_forms/recurrence_picker.dart';

void main() {
  testWidgets('editing a recurrence initializes its pattern and end settings', (
    tester,
  ) async {
    final rule = WeeklyRecurrenceRule(
      seriesStartDate: DateTime(2026, 10, 8),
      endCondition: AfterOccurrencesEndCondition(3),
      daysOfWeek: [1],
      intervalWeeks: 2,
    );
    RecurrenceRule? changedRule;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: RecurrencePicker(
            seriesStartDate: () => rule.seriesStartDate,
            initialRule: rule,
            canDisable: false,
            label: 'Series recurrence',
            onChanged: (value) => changedRule = value,
          ),
        ),
      ),
    );

    expect(find.text('Series recurrence'), findsOneWidget);
    expect(find.byType(SwitchListTile), findsNothing);
    expect(find.widgetWithText(FilterChip, 'Mon'), findsOneWidget);
    expect(
      tester
          .widget<FilterChip>(find.widgetWithText(FilterChip, 'Mon'))
          .selected,
      isTrue,
    );
    expect(
      tester
          .widget<FilterChip>(find.widgetWithText(FilterChip, 'Thu'))
          .selected,
      isFalse,
    );
    expect(
      tester.widget<TextField>(find.byType(TextField).first).controller!.text,
      '2',
    );
    expect(
      tester.widget<TextField>(find.byType(TextField).last).controller!.text,
      '3',
    );

    await tester.tap(find.widgetWithText(FilterChip, 'Thu'));
    await tester.pumpAndSettle();
    expect(changedRule, isA<WeeklyRecurrenceRule>());
    expect((changedRule! as WeeklyRecurrenceRule).daysOfWeek, [1, 4]);
  });

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

  testWidgets(
    'changing the series start while enabled notifies the parent after build',
    (tester) async {
      await tester.pumpWidget(const _RecurrencePickerHost());
      await tester.tap(find.byType(SwitchListTile));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Advance series start'));
      await tester.pump();

      expect(tester.takeException(), isNull);
      await tester.pumpAndSettle();
      expect(find.text('2026-10-09T00:00:00.000'), findsOneWidget);
    },
  );
}

class _RecurrencePickerHost extends StatefulWidget {
  const _RecurrencePickerHost();

  @override
  State<_RecurrencePickerHost> createState() => _RecurrencePickerHostState();
}

class _RecurrencePickerHostState extends State<_RecurrencePickerHost> {
  DateTime _seriesStart = DateTime(2026, 10, 8);
  RecurrenceRule? _rule;

  @override
  Widget build(BuildContext context) => MaterialApp(
    home: Scaffold(
      body: Column(
        children: [
          TextButton(
            onPressed: () => setState(
              () => _seriesStart = _seriesStart.add(const Duration(days: 1)),
            ),
            child: const Text('Advance series start'),
          ),
          RecurrencePicker(
            seriesStartDate: () => _seriesStart,
            onChanged: (rule) => setState(() => _rule = rule),
          ),
          Text(_rule?.seriesStartDate.toIso8601String() ?? 'No recurrence'),
        ],
      ),
    ),
  );
}
