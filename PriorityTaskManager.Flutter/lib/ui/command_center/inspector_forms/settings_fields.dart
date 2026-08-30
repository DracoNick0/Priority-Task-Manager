import 'package:flutter/material.dart';

import '../../../models/effective_settings.dart';
import '../../theme/app_theme.dart';

/// A titled, icon-led card wrapper used to visually group a related block of
/// settings fields (e.g. "Work hours", "Urgency thresholds").
class SettingsSectionCard extends StatelessWidget {
  const SettingsSectionCard({
    super.key,
    required this.icon,
    required this.title,
    required this.child,
    this.trailing,
  });

  final IconData icon;
  final String title;
  final Widget child;

  /// Optional trailing widget shown next to the title (e.g. an override
  /// checkbox).
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Card(
      margin: EdgeInsets.zero,
      color: colorScheme.surfaceContainerLow,
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingMd),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, size: 18, color: colorScheme.primary),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Text(
                    title,
                    style: Theme.of(context).textTheme.labelLarge?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                ?trailing,
              ],
            ),
            const SizedBox(height: AppTheme.spacingSm),
            child,
          ],
        ),
      ),
    );
  }
}

/// Dropdown for `SortOption` (mirrors `PriorityTaskManager.Models.SortOption`).
class SortOptionField extends StatelessWidget {
  const SortOptionField({
    super.key,
    required this.value,
    required this.onChanged,
  });

  final int value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<int>(
      initialValue: value,
      decoration: const InputDecoration(labelText: 'Sort tasks by'),
      items: [
        for (var i = 0; i < sortOptionNames.length; i++)
          DropdownMenuItem(value: i, child: Text(sortOptionNames[i])),
      ],
      onChanged: (v) => v == null ? null : onChanged(v),
    );
  }
}

/// Dropdown for `SchedulingMode` (mirrors `PriorityTaskManager.Models.SchedulingMode`).
class SchedulingModeField extends StatelessWidget {
  const SchedulingModeField({
    super.key,
    required this.value,
    required this.onChanged,
  });

  final int value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<int>(
      initialValue: value,
      decoration: const InputDecoration(labelText: 'Scheduling mode'),
      items: [
        for (var i = 0; i < schedulingModeNames.length; i++)
          DropdownMenuItem(value: i, child: Text(schedulingModeNames[i])),
      ],
      onChanged: (v) => v == null ? null : onChanged(v),
    );
  }
}

/// Time-of-day pickers for the workday start/end.
class WorkHoursField extends StatelessWidget {
  const WorkHoursField({
    super.key,
    required this.startMinutes,
    required this.endMinutes,
    required this.onStartChanged,
    required this.onEndChanged,
  });

  final int startMinutes;
  final int endMinutes;
  final ValueChanged<int> onStartChanged;
  final ValueChanged<int> onEndChanged;

  Future<void> _pick(
    BuildContext context,
    int currentMinutes,
    ValueChanged<int> onChanged,
  ) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(
        hour: currentMinutes ~/ 60,
        minute: currentMinutes % 60,
      ),
    );
    if (picked != null) {
      onChanged(picked.hour * 60 + picked.minute);
    }
  }

  String _format(int minutes) {
    final h = minutes ~/ 60;
    final m = minutes % 60;
    final period = h >= 12 ? 'PM' : 'AM';
    final h12 = h % 12 == 0 ? 12 : h % 12;
    return '$h12:${m.toString().padLeft(2, '0')} $period';
  }

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: OutlinedButton(
            onPressed: () => _pick(context, startMinutes, onStartChanged),
            child: Text('Start: ${_format(startMinutes)}'),
          ),
        ),
        const SizedBox(width: AppTheme.spacingSm),
        Expanded(
          child: OutlinedButton(
            onPressed: () => _pick(context, endMinutes, onEndChanged),
            child: Text('End: ${_format(endMinutes)}'),
          ),
        ),
      ],
    );
  }
}

const List<String> _weekdayShortNames = [
  'Mon',
  'Tue',
  'Wed',
  'Thu',
  'Fri',
  'Sat',
  'Sun',
];

/// Multi-select chips for workdays (Dart weekday ints, Monday = 1 ... Sunday = 7).
class WorkDaysField extends StatelessWidget {
  const WorkDaysField({
    super.key,
    required this.selected,
    required this.onChanged,
  });

