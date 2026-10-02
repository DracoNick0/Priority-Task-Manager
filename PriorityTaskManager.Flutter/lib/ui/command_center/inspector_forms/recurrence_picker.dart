import 'package:flutter/material.dart';

import '../../../models/recurrence_rule.dart';
import '../../theme/app_theme.dart';
import 'date_time_field.dart';

/// The pattern types a user can pick from; maps to one C# `RecurrenceRule`
/// subclass each (see docs/ARCHITECTURE_DATA.md "RecurrenceRule Polymorphic Shape").
enum RecurrencePatternType {
  daily,
  weekly,
  monthlyOnDays,
  monthlyRelative,
  yearly,
  explicitDates,
}

enum _EndType { never, after, until }

const List<String> _weekdayLabels = [
  'Sun',
  'Mon',
  'Tue',
  'Wed',
  'Thu',
  'Fri',
  'Sat',
];
const List<String> _dayOfWeekNames = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
];
const List<String> _monthLabels = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
];

/// A recurrence-pattern picker (issue #71): lets the user turn a new event
/// into a recurring series, choosing a pattern type and an end condition.
///
/// [onChanged] is called with the built [RecurrenceRule] whenever the
/// configuration changes and is currently valid/complete, or null while the
/// toggle is off or the current configuration is incomplete (e.g. no weekday
/// selected yet). [seriesStartDate] is read fresh each time a rule is built,
/// so it always reflects the enclosing form's current event start time.
class RecurrencePicker extends StatefulWidget {
  const RecurrencePicker({
    super.key,
    required this.seriesStartDate,
    required this.onChanged,
  });

  final DateTime Function() seriesStartDate;
  final ValueChanged<RecurrenceRule?> onChanged;

  @override
  State<RecurrencePicker> createState() => _RecurrencePickerState();
}

class _RecurrencePickerState extends State<RecurrencePicker> {
  bool _enabled = false;
  RecurrencePatternType _pattern = RecurrencePatternType.weekly;
  _EndType _endType = _EndType.after;

  final _intervalDaysController = TextEditingController(text: '1');
  final Set<int> _daysOfWeek = {};
  final _intervalWeeksController = TextEditingController(text: '1');
  final _daysOfMonthController = TextEditingController();
  String _shortMonthBehavior = 'Skip';
  String _ordinal = 'First';
  String _dayKind = 'SpecificDayOfWeek';
  String _relativeDayOfWeek = 'Monday';
  int _month = 1;
  final _yearlyDayController = TextEditingController(text: '1');
  final List<DateTime> _explicitDates = [];
  final _afterCountController = TextEditingController(text: '7');
  DateTime? _untilDate;

  @override
  void dispose() {
    _intervalDaysController.dispose();
    _intervalWeeksController.dispose();
    _daysOfMonthController.dispose();
    _yearlyDayController.dispose();
    _afterCountController.dispose();
    super.dispose();
  }

  void _update(VoidCallback fn) {
    setState(fn);
    widget.onChanged(_enabled ? _buildRule() : null);
  }

  RecurrenceEndCondition? _buildEndCondition() {
    switch (_endType) {
      case _EndType.never:
        return NeverEndCondition();
      case _EndType.after:
        final count = int.tryParse(_afterCountController.text);
        return (count == null || count < 1)
            ? null
            : AfterOccurrencesEndCondition(count);
      case _EndType.until:
        return _untilDate == null ? null : UntilDateEndCondition(_untilDate!);
    }
  }

  List<int> _parseDaysOfMonth(String text) {
    final days = <int>{};
    for (final part in text.split(',')) {
      final day = int.tryParse(part.trim());
      if (day != null && day >= 1 && day <= 31) days.add(day);
    }
    return days.toList()..sort();
  }

