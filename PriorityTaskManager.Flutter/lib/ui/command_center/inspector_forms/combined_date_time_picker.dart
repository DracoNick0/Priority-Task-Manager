import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';
import 'date_time_field.dart';

const int _minutesPerSlot = 30;
const double _timeSlotHeight = 40;
const double _paneHeight = 300;

/// Opens an Outlook-style combined date + time picker: one dialog with a
/// calendar on one side and a scrollable list of time slots on the other,
/// so a day and a time are chosen together instead of across two dialogs.
Future<DateTime?> showCombinedDateTimePicker(
  BuildContext context, {
  required DateTime initialDateTime,
  DateTime? firstDate,
  DateTime? lastDate,
  String? subtitle,
}) {
  return showDialog<DateTime>(
    context: context,
    builder: (context) => _CombinedDateTimePickerDialog(
      initialDateTime: initialDateTime,
      firstDate:
          firstDate ?? DateTime.now().subtract(const Duration(days: 365)),
      lastDate: lastDate ?? DateTime.now().add(const Duration(days: 365 * 5)),
      subtitle: subtitle,
    ),
  );
}

class _CombinedDateTimePickerDialog extends StatefulWidget {
  const _CombinedDateTimePickerDialog({
    required this.initialDateTime,
    required this.firstDate,
    required this.lastDate,
    this.subtitle,
  });

  final DateTime initialDateTime;
  final DateTime firstDate;
  final DateTime lastDate;
  final String? subtitle;

  @override
  State<_CombinedDateTimePickerDialog> createState() =>
      _CombinedDateTimePickerDialogState();
}

