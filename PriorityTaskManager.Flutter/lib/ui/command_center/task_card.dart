import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models/task_item.dart';
import '../../utils/external_link.dart';
import '../theme/app_theme.dart';

/// A single scheduled-task card in a Daily Column.
///
/// Visual states (per spec):
/// - Completed: 40% opacity, strikethrough title, circular checkbox.
/// - Blocked (unmet dependencies): 50% opacity, lock icon.
/// - Split/fragmented across the schedule: highlighted border + part badge.
class TaskCard extends StatelessWidget {
  const TaskCard({
    super.key,
    required this.task,
    this.startTime,
    this.endTime,
    this.showDueDate = false,
    required this.isBlocked,
    this.fragmentIndex,
    this.fragmentTotal,
    required this.onToggleCompleted,
    this.onUndoOccurrenceUnit,
    this.onSkipOccurrence,
    required this.onTap,
  });

  final TaskItem task;
  final DateTime? startTime;
  final DateTime? endTime;
  final bool showDueDate;
  final bool isBlocked;
  final int? fragmentIndex;
  final int? fragmentTotal;
  final ValueChanged<bool> onToggleCompleted;
  final VoidCallback? onUndoOccurrenceUnit;
  final VoidCallback? onSkipOccurrence;
  final VoidCallback onTap;

  bool get _isFragmented => fragmentTotal != null && fragmentTotal! > 1;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final timeFormat = DateFormat.jm();
    final dueDateFormat = DateFormat.yMMMd();

    final isResolved = task.isCompleted || task.isSkippedOrDisregarded;
    final double opacity = isResolved ? 0.4 : (isBlocked ? 0.5 : 1.0);

    Widget card = Container(
      margin: const EdgeInsets.symmetric(vertical: AppTheme.spacingXs),
      decoration: BoxDecoration(
        color: colorScheme.surface,
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        border: Border.all(
          color: _isFragmented
              ? colorScheme.primary
              : colorScheme.outlineVariant,
          width: _isFragmented ? 2 : 1,
        ),
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
                  value: isResolved,
                  onChanged:
                      isBlocked ||
                          task.isSkippedOrDisregarded ||
                          (task.recurrenceRule != null &&
                              !task.isRecurringOccurrence)
                      ? null
                      : onToggleCompleted,
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
                              style: Theme.of(context).textTheme.titleSmall
                                  ?.copyWith(
                                    fontWeight: FontWeight.w600,
                                    decoration: isResolved
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
                      if (task.isRecurringOccurrence) ...[
                        const SizedBox(height: 2),
                        Text(
                          'Occurrence ${dueDateFormat.format(task.occurrenceDate!)}',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                      ] else if (task.recurrenceRule != null &&
                          task.isCompleted) ...[
                        const SizedBox(height: 2),
                        Text(
                          'Recurring series completed',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                      ] else if (showDueDate) ...[
                        const SizedBox(height: 2),
                        Text(
                          task.dueDate == null
                              ? 'No due date'
                              : 'Due ${dueDateFormat.format(task.dueDate!)}',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                      ],
                      if (startTime != null && endTime != null) ...[
                        const SizedBox(height: 2),
                        Text(
                          '${timeFormat.format(startTime!)} \u2013 ${timeFormat.format(endTime!)}',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                      ],
                      if (task.link.isNotEmpty) ...[
                        const SizedBox(height: AppTheme.spacingXs),
                        InkWell(
                          onTap: () => openExternalLink(context, task.link),
                          child: Row(
                            children: [
                              Icon(
                                Icons.open_in_new,
                                size: 14,
                                color: colorScheme.primary,
                              ),
                              const SizedBox(width: AppTheme.spacingXs),
                              Expanded(
                                child: Text(
                                  task.link,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: Theme.of(context).textTheme.bodySmall
                                      ?.copyWith(
                                        color: colorScheme.primary,
                                        decoration: TextDecoration.underline,
                                      ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                      if (task.isRecurringOccurrence) ...[
                        const SizedBox(height: AppTheme.spacingXs),
                        Text(
                          '${task.completionCount}/${task.requiredCompletions} completions',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                        const SizedBox(height: 2),
                        LinearProgressIndicator(
                          value: task.completionProgress
                              .clamp(0.0, 1.0)
                              .toDouble(),
                          minHeight: 4,
                        ),
                      ],
                      if (task.showMissedIndicator &&
                          task.hasMissedOccurrence) ...[
                        const SizedBox(height: AppTheme.spacingXs),
                        Text(
                          'Missed occurrence',
                          style: Theme.of(context).textTheme.labelSmall
                              ?.copyWith(color: colorScheme.error),
                        ),
                      ],
                      if (task.isRecurringOccurrence && task.trackStreak) ...[
                        const SizedBox(height: AppTheme.spacingXs),
                        Text(
                          'Streak ${task.currentStreak} \u2022 Best ${task.bestStreak}',
                          style: Theme.of(context).textTheme.labelSmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                      ],
                      if (task.isSkippedOrDisregarded) ...[
                        const SizedBox(height: AppTheme.spacingXs),
                        Text(
                          task.occurrenceStatus!,
                          style: Theme.of(context).textTheme.labelSmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                      ],
                      if (task.isRecurringOccurrence &&
                          !isResolved &&
                          (onUndoOccurrenceUnit != null ||
                              onSkipOccurrence != null)) ...[
                        Wrap(
                          spacing: AppTheme.spacingSm,
                          children: [
                            if (task.completionCount > 0)
                              TextButton.icon(
                                onPressed: onUndoOccurrenceUnit,
                                icon: const Icon(Icons.undo, size: 16),
                                label: const Text('Undo last step'),
                                style: TextButton.styleFrom(
                                  visualDensity: VisualDensity.compact,
                                  padding: EdgeInsets.zero,
                                ),
                              ),
                            if (onSkipOccurrence != null)
                              TextButton.icon(
                                onPressed: onSkipOccurrence,
                                icon: const Icon(Icons.skip_next, size: 16),
                                label: const Text('Skip occurrence'),
                                style: TextButton.styleFrom(
                                  visualDensity: VisualDensity.compact,
                                  padding: EdgeInsets.zero,
                                ),
                              ),
                          ],
                        ),
                      ],
                      if (_isFragmented) ...[
                        const SizedBox(height: 4),
                        Tooltip(
                          message:
                              'Scheduled part ${fragmentIndex ?? 1} of '
                              '$fragmentTotal. Parts may continue on another '
                              'day or after an event.',
                          child: _FragmentBadge(
                            index: fragmentIndex ?? 1,
                            total: fragmentTotal!,
                          ),
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
        'Part $index of $total',
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
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
