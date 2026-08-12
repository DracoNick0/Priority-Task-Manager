import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// A single column in the Center Stage pipeline (e.g. "Today", "Tomorrow",
/// or "Unscheduled"). Has its own sticky header with quick actions and an
/// independently scrollable body of task/event cards.
class DayColumn extends StatelessWidget {
  const DayColumn({
    super.key,
    required this.title,
    required this.subtitle,
    required this.cards,
    this.onAddTask,
    this.onAddEvent,
    this.onCleanup,
  });

  static const double width = 320;

  final String title;
  final String? subtitle;
  final List<Widget> cards;
  final VoidCallback? onAddTask;
  final VoidCallback? onAddEvent;
  final VoidCallback? onCleanup;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      width: width,
      margin: const EdgeInsets.only(right: AppTheme.spacingMd),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerLow,
        borderRadius: BorderRadius.circular(AppTheme.radiusLg),
        border: Border.all(color: colorScheme.outlineVariant),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppTheme.spacingMd,
              AppTheme.spacingMd,
              AppTheme.spacingSm,
              AppTheme.spacingSm,
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.bold),
                        overflow: TextOverflow.ellipsis,
                      ),
                      if (subtitle != null)
                        Text(
                          subtitle!,
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: colorScheme.onSurfaceVariant),
                        ),
                    ],
                  ),
                ),
                if (onAddTask != null)
                  IconButton(
                    icon: const Icon(Icons.add_task, size: 18),
                    tooltip: 'Add Task',
                    onPressed: onAddTask,
                  ),
                if (onAddEvent != null)
                  IconButton(
                    icon: const Icon(Icons.event, size: 18),
                    tooltip: 'Add Event',
                    onPressed: onAddEvent,
                  ),
                if (onCleanup != null)
                  IconButton(
                    icon: const Icon(
                      Icons.cleaning_services_outlined,
                      size: 18,
                    ),
                    tooltip: 'Cleanup',
                    onPressed: onCleanup,
                  ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: cards.isEmpty
                ? Center(
                    child: Padding(
                      padding: const EdgeInsets.all(AppTheme.spacingLg),
                      child: Text(
                        'Nothing here',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ),
                  )
                : ListView(
                    padding: const EdgeInsets.all(AppTheme.spacingSm),
                    children: cards,
                  ),
          ),
        ],
      ),
    );
  }
}