class _CombinedDateTimePickerDialogState
    extends State<_CombinedDateTimePickerDialog> {
  late DateTime _date;
  late TimeOfDay _time;
  late final TextEditingController _timeController;
  late final ScrollController _timeListController;
  String? _timeError;

  @override
  void initState() {
    super.initState();
    _date = DateTime(
      widget.initialDateTime.year,
      widget.initialDateTime.month,
      widget.initialDateTime.day,
    );
    _time = TimeOfDay.fromDateTime(widget.initialDateTime);
    _timeController = TextEditingController(text: _formatTypedTime(_time));

    // Centers the initially-selected slot in the list rather than leaving it
    // at the very top, so nearby times are visible without scrolling.
    final initialSlot = (_time.hour * 60 + _time.minute) ~/ _minutesPerSlot;
    _timeListController = ScrollController(
      initialScrollOffset: (initialSlot * _timeSlotHeight - _timeSlotHeight * 3)
          .clamp(0.0, double.infinity),
    );
  }

  @override
  void dispose() {
    _timeController.dispose();
    _timeListController.dispose();
    super.dispose();
  }

  static String _formatTypedTime(TimeOfDay time) {
    final hour = time.hourOfPeriod == 0 ? 12 : time.hourOfPeriod;
    final minute = time.minute.toString().padLeft(2, '0');
    final period = time.period == DayPeriod.am ? 'AM' : 'PM';
    return '$hour:$minute $period';
  }

  static TimeOfDay? _parseTypedTime(String value) {
    final match = RegExp(
      r'^\s*(\d{1,2}):(\d{2})\s*([AaPp][Mm])\s*$',
    ).firstMatch(value);
    if (match == null) return null;
    final hour12 = int.parse(match.group(1)!);
    final minute = int.parse(match.group(2)!);
    if (hour12 < 1 || hour12 > 12 || minute > 59) return null;
    final isPm = match.group(3)!.toUpperCase() == 'PM';
    return TimeOfDay(hour: hour12 % 12 + (isPm ? 12 : 0), minute: minute);
  }

  void _selectSlot(TimeOfDay time) {
    setState(() {
      _time = time;
      _timeController.text = _formatTypedTime(time);
      _timeError = null;
    });
  }

  void _applyTypedTime(String value) {
    final parsed = _parseTypedTime(value);
    if (parsed == null) {
      setState(() => _timeError = 'Use a format like 2:15 PM');
      return;
    }
    setState(() {
      _time = parsed;
      _timeError = null;
    });
  }

  DateTime get _combined =>
      DateTime(_date.year, _date.month, _date.day, _time.hour, _time.minute);

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isNarrow = MediaQuery.sizeOf(context).width < 560;

    final calendar = CalendarDatePicker(
      initialDate: _date,
      firstDate: widget.firstDate,
      lastDate: widget.lastDate,
      onDateChanged: (d) => setState(() => _date = d),
    );

    final timePane = _TimeSlotPane(
      selected: _time,
      controller: _timeController,
      listController: _timeListController,
      error: _timeError,
      onSlotSelected: _selectSlot,
      onTypedSubmitted: _applyTypedTime,
    );

    return Dialog(
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: isNarrow ? 340 : 560),
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.spacingMd),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                dateTimeRowFormat.format(_combined),
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
              ),
              if (widget.subtitle != null) ...[
                const SizedBox(height: AppTheme.spacingXs),
                Text(
                  widget.subtitle!,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
              const SizedBox(height: AppTheme.spacingMd),
              if (isNarrow) ...[
                calendar,
                const Divider(),
                SizedBox(height: _paneHeight, child: timePane),
              ] else
                SizedBox(
                  height: _paneHeight,
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(child: calendar),
                      const VerticalDivider(width: AppTheme.spacingMd),
                      SizedBox(width: 200, child: timePane),
                    ],
                  ),
                ),
              const SizedBox(height: AppTheme.spacingMd),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text('Cancel'),
                  ),
                  const SizedBox(width: AppTheme.spacingSm),
                  FilledButton(
                    onPressed: () => Navigator.of(context).pop(_combined),
                    child: const Text('Done'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// The scrollable half-hour time slots plus a text field for typing an exact
/// (non-half-hour) time.
class _TimeSlotPane extends StatelessWidget {
  const _TimeSlotPane({
    required this.selected,
    required this.controller,
    required this.listController,
    required this.error,
    required this.onSlotSelected,
    required this.onTypedSubmitted,
  });

  final TimeOfDay selected;
  final TextEditingController controller;
  final ScrollController listController;
  final String? error;
  final ValueChanged<TimeOfDay> onSlotSelected;
  final ValueChanged<String> onTypedSubmitted;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    const slotCount = (24 * 60) ~/ _minutesPerSlot;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          controller: controller,
          decoration: InputDecoration(
            labelText: 'Time',
            hintText: 'e.g. 2:15 PM',
            errorText: error,
            isDense: true,
          ),
          onSubmitted: onTypedSubmitted,
        ),
        const SizedBox(height: AppTheme.spacingSm),
        Expanded(
          child: Container(
            decoration: BoxDecoration(
              border: Border.all(color: colorScheme.outlineVariant),
              borderRadius: BorderRadius.circular(AppTheme.radiusSm),
            ),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(AppTheme.radiusSm),
              child: ListView.builder(
                clipBehavior: Clip.antiAlias,
                controller: listController,
                itemExtent: _timeSlotHeight,
                itemCount: slotCount,
                itemBuilder: (context, index) {
                  final minutes = index * _minutesPerSlot;
                  final time = TimeOfDay(
                    hour: minutes ~/ 60,
                    minute: minutes % 60,
                  );
                  final isSelected =
                      time.hour == selected.hour &&
                      time.minute == selected.minute;
                  return Material(
                    type: MaterialType.transparency,
                    child: Ink(
                      color: isSelected ? colorScheme.primaryContainer : null,
                      child: InkWell(
                        onTap: () => onSlotSelected(time),
                        child: Container(
                          height: _timeSlotHeight,
                          padding: const EdgeInsets.symmetric(
                            horizontal: AppTheme.spacingMd,
                          ),
                          alignment: Alignment.centerLeft,
                          child: Text(
                            time.format(context),
                            style: isSelected
                                ? TextStyle(
                                    color: colorScheme.onPrimaryContainer,
                                    fontWeight: FontWeight.w600,
                                  )
                                : null,
                          ),
                        ),
                      ),
                    ),
                  );
                },
              ),
            ),
          ),
        ),
      ],
    );
  }
}
