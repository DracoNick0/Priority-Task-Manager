import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/schedule_models.dart';
import '../../models/task_item.dart';
import '../../providers/event_providers.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../../providers/view_provider.dart';
import '../theme/app_theme.dart';
import 'day_column.dart';
import 'event_card.dart';
import 'task_card.dart';

/// The Center Stage: a horizontally scrolling pipeline of Daily Columns
/// (Today, Tomorrow, ... , Unscheduled), each holding scheduled task cards
/// and fixed event cards for that day.
class CenterStage extends ConsumerWidget {
  const CenterStage({
    super.key,
    required this.showHamburger,
    required this.onOpenLeftRail,
    required this.showInspectorToggle,
    required this.onOpenInspector,
  });

  final bool showHamburger;
  final VoidCallback onOpenLeftRail;
  final bool showInspectorToggle;
  final VoidCallback onOpenInspector;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final activeListId = ref.watch(activeListIdProvider);
    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      children: [
        _buildHeader(context, ref, activeListId),
        const Divider(height: 1),
        Expanded(
          child: activeListId == null
              ? Center(
                  child: Text(
                    'Select a list from the left rail to get started.',
                    style: TextStyle(color: colorScheme.onSurfaceVariant),
                  ),
                )
              : _Pipeline(listId: activeListId),
        ),
      ],
    );
  }

  Widget _buildHeader(
    BuildContext context,
    WidgetRef ref,
    String? activeListId,
  ) {
    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
    final activeList = lists
        .where((list) => list.id == activeListId)
        .firstOrNull;

    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppTheme.spacingMd,
        vertical: AppTheme.spacingSm,
      ),
      child: Row(
        children: [
          if (showHamburger)
            IconButton(
              icon: const Icon(Icons.menu),
              tooltip: 'Show navigation',
              onPressed: onOpenLeftRail,
            ),
          Expanded(
            child: Text(
              activeList?.name ?? 'Priority Task Manager',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.bold,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          if (showInspectorToggle)
            IconButton(
              icon: const Icon(Icons.info_outline),
              tooltip: 'Show inspector',
              onPressed: onOpenInspector,
            ),
        ],
      ),
    );
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}

class _Pipeline extends ConsumerWidget {
  const _Pipeline({required this.listId});

  final String listId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tasksAsync = ref.watch(tasksProvider(listId));
    final scheduleAsync = ref.watch(scheduleProvider);
    final events = ref.watch(eventsProvider(listId));

