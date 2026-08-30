import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/task_item.dart';
import '../../providers/event_providers.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../theme/app_theme.dart';
import 'event_card.dart';
import 'task_card.dart';

/// Ways [GuestTaskList] can order tasks. Scheduling (which orders tasks by
/// computed placement) is an online-exclusive, Subscription-gated capability
/// (see docs/VISION.md), so Guests instead get a plain, user-chosen sort.
enum GuestTaskSort { importance, dueDate, alphabetical }

const Map<GuestTaskSort, String> guestTaskSortLabels = {
  GuestTaskSort.importance: 'Importance',
  GuestTaskSort.dueDate: 'Due Date',
  GuestTaskSort.alphabetical: 'Alphabetical',
};

final guestTaskSortProvider = StateProvider<GuestTaskSort>(
  (ref) => GuestTaskSort.importance,
);

/// The Center Stage body shown to Guests (no account): since scheduling
/// requires an authenticated, Subscription-tier account (issue #41), Guests
/// see a plain, sortable list of their tasks instead of the computed Daily
/// Column pipeline. Fixed events are listed separately above the tasks,
/// sorted by start time, since they are not placed against a schedule here.
class GuestTaskList extends ConsumerWidget {
  const GuestTaskList({super.key, required this.listId});

  final String listId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tasksAsync = ref.watch(tasksProvider(listId));
    final events = ref.watch(eventsProvider(listId)).asData?.value ?? const [];
    final sort = ref.watch(guestTaskSortProvider);
    final colorScheme = Theme.of(context).colorScheme;

    if (tasksAsync.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (tasksAsync.hasError) {
      return Center(child: Text('Could not load tasks: ${tasksAsync.error}'));
    }

    final tasks = tasksAsync.value ?? const <TaskItem>[];
    final incompleteTasks = tasks.where((t) => !t.isCompleted).toList()
      ..sort(_comparatorFor(sort));
    final completedTasks = tasks.where((t) => t.isCompleted).toList()
      ..sort(_comparatorFor(sort));

    final sortedEvents = [...events]
      ..sort((a, b) => a.startTime.compareTo(b.startTime));

    Widget buildTaskCard(TaskItem task) {
      final isBlocked = task.dependencies.any((depId) {
        final dependency = tasks.where((t) => t.id == depId).firstOrNull;
        return dependency != null && !dependency.isCompleted;
      });
      return TaskCard(
        key: ValueKey(task.id),
        task: task,
        isBlocked: isBlocked,
        onToggleCompleted: (value) => ref
            .read(tasksProvider(listId).notifier)
            .setCompleted(task.id, value),
        onTap: () => ref.read(selectedInspectorProvider.notifier).state =
            InspectorTarget(kind: InspectorKind.task, id: task.id),
      );
    }

    return Column(
      children: [
        Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(
            horizontal: AppTheme.spacingMd,
            vertical: AppTheme.spacingXs,
          ),
          color: colorScheme.surfaceContainerLow,
          child: Row(
            children: [
              Icon(
                Icons.info_outline,
                size: 16,
                color: colorScheme.onSurfaceVariant,
              ),
              const SizedBox(width: AppTheme.spacingXs),
              Expanded(
                child: Text(
                  'Log in to compute a scheduled plan. Showing your tasks sorted by:',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: colorScheme.onSurfaceVariant,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              DropdownButton<GuestTaskSort>(
                value: sort,
                underline: const SizedBox.shrink(),
                items: [
                  for (final option in GuestTaskSort.values)
                    DropdownMenuItem(
                      value: option,
                      child: Text(guestTaskSortLabels[option]!),
                    ),
                ],
                onChanged: (value) {
                  if (value != null) {
                    ref.read(guestTaskSortProvider.notifier).state = value;
                  }
                },
              ),
            ],
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(AppTheme.spacingMd),
            children: [
              for (final event in sortedEvents)
                EventCard(
                  key: ValueKey(event.id),
                  event: event,
                  onTap: () =>
                      ref.read(selectedInspectorProvider.notifier).state =
                          InspectorTarget(
                            kind: InspectorKind.event,
                            id: event.id,
                          ),
                ),
              for (final task in incompleteTasks) buildTaskCard(task),
              for (final task in completedTasks) buildTaskCard(task),
            ],
          ),
        ),
      ],
    );
  }

  static int Function(TaskItem, TaskItem) _comparatorFor(GuestTaskSort sort) {
    switch (sort) {
      case GuestTaskSort.importance:
        return (a, b) => b.importance.compareTo(a.importance);
      case GuestTaskSort.dueDate:
        return (a, b) {
          if (a.dueDate == null && b.dueDate == null) return 0;
          if (a.dueDate == null) return 1;
          if (b.dueDate == null) return -1;
          return a.dueDate!.compareTo(b.dueDate!);
        };
      case GuestTaskSort.alphabetical:
        return (a, b) =>
            a.title.toLowerCase().compareTo(b.title.toLowerCase());
    }
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
