import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../theme/app_theme.dart';
import 'combined_date_time_picker.dart';

/// Shared display format for a combined date + time value across the
/// inspector forms, e.g. "Wed, Sep 24 • 2:30 PM".
final DateFormat dateTimeRowFormat = DateFormat('EEE, MMM d • h:mm a');

/// Opens the combined date + time picker dialog used to choose a
/// [DateTime], returning null if the user cancels.
///
/// Centralizing this here means the picker UI only needs to change in one
/// place.
Future<DateTime?> pickDateAndTime(
  BuildContext context, {
  required DateTime initialDate,
  DateTime? firstDate,
  DateTime? lastDate,
  TimeOfDay? initialTime,
  String? timeHelpText,
}) {
  final time = initialTime ?? TimeOfDay.fromDateTime(initialDate);
  return showCombinedDateTimePicker(
    context,
    initialDateTime: DateTime(
      initialDate.year,
      initialDate.month,
      initialDate.day,
      time.hour,
      time.minute,
    ),
    firstDate: firstDate,
    lastDate: lastDate,
    subtitle: timeHelpText,
  );
}

/// Bordered, labeled date/time field used for standalone form fields (e.g.
/// a task's due date), with an optional clear button when [value] is set.
class DateTimeFieldBox extends StatelessWidget {
  const DateTimeFieldBox({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
    required this.onPick,
    this.onClear,
  });

  final IconData icon;
  final String label;
  final DateTime? value;
  final VoidCallback onPick;

  /// Null hides the clear button, even when [value] is set.
  final VoidCallback? onClear;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return InkWell(
      onTap: onPick,
      borderRadius: BorderRadius.circular(AppTheme.radiusMd),
      child: Container(
        padding: const EdgeInsets.symmetric(
          horizontal: AppTheme.spacingMd,
          vertical: AppTheme.spacingSm,
        ),
        decoration: BoxDecoration(
          border: Border.all(color: colorScheme.outline),
          borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        ),
        child: Row(
          children: [
            Icon(icon, size: 20, color: colorScheme.onSurfaceVariant),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: Theme.of(context).textTheme.labelSmall),
                  Text(
                    value == null
                        ? 'Not set'
                        : dateTimeRowFormat.format(value!.toLocal()),
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
            if (value != null && onClear != null)
              IconButton(
                icon: const Icon(Icons.clear, size: 18),
                tooltip: 'Clear $label',
                onPressed: onClear,
              ),
          ],
        ),
      ),
    );
  }
}

/// Compact single-line date/time row for a value that's always set, meant to
/// sit inside a card alongside other rows (e.g. an event's start/end, or a
/// list's simulated-time override).
class DateTimeCompactRow extends StatelessWidget {
  const DateTimeCompactRow({
    super.key,
    required this.icon,
    required this.value,
    required this.onPick,
    this.label,
    this.editIconColor,
  });

  final IconData icon;
  final DateTime value;
  final VoidCallback onPick;

  /// Optional short label shown before the value (e.g. "Start"/"End").
  final String? label;

  /// Defaults to the theme's primary color.
  final Color? editIconColor;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return InkWell(
      onTap: onPick,
      borderRadius: BorderRadius.circular(AppTheme.radiusSm),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppTheme.spacingXs),
        child: Row(
          children: [
            Icon(icon, size: 18, color: colorScheme.onSurfaceVariant),
            const SizedBox(width: AppTheme.spacingSm),
            if (label != null)
              SizedBox(
                width: 40,
                child: Text(
                  label!,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ),
            Expanded(
              child: Text(
                dateTimeRowFormat.format(value.toLocal()),
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ),
            Icon(
              Icons.edit_calendar_outlined,
              size: 18,
              color: editIconColor ?? colorScheme.primary,
            ),
          ],
        ),
      ),
    );
  }
}