    if (tasksAsync.isLoading || scheduleAsync.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (tasksAsync.hasError) {
      return Center(child: Text('Could not load tasks: ${tasksAsync.error}'));
    }
    if (scheduleAsync.hasError) {
      return Center(
        child: Text('Could not compute schedule: ${scheduleAsync.error}'),
      );
    }

    final tasks = tasksAsync.value ?? const <TaskItem>[];
    final schedule = scheduleAsync.value ?? DailySchedule.empty();
    final tasksById = {for (final task in tasks) task.id: task};

    // Fragment counts: how many scheduled chunks share the same task id.
    final allScheduled = [...schedule.todayTasks, ...schedule.futureTasks];
    final fragmentGroups = <String, List<ScheduledTask>>{};
    for (final scheduled in allScheduled) {
      (fragmentGroups[scheduled.id] ??= []).add(scheduled);
    }
    for (final group in fragmentGroups.values) {
      group.sort((a, b) => a.startTime.compareTo(b.startTime));
    }

    bool isBlocked(TaskItem task) => task.dependencies.any((depId) {
      final dependency = tasksById[depId];
      return dependency != null && !dependency.isCompleted;
    });

    Widget buildTaskCard(ScheduledTask scheduled) {
      final task = tasksById[scheduled.id];
      if (task == null) return const SizedBox.shrink();
      final group = fragmentGroups[scheduled.id]!;
      final fragmentIndex = group.indexOf(scheduled) + 1;
      return TaskCard(
        key: ValueKey('${scheduled.id}-${scheduled.startTime}'),
        task: task,
        startTime: scheduled.startTime,
        endTime: scheduled.endTime,
        isBlocked: isBlocked(task),
        fragmentIndex: fragmentIndex,
        fragmentTotal: group.length,
        onToggleCompleted: (value) => ref
            .read(tasksProvider(listId).notifier)
            .setCompleted(task.id, value),
        onTap: () => ref.read(selectedInspectorProvider.notifier).state =
            InspectorTarget(kind: InspectorKind.task, id: task.id),
      );
    }

    Widget buildPlainTaskCard(TaskItem task) {
      return TaskCard(
        key: ValueKey('plain-${task.id}'),
        task: task,
        isBlocked: isBlocked(task),
        onToggleCompleted: (value) => ref
            .read(tasksProvider(listId).notifier)
            .setCompleted(task.id, value),
        onTap: () => ref.read(selectedInspectorProvider.notifier).state =
            InspectorTarget(kind: InspectorKind.task, id: task.id),
      );
    }

    Widget buildEventCard(FixedEvent event) {
      return EventCard(
        key: ValueKey(event.id),
        event: event,
        onTap: () => ref.read(selectedInspectorProvider.notifier).state =
            InspectorTarget(kind: InspectorKind.event, id: event.id),
      );
    }

    void openNewTask(DateTime day) {
      ref.read(selectedInspectorProvider.notifier).state =
          const InspectorTarget(kind: InspectorKind.task, id: null);
    }

    void openNewEvent(DateTime day) {
      ref.read(selectedInspectorProvider.notifier).state =
          const InspectorTarget(kind: InspectorKind.event, id: null);
    }

    void cleanupDay(List<TaskItem> completedInDay) {
      final notifier = ref.read(tasksProvider(listId).notifier);
      for (final task in completedInDay) {
        notifier.deleteTask(task.id);
      }
    }

    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    // Completed tasks aren't sent through the scheduler, so surface them
    // directly in Today's column so they remain visible (dimmed/struck-through)
    // instead of disappearing once checked off.
    final completedTasks = tasks.where((task) => task.isCompleted).toList();

    bool isSameDay(DateTime a, DateTime b) =>
        a.year == b.year && a.month == b.month && a.day == b.day;

    final todayEvents = events
        .where((event) => isSameDay(event.startTime, today))
        .toList();

    final todayCards = <Widget>[
      ...schedule.todayTasks.map(buildTaskCard),
      ...completedTasks.map(buildPlainTaskCard),
      ...todayEvents.map(buildEventCard),
    ];

    // Bucket future scheduled tasks and events by calendar day.
    final futureByDay = <DateTime, List<ScheduledTask>>{};
    for (final scheduled in schedule.futureTasks) {
      final day = DateTime(
        scheduled.startTime.year,
        scheduled.startTime.month,
        scheduled.startTime.day,
      );
      (futureByDay[day] ??= []).add(scheduled);
    }
    final futureDays = futureByDay.keys.toList()..sort();

    final unscheduledTasks = schedule.unscheduledTaskIds
        .map((id) => tasksById[id])
        .whereType<TaskItem>()
        .toList();

    final columns = <DayColumn>[
      DayColumn(
        title: 'Today',
        subtitle: _formatDate(today),
        cards: todayCards,
        onAddTask: () => openNewTask(today),
        onAddEvent: () => openNewEvent(today),
        onCleanup: () => cleanupDay(completedTasks),
      ),
      for (final day in futureDays)
        DayColumn(
          title: isSameDay(day, today.add(const Duration(days: 1)))
              ? 'Tomorrow'
              : _formatWeekday(day),
          subtitle: _formatDate(day),
          cards: [
            ...futureByDay[day]!.map(buildTaskCard),
            ...events
                .where((event) => isSameDay(event.startTime, day))
                .map(buildEventCard),
          ],
          onAddTask: () => openNewTask(day),
          onAddEvent: () => openNewEvent(day),
          onCleanup: () {},
        ),
      DayColumn(
        title: 'Unscheduled',
        subtitle: '${unscheduledTasks.length} task(s)',
        cards: unscheduledTasks.map(buildPlainTaskCard).toList(),
        onAddTask: () => openNewTask(today),
        onAddEvent: () => openNewEvent(today),
        onCleanup: () {},
      ),
    ];

    return ListView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: columns,
    );
  }

  static String _formatDate(DateTime date) =>
      '${date.month}/${date.day}/${date.year}';

  static const _weekdays = [
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ];

  static String _formatWeekday(DateTime date) => _weekdays[date.weekday - 1];
}