  final Set<int> selected;
  final ValueChanged<Set<int>> onChanged;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AppTheme.spacingXs,
      runSpacing: AppTheme.spacingXs,
      children: [
        for (var day = 1; day <= 7; day++)
          FilterChip(
            label: Text(_weekdayShortNames[day - 1]),
            selected: selected.contains(day),
            showCheckmark: false,
            avatar: selected.contains(day)
                ? const Icon(Icons.check, size: 16)
                : null,
            onSelected: (isSelected) {
              final next = Set<int>.from(selected);
              if (isSelected) {
                next.add(day);
              } else {
                next.remove(day);
              }
              onChanged(next);
            },
          ),
      ],
    );
  }
}

/// Numeric fields for the four slack-urgency band thresholds.
class SlackThresholdsField extends StatelessWidget {
  const SlackThresholdsField({
    super.key,
    required this.dire,
    required this.pressing,
    required this.focus,
    required this.safe,
    required this.onDireChanged,
    required this.onPressingChanged,
    required this.onFocusChanged,
    required this.onSafeChanged,
  });

  final double dire;
  final double pressing;
  final double focus;
  final double safe;
  final ValueChanged<double> onDireChanged;
  final ValueChanged<double> onPressingChanged;
  final ValueChanged<double> onFocusChanged;
  final ValueChanged<double> onSafeChanged;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Multiplier of the average workday duration.',
          style: Theme.of(context).textTheme.bodySmall,
        ),
        const SizedBox(height: AppTheme.spacingSm),
        Row(
          children: [
            Expanded(
              child: _ThresholdField(
                label: 'Dire',
                value: dire,
                color: const Color(0xFFEF4444),
                onChanged: onDireChanged,
              ),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: _ThresholdField(
                label: 'Pressing',
                value: pressing,
                color: const Color(0xFFF97316),
                onChanged: onPressingChanged,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppTheme.spacingSm),
        Row(
          children: [
            Expanded(
              child: _ThresholdField(
                label: 'Focus',
                value: focus,
                color: const Color(0xFFEAB308),
                onChanged: onFocusChanged,
              ),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: _ThresholdField(
                label: 'Safe',
                value: safe,
                color: const Color(0xFF10B981),
                onChanged: onSafeChanged,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _ThresholdField extends StatelessWidget {
  const _ThresholdField({
    required this.label,
    required this.value,
    required this.color,
    required this.onChanged,
  });

  final String label;
  final double value;
  final Color color;
  final ValueChanged<double> onChanged;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      key: ValueKey('$label-${value.toStringAsFixed(2)}'),
      initialValue: value.toString(),
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: Padding(
          padding: const EdgeInsets.all(12),
          child: DecoratedBox(
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
        ),
        prefixIconConstraints: const BoxConstraints(
          minWidth: 24,
          minHeight: 24,
        ),
      ),
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      onChanged: (text) {
        final parsed = double.tryParse(text);
        if (parsed != null) onChanged(parsed);
      },
    );
  }
}

/// Wraps a setting [child] with a checkbox for "override for this list"; when
/// unchecked, the list inherits the global default and [child] is hidden.
class OverrideToggle extends StatelessWidget {
  const OverrideToggle({
    super.key,
    required this.label,
    required this.isOverridden,
    required this.onChanged,
    required this.child,
  });

  final String label;
  final bool isOverridden;
  final ValueChanged<bool> onChanged;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return AnimatedContainer(
      duration: const Duration(milliseconds: 150),
      margin: const EdgeInsets.only(bottom: AppTheme.spacingSm),
      padding: const EdgeInsets.symmetric(horizontal: AppTheme.spacingSm),
      decoration: BoxDecoration(
        color: isOverridden
            ? colorScheme.primaryContainer.withValues(alpha: 0.25)
            : Colors.transparent,
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          CheckboxListTile(
            value: isOverridden,
            onChanged: (v) => onChanged(v ?? false),
            title: Text(
              label,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                fontWeight: isOverridden ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            secondary: isOverridden
                ? null
                : Text(
                    'Inherited',
                    style: Theme.of(context).textTheme.labelSmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
            controlAffinity: ListTileControlAffinity.leading,
            contentPadding: EdgeInsets.zero,
            dense: true,
          ),
          if (isOverridden)
            Padding(
              padding: const EdgeInsets.only(
                left: AppTheme.spacingMd,
                bottom: AppTheme.spacingSm,
              ),
              child: child,
            ),
        ],
      ),
    );
  }
}
