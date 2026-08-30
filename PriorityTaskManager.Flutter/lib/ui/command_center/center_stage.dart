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
    final scheduleAsync = ref.watch(scheduleProvider);
    final schedule = scheduleAsync.asData?.value;

    return Column(
      children: [
        _buildHeader(context, ref, activeListId),
        const Divider(height: 1),
        if (activeListId != null &&
            schedule != null &&
            schedule.leastSlackTask != 'None')
          _buildLeastSlackBar(context, schedule),
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

    void openNewTask() {
      ref.read(selectedInspectorProvider.notifier).state =
          const InspectorTarget(kind: InspectorKind.task, id: null);
    }

    void openNewEvent() {
      ref.read(selectedInspectorProvider.notifier).state =
          const InspectorTarget(kind: InspectorKind.event, id: null);
    }

    void cleanupCompleted() {
      if (activeListId == null) return;
      final tasks =
          ref.read(tasksProvider(activeListId)).asData?.value ??
          const <TaskItem>[];
      final notifier = ref.read(tasksProvider(activeListId).notifier);
      for (final task in tasks.where((task) => task.isCompleted)) {
        notifier.deleteTask(task.id);
      }
    }

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppTheme.spacingMd),
      child: SizedBox(
        height: AppTheme.paneHeaderHeight,
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
                style: Theme.of(
                  context,
                ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            IconButton(
              icon: const Icon(Icons.add_task),
              tooltip: 'Add Task',
              onPressed: activeListId == null ? null : openNewTask,
            ),
            IconButton(
              icon: const Icon(Icons.event),
              tooltip: 'Add Event',
              onPressed: activeListId == null ? null : openNewEvent,
            ),
            IconButton(
              icon: const Icon(Icons.cleaning_services_outlined),
              tooltip: 'Cleanup completed tasks',
              onPressed: activeListId == null ? null : cleanupCompleted,
            ),
            if (showInspectorToggle)
              IconButton(
                icon: const Icon(Icons.info_outline),
                tooltip: 'Show inspector',
                onPressed: onOpenInspector,
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildLeastSlackBar(BuildContext context, DailySchedule schedule) {
    final colorScheme = Theme.of(context).colorScheme;
    final bodySmall = Theme.of(context).textTheme.bodySmall;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(
        horizontal: AppTheme.spacingMd,
        vertical: AppTheme.spacingXs,
      ),
      color: colorScheme.surfaceContainerLow,
      child: Row(
        children: [
          Icon(
            Icons.hourglass_bottom,
            size: 16,
            color: colorScheme.onSurfaceVariant,
          ),
          const SizedBox(width: AppTheme.spacingXs),
          Expanded(
            child: Text.rich(
              TextSpan(
                children: [
                  TextSpan(
                    text: 'Least slack: ',
                    style: bodySmall?.copyWith(fontWeight: FontWeight.bold),
                  ),
                  TextSpan(
                    text: '"${schedule.leastSlackTask}"  ',
                    style: bodySmall,
                  ),
                  TextSpan(
                    text:
                        'Realistic ${schedule.realisticSlack} · Actual ${schedule.actualSlack}',
                    style: bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
              overflow: TextOverflow.ellipsis,
              maxLines: 1,
            ),
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
    final events = ref.watch(eventsProvider(listId)).asData?.value ?? const [];

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

    // The list's work window (mirrors ApiScheduleRepository's default
    // profile) used to derive each day's "Free Time" remainder.
    const workStartHour = 9;
    const workEndHour = 17;
    bool isWorkDay(DateTime day) =>
        day.weekday >= DateTime.monday && day.weekday <= DateTime.friday;

    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final todayWorkEnd = DateTime(
      today.year,
      today.month,
      today.day,
      workEndHour,
    );
    final isPastWorkEnd = now.isAfter(todayWorkEnd);

    /// Minutes remaining in [day]'s work window: the full window for future
    /// days, or only what's left after [now] when [day] is today.
    int windowMinutesFor(DateTime day) {
      if (!isWorkDay(day)) return 0;
      final isToday =
          day.year == today.year &&
          day.month == today.month &&
          day.day == today.day;
      final workStart = DateTime(day.year, day.month, day.day, workStartHour);
      final workEnd = DateTime(day.year, day.month, day.day, workEndHour);
      final effectiveStart = (isToday && now.isAfter(workStart))
          ? now
          : workStart;
      if (!effectiveStart.isBefore(workEnd)) return 0;
      return workEnd.difference(effectiveStart).inMinutes;
    }

    String freeTimeLabel(
      DateTime day,
      List<ScheduledTask> dayTasks,
      List<FixedEvent> dayEvents,
    ) {
      final windowMinutes = windowMinutesFor(day);
      final usedMinutes =
          dayTasks.fold<double>(0, (sum, t) => sum + t.chunkHours * 60) +
          dayEvents.fold<double>(
            0,
            (sum, e) => sum + e.endTime.difference(e.startTime).inMinutes,
          );
      final remaining = (windowMinutes - usedMinutes).clamp(0, windowMinutes);
      final hours = remaining ~/ 60;
      final minutes = (remaining % 60).round();
      return 'Free Time: ${hours}h ${minutes}m';
    }

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
      if (todayCards.isNotEmpty && !isPastWorkEnd)
        DayColumn(
          title: 'Today',
          subtitle: _formatDate(today),
          freeTimeLabel: freeTimeLabel(today, schedule.todayTasks, todayEvents),
          cards: todayCards,
        ),
      for (final day in futureDays)
        if (futureByDay[day]!.isNotEmpty ||
            events.any((event) => isSameDay(event.startTime, day)))
          DayColumn(
            title: isSameDay(day, today.add(const Duration(days: 1)))
                ? 'Tomorrow'
                : _formatWeekday(day),
            subtitle: _formatDate(day),
            freeTimeLabel: freeTimeLabel(
              day,
              futureByDay[day]!,
              events.where((event) => isSameDay(event.startTime, day)).toList(),
            ),
            cards: [
              ...futureByDay[day]!.map(buildTaskCard),
              ...events
                  .where((event) => isSameDay(event.startTime, day))
                  .map(buildEventCard),
            ],
          ),
      DayColumn(
        title: 'Unscheduled',
        subtitle: '${unscheduledTasks.length} task(s)',
        cards: unscheduledTasks.map(buildPlainTaskCard).toList(),
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
