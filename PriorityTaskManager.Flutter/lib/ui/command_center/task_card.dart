import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models/task_item.dart';
import '../theme/app_theme.dart';

/// A single scheduled-task card in a Daily Column.
///
/// Visual states (per spec):
/// - Completed: 40% opacity, strikethrough title, circular checkbox.
/// - Blocked (unmet dependencies): 50% opacity, lock icon.
/// - Split/fragmented across days: dashed border + "n/total" badge.
class TaskCard extends StatelessWidget {
  const TaskCard({
    super.key,
    required this.task,
    this.startTime,
    this.endTime,
    required this.isBlocked,
    this.fragmentIndex,
    this.fragmentTotal,
    required this.onToggleCompleted,
    required this.onTap,
  });

  final TaskItem task;
  final DateTime? startTime;
  final DateTime? endTime;
  final bool isBlocked;
  final int? fragmentIndex;
  final int? fragmentTotal;
  final ValueChanged<bool> onToggleCompleted;
  final VoidCallback onTap;

  bool get _isFragmented => fragmentTotal != null && fragmentTotal! > 1;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final timeFormat = DateFormat.jm();

    final double opacity = task.isCompleted
        ? 0.4
        : (isBlocked ? 0.5 : 1.0);

    Widget card = Container(
      margin: const EdgeInsets.symmetric(vertical: AppTheme.spacingXs),
      decoration: BoxDecoration(
        color: colorScheme.surface,
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        border: _isFragmented
            ? Border(
                top: BorderSide(color: colorScheme.primary, width: 2),
                bottom: BorderSide(color: colorScheme.primary, width: 2),
                left: BorderSide(color: colorScheme.outlineVariant),
                right: BorderSide(color: colorScheme.outlineVariant),
              )
            : Border.all(color: colorScheme.outlineVariant),
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(AppTheme.radiusMd),
          child: Padding(
            padding: const EdgeInsets.all(AppTheme.spacingSm),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _CircularCheckbox(
                  value: task.isCompleted,
                  onChanged: isBlocked ? null : onToggleCompleted,
                ),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              task.title,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                fontWeight: FontWeight.w600,
                                decoration: task.isCompleted
                                    ? TextDecoration.lineThrough
                                    : null,
                              ),
                            ),
                          ),
                          if (isBlocked) ...[
                            const SizedBox(width: 4),
                            Icon(
                              Icons.lock_outline,
                              size: 14,
                              color: colorScheme.onSurfaceVariant,
                            ),
                          ],
                        ],
                      ),
                      if (startTime != null && endTime != null) ...[
                        const SizedBox(height: 2),
                        Text(
                          '${timeFormat.format(startTime!)} \u2013 ${timeFormat.format(endTime!)}',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                      ],
                      if (_isFragmented) ...[
                        const SizedBox(height: 4),
                        _FragmentBadge(
                          index: fragmentIndex ?? 1,
                          total: fragmentTotal!,
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );

    return AnimatedOpacity(
      duration: const Duration(milliseconds: 200),
      opacity: opacity,
      child: card,
    );
  }
}

class _FragmentBadge extends StatelessWidget {
  const _FragmentBadge({required this.index, required this.total});

  final int index;
  final int total;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
      decoration: BoxDecoration(
        color: colorScheme.primaryContainer,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        '$index/$total',
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.bold,
          color: colorScheme.onPrimaryContainer,
        ),
      ),
    );
  }
}

class _CircularCheckbox extends StatelessWidget {
  const _CircularCheckbox({required this.value, required this.onChanged});

  final bool value;
  final ValueChanged<bool>? onChanged;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return GestureDetector(
      onTap: onChanged == null ? null : () => onChanged!(!value),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        width: 20,
        height: 20,
        margin: const EdgeInsets.only(top: 2),
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: value ? colorScheme.primary : Colors.transparent,
          border: Border.all(
            color: value ? colorScheme.primary : colorScheme.outline,
            width: 2,
          ),
        ),
        child: value
            ? Icon(Icons.check, size: 14, color: colorScheme.onPrimary)
            : null,
      ),
    );
  }
}