  RecurrenceRule? _buildRule() {
    final endCondition = _buildEndCondition();
    if (endCondition == null) return null;
    final seriesStartDate = widget.seriesStartDate();

    switch (_pattern) {
      case RecurrencePatternType.daily:
        final interval = int.tryParse(_intervalDaysController.text);
        if (interval == null || interval < 1) return null;
        return DailyIntervalRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          intervalDays: interval,
        );
      case RecurrencePatternType.weekly:
        if (_daysOfWeek.isEmpty) return null;
        final weeks = int.tryParse(_intervalWeeksController.text);
        if (weeks == null || weeks < 1) return null;
        return WeeklyRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          daysOfWeek: _daysOfWeek.toList()..sort(),
          intervalWeeks: weeks,
        );
      case RecurrencePatternType.monthlyOnDays:
        final days = _parseDaysOfMonth(_daysOfMonthController.text);
        if (days.isEmpty) return null;
        return MonthlyOnDaysRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          daysOfMonth: days,
          shortMonthBehavior: _shortMonthBehavior,
        );
      case RecurrencePatternType.monthlyRelative:
        return MonthlyRelativeRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          ordinal: _ordinal,
          dayKind: _dayKind,
          dayOfWeek: _dayKind == 'SpecificDayOfWeek'
              ? _relativeDayOfWeek
              : null,
        );
      case RecurrencePatternType.yearly:
        final day = int.tryParse(_yearlyDayController.text);
        if (day == null || day < 1 || day > 31) return null;
        return YearlyRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          month: _month,
          day: day,
        );
      case RecurrencePatternType.explicitDates:
        if (_explicitDates.isEmpty) return null;
        return ExplicitDatesRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          dates: List.of(_explicitDates),
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Card(
      margin: EdgeInsets.zero,
      color: colorScheme.surfaceContainerLow,
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingSm),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Recurring event'),
              value: _enabled,
              onChanged: (v) => _update(() {
                _enabled = v;
                if (v && _daysOfWeek.isEmpty) {
                  _daysOfWeek.add(widget.seriesStartDate().weekday % 7);
                }
              }),
            ),
            if (_enabled) ...[
              const SizedBox(height: AppTheme.spacingSm),
              DropdownButtonFormField<RecurrencePatternType>(
                initialValue: _pattern,
                decoration: const InputDecoration(labelText: 'Repeats'),
                items: const [
                  DropdownMenuItem(
                    value: RecurrencePatternType.daily,
                    child: Text('Daily'),
                  ),
                  DropdownMenuItem(
                    value: RecurrencePatternType.weekly,
                    child: Text('Weekly'),
                  ),
                  DropdownMenuItem(
                    value: RecurrencePatternType.monthlyOnDays,
                    child: Text('Monthly (on day of month)'),
                  ),
                  DropdownMenuItem(
                    value: RecurrencePatternType.monthlyRelative,
                    child: Text('Monthly (relative day)'),
                  ),
                  DropdownMenuItem(
                    value: RecurrencePatternType.yearly,
                    child: Text('Yearly'),
                  ),
                  DropdownMenuItem(
                    value: RecurrencePatternType.explicitDates,
                    child: Text('Custom dates'),
                  ),
                ],
                onChanged: (v) => _update(() => _pattern = v!),
              ),
              const SizedBox(height: AppTheme.spacingSm),
              _buildPatternFields(),
              const Divider(height: AppTheme.spacingLg),
              DropdownButtonFormField<_EndType>(
                initialValue: _endType,
                decoration: const InputDecoration(labelText: 'Ends'),
                items: const [
                  DropdownMenuItem(value: _EndType.never, child: Text('Never')),
                  DropdownMenuItem(
                    value: _EndType.after,
                    child: Text('After a number of times'),
                  ),
                  DropdownMenuItem(
                    value: _EndType.until,
                    child: Text('On a date'),
                  ),
                ],
                onChanged: (v) => _update(() => _endType = v!),
              ),
              const SizedBox(height: AppTheme.spacingSm),
              _buildEndFields(),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildPatternFields() {
    switch (_pattern) {
      case RecurrencePatternType.daily:
        return TextField(
          controller: _intervalDaysController,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(labelText: 'Every N days'),
          onChanged: (_) => _update(() {}),
        );
      case RecurrencePatternType.weekly:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: AppTheme.spacingXs,
              children: List.generate(7, (day) {
                return FilterChip(
                  label: Text(_weekdayLabels[day]),
                  selected: _daysOfWeek.contains(day),
                  onSelected: (selected) => _update(() {
                    if (selected) {
                      _daysOfWeek.add(day);
                    } else {
                      _daysOfWeek.remove(day);
                    }
                  }),
                );
              }),
            ),
            const SizedBox(height: AppTheme.spacingSm),
            TextField(
              controller: _intervalWeeksController,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Every N weeks'),
              onChanged: (_) => _update(() {}),
            ),
          ],
        );
      case RecurrencePatternType.monthlyOnDays:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            TextField(
              controller: _daysOfMonthController,
              decoration: const InputDecoration(
                labelText: 'Days of month (comma-separated, e.g. 1, 15)',
              ),
              onChanged: (_) => _update(() {}),
            ),
            const SizedBox(height: AppTheme.spacingSm),
            DropdownButtonFormField<String>(
              initialValue: _shortMonthBehavior,
              decoration: const InputDecoration(
                labelText: 'If a day is missing in a short month',
              ),
              items: const [
                DropdownMenuItem(value: 'Skip', child: Text('Skip that month')),
                DropdownMenuItem(
                  value: 'ClampToLastDay',
                  child: Text('Use the last day of the month'),
                ),
              ],
              onChanged: (v) => _update(() => _shortMonthBehavior = v!),
            ),
          ],
        );
      case RecurrencePatternType.monthlyRelative:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            DropdownButtonFormField<String>(
              initialValue: _ordinal,
              decoration: const InputDecoration(labelText: 'Occurrence'),
              items: const [
                DropdownMenuItem(value: 'First', child: Text('First')),
                DropdownMenuItem(value: 'Second', child: Text('Second')),
                DropdownMenuItem(value: 'Third', child: Text('Third')),
                DropdownMenuItem(value: 'Fourth', child: Text('Fourth')),
                DropdownMenuItem(value: 'Last', child: Text('Last')),
              ],
              onChanged: (v) => _update(() => _ordinal = v!),
            ),
            const SizedBox(height: AppTheme.spacingSm),
            DropdownButtonFormField<String>(
              initialValue: _dayKind,
              decoration: const InputDecoration(labelText: 'Day'),
              items: const [
                DropdownMenuItem(
                  value: 'SpecificDayOfWeek',
                  child: Text('Specific weekday'),
                ),
                DropdownMenuItem(value: 'AnyDay', child: Text('Any day')),
                DropdownMenuItem(
                  value: 'Weekday',
                  child: Text('Weekday (Mon-Fri)'),
                ),
                DropdownMenuItem(
                  value: 'WeekendDay',
                  child: Text('Weekend day'),
                ),
              ],
              onChanged: (v) => _update(() => _dayKind = v!),
            ),
            if (_dayKind == 'SpecificDayOfWeek') ...[
              const SizedBox(height: AppTheme.spacingSm),
              DropdownButtonFormField<String>(
                initialValue: _relativeDayOfWeek,
                decoration: const InputDecoration(labelText: 'Weekday'),
                items: _dayOfWeekNames
                    .map((d) => DropdownMenuItem(value: d, child: Text(d)))
                    .toList(),
                onChanged: (v) => _update(() => _relativeDayOfWeek = v!),
              ),
            ],
          ],
        );
      case RecurrencePatternType.yearly:
        return Row(
          children: [
            Expanded(
              flex: 2,
              child: DropdownButtonFormField<int>(
                initialValue: _month,
                decoration: const InputDecoration(labelText: 'Month'),
                items: List.generate(
                  12,
                  (i) => DropdownMenuItem(
                    value: i + 1,
                    child: Text(_monthLabels[i]),
                  ),
                ),
                onChanged: (v) => _update(() => _month = v!),
              ),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: TextField(
                controller: _yearlyDayController,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Day'),
                onChanged: (_) => _update(() {}),
              ),
            ),
          ],
        );
      case RecurrencePatternType.explicitDates:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: AppTheme.spacingXs,
              children: [
                for (final date in _explicitDates)
                  Chip(
                    label: Text(dateTimeRowFormat.format(date)),
                    onDeleted: () => _update(() => _explicitDates.remove(date)),
                  ),
              ],
            ),
            const SizedBox(height: AppTheme.spacingSm),
            OutlinedButton.icon(
              icon: const Icon(Icons.add),
              label: const Text('Add date'),
              onPressed: () async {
                final picked = await pickDateAndTime(
                  context,
                  initialDate: widget.seriesStartDate(),
                );
                if (picked != null) _update(() => _explicitDates.add(picked));
              },
            ),
          ],
        );
    }
  }

  Widget _buildEndFields() {
    switch (_endType) {
      case _EndType.never:
        return const SizedBox.shrink();
      case _EndType.after:
        return TextField(
          controller: _afterCountController,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(labelText: 'Number of occurrences'),
          onChanged: (_) => _update(() {}),
        );
      case _EndType.until:
        return DateTimeFieldBox(
          icon: Icons.event_busy_outlined,
          label: 'Last occurrence date',
          value: _untilDate,
          onPick: () async {
            final picked = await pickDateAndTime(
              context,
              initialDate: _untilDate ?? widget.seriesStartDate(),
            );
            if (picked != null) _update(() => _untilDate = picked);
          },
        );
    }
  }
}
