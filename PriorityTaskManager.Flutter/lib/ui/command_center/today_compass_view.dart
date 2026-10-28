import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/schedule_models.dart';
import '../../models/task_item.dart';
import '../../models/task_list.dart';
import '../../providers/engine_status_provider.dart';
import '../../providers/event_providers.dart';
import '../../providers/selection_provider.dart';
import '../../providers/session_provider.dart';
import '../../providers/task_providers.dart';
import '../../providers/view_provider.dart';
import '../../utils/iterable_extensions.dart';
import '../theme/app_theme.dart';
import 'empty_schedule_hero.dart';
import 'fast_capture_palette.dart';
import 'workload_capacity_meter.dart';

/// The Today Execution Compass View (resolves Issue #84).
/// Provides a fluid, priority-ranked daily plan that answers:
/// "What is the best order to do my work today, and what fits in my remaining hours?"
class TodayCompassView extends ConsumerWidget {
  const TodayCompassView({super.key, required this.listId});

  final String listId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final now = ref.watch(engineClockProvider).asData?.value ?? DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final isAuthenticated =
        ref.watch(sessionControllerProvider).asData?.value.status ==
        SessionStatus.authenticated;

    final tasksAsync = ref.watch(tasksProvider(listId));
    final scheduleAsync = ref.watch(scheduleProvider);
    final eventSeriesAsync = ref.watch(eventsProvider(listId));
    final eventOccurrencesAsync = ref.watch(
      eventOccurrencesProvider((listId, today)),
    );

    if (tasksAsync.isLoading && !tasksAsync.hasValue) {
      return const Center(child: CircularProgressIndicator());
    }
    if (eventOccurrencesAsync.hasError && !eventOccurrencesAsync.hasValue) {
      return Center(
        child: Text(
          'Could not load event occurrences: ${eventOccurrencesAsync.error}',
        ),
      );
    }

    final tasks = tasksAsync.asData?.value ?? const <TaskItem>[];
    final eventSeries = eventSeriesAsync.asData?.value ?? const <FixedEvent>[];
    final events = eventOccurrencesAsync.asData?.value ?? const <FixedEvent>[];
    final schedule = scheduleAsync.asData?.value ?? DailySchedule.empty();

    if (!tasksAsync.isLoading &&
        !eventSeriesAsync.isLoading &&
        tasks.isEmpty &&
        eventSeries.isEmpty) {
      return EmptyScheduleHero(listId: listId);
    }

    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
    final activeList = lists.where((l) => l.id == listId).firstOrNull;

    final workStartMinutes = activeList?.workStartMinutes ?? 9 * 60;
    final workEndMinutes = activeList?.workEndMinutes ?? 17 * 60;
    final workEndHour = workEndMinutes ~/ 60;
    final workEndMinute = workEndMinutes % 60;
    final workEndTime = DateTime(
      today.year,
      today.month,
      today.day,
      workEndHour,
      workEndMinute,
    );

    final isPastWorkEnd = now.isAfter(workEndTime);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 880),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Header
              _buildHeader(context, ref, now, activeList),
              const SizedBox(height: AppTheme.spacingMd),

              // Capacity and Workload Bar
              _buildCapacityBar(
                context,
                ref,
                now,
                tasks,
                events,
                schedule,
                workStartMinutes,
                workEndMinutes,
                isPastWorkEnd,
              ),
              const SizedBox(height: AppTheme.spacingMd),

              // Evening accomplishments or active execution sequence
              if (isPastWorkEnd)
                _buildEveningWrapUp(context, ref, today, tasks, workEndTime)
              else
                _buildExecutionSequence(
                  context,
                  ref,
                  today,
                  now,
                  tasks,
                  events,
                  schedule,
                  workEndTime,
                  isAuthenticated,
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader(
    BuildContext context,
    WidgetRef ref,
    DateTime now,
    TaskList? activeList,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final dateStr = _formatDateHeading(now);
    final timeStr = _formatTime(now);

    return Card(
      elevation: 0,
      color: colorScheme.surfaceContainerLowest,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        side: BorderSide(color: colorScheme.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingMd),
        child: Row(
          children: [
            Icon(
              Icons.compass_calibration,
              color: colorScheme.primary,
              size: 28,
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'TODAY - $dateStr',
                    style: textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      letterSpacing: 0.5,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'Current simulated time: $timeStr · ${activeList?.name ?? 'Active List'}',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
            FilledButton.tonalIcon(
              onPressed: () => showFastCapturePalette(context),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('Quick Task'),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            OutlinedButton.icon(
              onPressed: () {
                ref.read(selectedInspectorProvider.notifier).state =
                    InspectorTarget(kind: InspectorKind.list, id: listId);
              },
              icon: const Icon(Icons.schedule, size: 18),
              label: const Text('Hours'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCapacityBar(
    BuildContext context,
    WidgetRef ref,
    DateTime now,
    List<TaskItem> tasks,
    List<FixedEvent> events,
    DailySchedule schedule,
    int workStartMinutes,
    int workEndMinutes,
    bool isPastWorkEnd,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final today = DateTime(now.year, now.month, now.day);
    bool isSameDay(DateTime a, DateTime b) =>
        a.year == b.year && a.month == b.month && a.day == b.day;

    final todayEvents = events
        .where((e) => isSameDay(e.startTime, today))
        .toList();
    final workdayStart = today.add(Duration(minutes: workStartMinutes));
    final workdayEnd = today.add(Duration(minutes: workEndMinutes));

    double plannedWorkHours;
    if (schedule.todayTasks.isNotEmpty) {
      plannedWorkHours = scheduledTaskHoursWithinWorkWindow(
        schedule.todayTasks,
        windowStart: workdayStart,
        windowEnd: workdayEnd,
      );
    } else {
      final incompleteToday = tasks.where(
        (t) =>
            !t.isCompleted &&
            (t.dueDate == null || isSameDay(t.dueDate!, today)),
      );
      plannedWorkHours = incompleteToday.fold<double>(
        0.0,
        (sum, t) => sum + (t.estimatedDurationMinutes / 60.0),
      );
    }

    final meetingHours = eventHoursWithinWorkWindow(
      todayEvents,
      windowStart: workdayStart,
      windowEnd: workdayEnd,
    );

    final totalCapacityHours = max(
      0.0,
      workdayEnd.difference(workdayStart).inSeconds / 3600.0,
    ).toDouble();
    final totalPlanned = plannedWorkHours + meetingHours;
    final freeBufferHours = max(0.0, totalCapacityHours - totalPlanned);
    final capacityRatio = totalCapacityHours > 0
        ? (totalPlanned / totalCapacityHours)
        : 1.0;
    final capacityPercent = (capacityRatio * 100).round();

    Color meterColor;
    if (capacityPercent < 85) {
      meterColor = Colors.green;
    } else if (capacityPercent <= 100) {
      meterColor = Colors.amber;
    } else {
      meterColor = Colors.red;
    }

    // Workday remaining calculation
    final currentMinutes = now.hour * 60 + now.minute;
    final remainingMinutes = max(
      0,
      workEndMinutes - max(workStartMinutes, currentMinutes),
    );
    final remainingWorkHours = remainingMinutes / 60.0;
    final isOverloaded =
        !isPastWorkEnd && (plannedWorkHours > remainingWorkHours);

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        side: BorderSide(
          color: isOverloaded
              ? Colors.amber.shade700
              : colorScheme.outlineVariant,
          width: isOverloaded ? 1.5 : 1.0,
        ),
      ),
      color: colorScheme.surfaceContainerLowest,
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingMd),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text.rich(
                    TextSpan(
                      children: [
                        const TextSpan(
                          text: "TODAY'S WORKLOAD: ",
                          style: TextStyle(fontWeight: FontWeight.bold),
                        ),
                        TextSpan(
                          text:
                              '${plannedWorkHours.toStringAsFixed(1)}h Planned Work · ',
                        ),
                        TextSpan(
                          text:
                              '${meetingHours.toStringAsFixed(1)}h Meetings · ',
                        ),
                        TextSpan(
                          text:
                              '${freeBufferHours.toStringAsFixed(1)}h Free Buffer',
                          style: TextStyle(
                            color: freeBufferHours > 0.5
                                ? Colors.green.shade700
                                : Colors.amber.shade800,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                    style: textTheme.bodySmall,
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 3,
                  ),
                  decoration: BoxDecoration(
                    color: meterColor.withAlpha(35),
                    borderRadius: BorderRadius.circular(AppTheme.radiusSm),
                    border: Border.all(color: meterColor),
                  ),
                  child: Text(
                    'CAPACITY: $capacityPercent%',
                    style: TextStyle(
                      color: meterColor,
                      fontWeight: FontWeight.bold,
                      fontSize: 12,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTheme.spacingSm),
            SegmentedCapacityMeter(
              key: const ValueKey('today-capacity-meter'),
              eventHours: meetingHours,
              taskHours: plannedWorkHours,
              capacityHours: totalCapacityHours,
              loadColor: meterColor,
              height: 8,
            ),
            if (isOverloaded) ...[
              const SizedBox(height: AppTheme.spacingMd),
              Container(
                padding: const EdgeInsets.all(AppTheme.spacingSm),
                decoration: BoxDecoration(
                  color: Colors.amber.withAlpha(30),
                  borderRadius: BorderRadius.circular(AppTheme.radiusSm),
                  border: Border.all(color: Colors.amber.shade600),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.warning_amber_rounded,
                          size: 18,
                          color: Colors.amber,
                        ),
                        const SizedBox(width: AppTheme.spacingXs),
                        Text(
                          'Workload Notice: Remaining planned work (${plannedWorkHours.toStringAsFixed(1)}h) exceeds remaining workday (${remainingWorkHours.toStringAsFixed(1)}h)',
                          style: textTheme.bodySmall?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: AppTheme.spacingSm),
                    Wrap(
                      spacing: AppTheme.spacingSm,
                      runSpacing: AppTheme.spacingXs,
                      children: [
                        OutlinedButton.icon(
                          onPressed: () =>
                              _pushNonUrgentToTomorrow(context, ref, tasks),
                          icon: const Icon(Icons.arrow_forward, size: 16),
                          label: const Text(
                            'Push non-urgent tasks to tomorrow',
                          ),
                          style: OutlinedButton.styleFrom(
                            visualDensity: VisualDensity.compact,
                          ),
                        ),
                        OutlinedButton.icon(
                          onPressed: () =>
                              _extendTodayHours(context, ref, workEndMinutes),
                          icon: const Icon(Icons.more_time, size: 16),
                          label: const Text("Extend today's hours by 1h"),
                          style: OutlinedButton.styleFrom(
                            visualDensity: VisualDensity.compact,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildExecutionSequence(
    BuildContext context,
    WidgetRef ref,
    DateTime today,
    DateTime now,
    List<TaskItem> tasks,
    List<FixedEvent> events,
    DailySchedule schedule,
    DateTime workEndTime,
    bool isAuthenticated,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    bool isSameDay(DateTime a, DateTime b) =>
        a.year == b.year && a.month == b.month && a.day == b.day;

    final todayEvents =
        events
            .where(
              (event) =>
                  isSameDay(event.startTime, today) &&
                  event.endTime.isAfter(now),
            )
            .toList()
          ..sort((a, b) => a.startTime.compareTo(b.startTime));

    final incompleteTasks = tasks.where((t) => !t.isCompleted).toList();

    // Map scheduled tasks to TaskItems
    final tasksById = {for (final t in tasks) t.id: t};
    for (final unscheduled in schedule.unscheduledTasks) {
      tasksById[unscheduled.id] = unscheduled;
    }

    final todayScheduledIds = schedule.todayTasks.map((t) => t.id).toSet();

    // Find highest urgency / least slack task
    TaskItem? leastSlackItem;
    if (schedule.leastSlackTask != 'None') {
      leastSlackItem = tasks
          .where((t) => t.title == schedule.leastSlackTask)
          .firstOrNull;
    }
    leastSlackItem ??=
        incompleteTasks
            .where((t) => todayScheduledIds.contains(t.id))
            .firstOrNull ??
        incompleteTasks.firstOrNull;

    // Remaining items for today / next up in queue
    final remainingTasks = incompleteTasks
        .where((t) => t.id != leastSlackItem?.id)
        .toList();

    final nextUpTasks = remainingTasks
        .where((t) => t.estimatedDurationMinutes > 45)
        .toList();
    final quickWinTasks = remainingTasks
        .where((t) => t.estimatedDurationMinutes <= 45)
        .toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (!isAuthenticated)
          Container(
            padding: const EdgeInsets.all(AppTheme.spacingSm),
            margin: const EdgeInsets.only(bottom: AppTheme.spacingMd),
            decoration: BoxDecoration(
              color: colorScheme.surfaceContainerLow,
              borderRadius: BorderRadius.circular(AppTheme.radiusSm),
              border: Border.all(color: colorScheme.outlineVariant),
            ),
            child: Row(
              children: [
                Icon(Icons.info_outline, size: 16, color: colorScheme.primary),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Text(
                    'Guest mode: Tasks sequenced by importance and deadline. Log in for automated schedule optimization.',
                    style: textTheme.bodySmall,
                  ),
                ),
              ],
            ),
          ),

        // Section 1: Highest Urgency (Least Slack)
        if (leastSlackItem != null) ...[
          _SectionHeader(
            icon: Icons.priority_high,
            title: '1. HIGHEST URGENCY (Least Slack)',
            subtitle: 'Recommended focus for immediate attention',
            accentColor: Colors.red.shade700,
          ),
          const SizedBox(height: AppTheme.spacingXs),
          _TodayTaskCard(
            task: leastSlackItem,
            isLeastSlack: true,
            slackNotice: schedule.leastSlackTask != 'None'
                ? 'Realistic ${schedule.realisticSlack} · Actual ${schedule.actualSlack}'
                : 'Urgent deadline today',
            onCompleted: () => _completeTask(context, ref, leastSlackItem!),
            onPostpone: () =>
                _postponeToTomorrow(context, ref, leastSlackItem!),
          ),
          const SizedBox(height: AppTheme.spacingLg),
        ],

        // Section: Fixed Calendar Commitments
        if (todayEvents.isNotEmpty) ...[
          const _SectionHeader(
            icon: Icons.event,
            title: 'FIXED CALENDAR COMMITMENTS',
            subtitle: 'Non-movable schedule anchors',
          ),
          const SizedBox(height: AppTheme.spacingXs),
          for (final event in todayEvents) ...[
            _TodayEventCard(event: event),
            const SizedBox(height: AppTheme.spacingSm),
          ],
          const SizedBox(height: AppTheme.spacingMd),
        ],

        // Section 2: Next Up
        if (nextUpTasks.isNotEmpty) ...[
          const _SectionHeader(
            icon: Icons.arrow_forward_rounded,
            title: '2. NEXT UP',
            subtitle: 'Substantial focus blocks scheduled today',
          ),
          const SizedBox(height: AppTheme.spacingXs),
          for (final task in nextUpTasks) ...[
            _TodayTaskCard(
              task: task,
              onCompleted: () => _completeTask(context, ref, task),
              onPostpone: () => _postponeToTomorrow(context, ref, task),
            ),
            const SizedBox(height: AppTheme.spacingSm),
          ],
          const SizedBox(height: AppTheme.spacingMd),
        ],

        // Section 3: Low Effort / Flexible (Quick Wins)
        if (quickWinTasks.isNotEmpty) ...[
          const _SectionHeader(
            icon: Icons.bolt,
            title: '3. LOW EFFORT / FLEXIBLE',
            subtitle: 'Quick tasks and flexible buffers (≤ 45m)',
          ),
          const SizedBox(height: AppTheme.spacingXs),
          for (final task in quickWinTasks) ...[
            _TodayTaskCard(
              task: task,
              onCompleted: () => _completeTask(context, ref, task),
              onPostpone: () => _postponeToTomorrow(context, ref, task),
            ),
            const SizedBox(height: AppTheme.spacingSm),
          ],
          const SizedBox(height: AppTheme.spacingMd),
        ],

        // Workday Ends Divider
        Container(
          padding: const EdgeInsets.symmetric(vertical: AppTheme.spacingMd),
          alignment: Alignment.center,
          child: Row(
            children: [
              const Expanded(child: Divider()),
              Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppTheme.spacingMd,
                ),
                child: Text(
                  'WORKDAY ENDS AT ${_formatTime(workEndTime)}',
                  style: textTheme.labelSmall?.copyWith(
                    letterSpacing: 1.2,
                    fontWeight: FontWeight.bold,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
              ),
              const Expanded(child: Divider()),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildEveningWrapUp(
    BuildContext context,
    WidgetRef ref,
    DateTime today,
    List<TaskItem> tasks,
    DateTime workEndTime,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final completedTasks = tasks.where((t) => t.isCompleted).toList();
    final incompleteTasks = tasks.where((t) => !t.isCompleted).toList();

    // Categorize unfinished tasks: Due tonight before midnight vs Due later
    final midnight = DateTime(today.year, today.month, today.day, 23, 59, 59);
    final dueTonight = incompleteTasks
        .where((t) => t.dueDate != null && t.dueDate!.isBefore(midnight))
        .toList();
    final dueLater = incompleteTasks
        .where((t) => t.dueDate == null || t.dueDate!.isAfter(midnight))
        .toList();

    final completedHours = completedTasks.fold<double>(
      0.0,
      (sum, t) => sum + (t.estimatedDurationMinutes / 60.0),
    );

    return Card(
      elevation: 0,
      color: colorScheme.surfaceContainerLowest,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        side: BorderSide(color: colorScheme.primaryContainer),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingLg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  Icons.bedtime_outlined,
                  color: colorScheme.primary,
                  size: 28,
                ),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Workday Complete — Evening Wrap-Up',
                        style: textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Text(
                        'Configured work hours ended at ${_formatTime(workEndTime)}. Review your day and set up tomorrow.',
                        style: textTheme.bodySmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const Divider(height: AppTheme.spacingXl),

            // Accomplishments
            Text(
              "TODAY'S ACCOMPLISHMENTS:",
              style: textTheme.labelSmall?.copyWith(
                fontWeight: FontWeight.bold,
                color: Colors.green.shade700,
                letterSpacing: 1.1,
              ),
            ),
            const SizedBox(height: AppTheme.spacingSm),
            Container(
              padding: const EdgeInsets.all(AppTheme.spacingMd),
              decoration: BoxDecoration(
                color: Colors.green.withAlpha(25),
                borderRadius: BorderRadius.circular(AppTheme.radiusSm),
                border: Border.all(color: Colors.green.withAlpha(80)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.task_alt, color: Colors.green, size: 22),
                  const SizedBox(width: AppTheme.spacingSm),
                  Expanded(
                    child: Text(
                      'Completed ${completedTasks.length} task(s) (${completedHours.toStringAsFixed(1)} hours of focused work)',
                      style: textTheme.bodyMedium?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            if (dueTonight.isNotEmpty) ...[
              const SizedBox(height: AppTheme.spacingLg),
              Text(
                '! DEADLINE ATTENTION (Due Tonight Before Midnight):',
                style: textTheme.labelSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: Colors.red.shade700,
                  letterSpacing: 1.1,
                ),
              ),
              const SizedBox(height: AppTheme.spacingSm),
              for (final task in dueTonight) ...[
                Container(
                  padding: const EdgeInsets.all(AppTheme.spacingMd),
                  decoration: BoxDecoration(
                    color: Colors.red.withAlpha(20),
                    borderRadius: BorderRadius.circular(AppTheme.radiusSm),
                    border: Border.all(color: Colors.red.withAlpha(80)),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        task.title,
                        style: const TextStyle(fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Effort: ~${task.estimatedDurationMinutes}m · Due: ${_formatTime(task.dueDate!)}',
                        style: textTheme.bodySmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                      const SizedBox(height: AppTheme.spacingSm),
                      Wrap(
                        spacing: AppTheme.spacingSm,
                        runSpacing: AppTheme.spacingXs,
                        children: [
                          FilledButton.tonal(
                            onPressed: () =>
                                _completeTonight(context, ref, task),
                            child: const Text('Complete Tonight (+2h Window)'),
                          ),
                          OutlinedButton(
                            onPressed: () => _completeTask(context, ref, task),
                            child: const Text('Mark Done'),
                          ),
                          TextButton(
                            onPressed: () =>
                                _postponeToTomorrow(context, ref, task),
                            child: const Text('Postpone (Late)'),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: AppTheme.spacingSm),
              ],
            ],

            if (dueLater.isNotEmpty) ...[
              const SizedBox(height: AppTheme.spacingLg),
              Text(
                'FLEXIBLE WORK (Due Tomorrow or Later):',
                style: textTheme.labelSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: colorScheme.onSurfaceVariant,
                  letterSpacing: 1.1,
                ),
              ),
              const SizedBox(height: AppTheme.spacingSm),
              for (final task in dueLater) ...[
                ListTile(
                  dense: true,
                  leading: const Icon(Icons.arrow_forward, size: 18),
                  title: Text(task.title),
                  subtitle: Text(
                    task.dueDate != null
                        ? 'Due: ${_formatDateHeading(task.dueDate!)}'
                        : 'No due date',
                  ),
                  trailing: OutlinedButton(
                    onPressed: () => _postponeToTomorrow(context, ref, task),
                    child: const Text('Push to Tomorrow'),
                  ),
                ),
              ],
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _completeTask(
    BuildContext context,
    WidgetRef ref,
    TaskItem task,
  ) async {
    final taskNotifier = ref.read(tasksProvider(listId).notifier);
    if (task.isRecurringOccurrence) {
      await taskNotifier.completeTaskOccurrence(task);
    } else {
      await taskNotifier.setCompleted(task.id, true);
    }

    if (context.mounted) {
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              'Completed "${task.title}"',
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 6),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                if (task.isRecurringOccurrence) {
                  await taskNotifier.undoTaskOccurrenceCompletion(task);
                } else {
                  await taskNotifier.setCompleted(task.id, false);
                }
              },
            ),
          ),
        );
    }
  }

  Future<void> _postponeToTomorrow(
    BuildContext context,
    WidgetRef ref,
    TaskItem task,
  ) async {
    final now = ref.read(engineClockProvider).asData?.value ?? DateTime.now();
    final tomorrow = now.add(const Duration(days: 1));
    final newDueDate = DateTime(
      tomorrow.year,
      tomorrow.month,
      tomorrow.day,
      17,
      0,
    );
    final previousDueDate = task.dueDate;

    final updated = TaskItem(
      id: task.id,
      listId: task.listId,
      title: task.title,
      description: task.description,
      dueDate: newDueDate,
      estimatedDurationMinutes: task.estimatedDurationMinutes,
      importance: task.importance,
      complexity: task.complexity,
      notBefore: task.notBefore,
      isPinned: task.isPinned,
      isDivisible: task.isDivisible,
      link: task.link,
      dependencies: task.dependencies,
      recurrenceRule: task.recurrenceRule,
      progressionMode: task.progressionMode,
      requiredCompletions: task.requiredCompletions,
      showMissedIndicator: task.showMissedIndicator,
      trackStreak: task.trackStreak,
    );

    final taskNotifier = ref.read(tasksProvider(listId).notifier);
    await taskNotifier.updateTask(updated);

    if (context.mounted) {
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              'Pushed "${task.title}" to tomorrow',
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 6),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                final restored = TaskItem(
                  id: task.id,
                  listId: task.listId,
                  title: task.title,
                  description: task.description,
                  dueDate: previousDueDate,
                  estimatedDurationMinutes: task.estimatedDurationMinutes,
                  importance: task.importance,
                  complexity: task.complexity,
                  notBefore: task.notBefore,
                  isPinned: task.isPinned,
                  isDivisible: task.isDivisible,
                  link: task.link,
                  dependencies: task.dependencies,
                  recurrenceRule: task.recurrenceRule,
                  progressionMode: task.progressionMode,
                  requiredCompletions: task.requiredCompletions,
                  showMissedIndicator: task.showMissedIndicator,
                  trackStreak: task.trackStreak,
                );
                await taskNotifier.updateTask(restored);
              },
            ),
          ),
        );
    }
  }

  Future<void> _pushNonUrgentToTomorrow(
    BuildContext context,
    WidgetRef ref,
    List<TaskItem> tasks,
  ) async {
    final now = ref.read(engineClockProvider).asData?.value ?? DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final tomorrow = now.add(const Duration(days: 1));
    final newDueDate = DateTime(
      tomorrow.year,
      tomorrow.month,
      tomorrow.day,
      17,
      0,
    );

    // Filter non-urgent tasks: importance < 8 and not due today
    final candidates = tasks
        .where(
          (t) =>
              !t.isCompleted &&
              t.importance < 8 &&
              (t.dueDate == null ||
                  !t.dueDate!.isBefore(
                    DateTime(today.year, today.month, today.day, 23, 59),
                  )),
        )
        .toList();

    if (candidates.isEmpty) return;

    final taskNotifier = ref.read(tasksProvider(listId).notifier);
    final previousDates = <String, DateTime?>{};

    for (final task in candidates) {
      previousDates[task.id] = task.dueDate;
      final updated = TaskItem(
        id: task.id,
        listId: task.listId,
        title: task.title,
        description: task.description,
        dueDate: newDueDate,
        estimatedDurationMinutes: task.estimatedDurationMinutes,
        importance: task.importance,
        complexity: task.complexity,
        notBefore: task.notBefore,
        isPinned: task.isPinned,
        isDivisible: task.isDivisible,
        link: task.link,
        dependencies: task.dependencies,
        recurrenceRule: task.recurrenceRule,
        progressionMode: task.progressionMode,
        requiredCompletions: task.requiredCompletions,
        showMissedIndicator: task.showMissedIndicator,
        trackStreak: task.trackStreak,
      );
      await taskNotifier.updateTask(updated);
    }

    if (context.mounted) {
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              'Pushed ${candidates.length} non-urgent task(s) to tomorrow',
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 8),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                for (final task in candidates) {
                  final restored = TaskItem(
                    id: task.id,
                    listId: task.listId,
                    title: task.title,
                    description: task.description,
                    dueDate: previousDates[task.id],
                    estimatedDurationMinutes: task.estimatedDurationMinutes,
                    importance: task.importance,
                    complexity: task.complexity,
                    notBefore: task.notBefore,
                    isPinned: task.isPinned,
                    isDivisible: task.isDivisible,
                    link: task.link,
                    dependencies: task.dependencies,
                    recurrenceRule: task.recurrenceRule,
                    progressionMode: task.progressionMode,
                    requiredCompletions: task.requiredCompletions,
                    showMissedIndicator: task.showMissedIndicator,
                    trackStreak: task.trackStreak,
                  );
                  await taskNotifier.updateTask(restored);
                }
              },
            ),
          ),
        );
    }
  }

  Future<void> _extendTodayHours(
    BuildContext context,
    WidgetRef ref,
    int currentWorkEndMinutes,
  ) async {
    final lists = ref.read(taskListsProvider).asData?.value ?? [];
    final activeList = lists.where((l) => l.id == listId).firstOrNull;
    if (activeList == null) return;

    final previousEnd = activeList.workEndMinutes;
    final extendedEnd = (currentWorkEndMinutes + 60).clamp(0, 24 * 60);

    final updated = TaskList(
      id: activeList.id,
      name: activeList.name,
      description: activeList.description,
      sortOption: activeList.sortOption,
      schedulingMode: activeList.schedulingMode,
      workStartMinutes: activeList.workStartMinutes,
      workEndMinutes: extendedEnd,
      workDays: activeList.workDays,
      slackThresholdDire: activeList.slackThresholdDire,
      slackThresholdPressing: activeList.slackThresholdPressing,
      slackThresholdFocus: activeList.slackThresholdFocus,
      slackThresholdSafe: activeList.slackThresholdSafe,
      simulatedTime: activeList.simulatedTime,
      lastSimulatedTime: activeList.lastSimulatedTime,
    );

    await ref.read(taskListsProvider.notifier).updateList(updated);

    if (context.mounted) {
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              "Extended today's work hours by 1h (ends at ${_formatMinutes(extendedEnd)})",
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 8),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                final restored = TaskList(
                  id: activeList.id,
                  name: activeList.name,
                  description: activeList.description,
                  sortOption: activeList.sortOption,
                  schedulingMode: activeList.schedulingMode,
                  workStartMinutes: activeList.workStartMinutes,
                  workEndMinutes: previousEnd,
                  workDays: activeList.workDays,
                  slackThresholdDire: activeList.slackThresholdDire,
                  slackThresholdPressing: activeList.slackThresholdPressing,
                  slackThresholdFocus: activeList.slackThresholdFocus,
                  slackThresholdSafe: activeList.slackThresholdSafe,
                  simulatedTime: activeList.simulatedTime,
                  lastSimulatedTime: activeList.lastSimulatedTime,
                );
                await ref.read(taskListsProvider.notifier).updateList(restored);
              },
            ),
          ),
        );
    }
  }

  Future<void> _completeTonight(
    BuildContext context,
    WidgetRef ref,
    TaskItem task,
  ) async {
    final now = ref.read(engineClockProvider).asData?.value ?? DateTime.now();
    final extensionEndMinutes = min(
      24 * 60,
      (now.hour * 60 + now.minute) + 120,
    );
    await _extendTodayHours(context, ref, extensionEndMinutes);
  }

  static String _formatDateHeading(DateTime date) {
    const weekdays = [
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
      'Sunday',
    ];
    const months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec',
    ];
    final weekday = weekdays[date.weekday - 1];
    final month = months[date.month - 1];
    return '$weekday, $month ${date.day}, ${date.year}';
  }

  static String _formatTime(DateTime time) {
    final hour = time.hour > 12
        ? time.hour - 12
        : time.hour == 0
        ? 12
        : time.hour;
    final minute = time.minute.toString().padLeft(2, '0');
    final amPm = time.hour >= 12 ? 'PM' : 'AM';
    return '$hour:$minute $amPm';
  }

  static String _formatMinutes(int minutes) {
    final hour = minutes ~/ 60;
    final minute = minutes % 60;
    final displayHour = hour > 12
        ? hour - 12
        : hour == 0
        ? 12
        : hour;
    final amPm = hour >= 12 ? 'PM' : 'AM';
    return '$displayHour:${minute.toString().padLeft(2, '0')} $amPm';
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({
    required this.icon,
    required this.title,
    required this.subtitle,
    this.accentColor,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color? accentColor;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final effectiveColor = accentColor ?? colorScheme.primary;

    return Padding(
      padding: const EdgeInsets.only(top: AppTheme.spacingSm),
      child: Row(
        children: [
          Icon(icon, size: 20, color: effectiveColor),
          const SizedBox(width: AppTheme.spacingXs),
          Text(
            title,
            style: textTheme.labelLarge?.copyWith(
              fontWeight: FontWeight.bold,
              color: effectiveColor,
              letterSpacing: 0.8,
            ),
          ),
          const SizedBox(width: AppTheme.spacingSm),
          Expanded(
            child: Text(
              '($subtitle)',
              style: textTheme.bodySmall?.copyWith(
                color: colorScheme.onSurfaceVariant,
                fontSize: 11,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }
}

class _TodayTaskCard extends StatelessWidget {
  const _TodayTaskCard({
    required this.task,
    required this.onCompleted,
    required this.onPostpone,
    this.isLeastSlack = false,
    this.slackNotice,
  });

  final TaskItem task;
  final VoidCallback onCompleted;
  final VoidCallback onPostpone;
  final bool isLeastSlack;
  final String? slackNotice;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Card(
      elevation: 0,
      color: isLeastSlack
          ? colorScheme.errorContainer.withAlpha(25)
          : colorScheme.surfaceContainerLowest,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        side: BorderSide(
          color: isLeastSlack
              ? Colors.red.shade400
              : colorScheme.outlineVariant,
          width: isLeastSlack ? 1.5 : 1.0,
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingMd),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Checkbox(value: task.isCompleted, onChanged: (_) => onCompleted()),
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
                          style: textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.bold,
                            decoration: task.isCompleted
                                ? TextDecoration.lineThrough
                                : null,
                          ),
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 6,
                          vertical: 2,
                        ),
                        decoration: BoxDecoration(
                          color: colorScheme.surfaceContainerHigh,
                          borderRadius: BorderRadius.circular(
                            AppTheme.radiusSm,
                          ),
                        ),
                        child: Text(
                          '~${task.estimatedDurationMinutes}m',
                          style: textTheme.labelSmall?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ],
                  ),
                  if (task.description.isNotEmpty) ...[
                    const SizedBox(height: 2),
                    Text(
                      task.description,
                      style: textTheme.bodySmall?.copyWith(
                        color: colorScheme.onSurfaceVariant,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                  const SizedBox(height: AppTheme.spacingSm),
                  Wrap(
                    spacing: AppTheme.spacingSm,
                    runSpacing: 4,
                    children: [
                      if (task.dueDate != null)
                        _MetaChip(
                          icon: Icons.event_outlined,
                          label: 'Due: ${_formatShortDate(task.dueDate!)}',
                          isAlert: isLeastSlack,
                        ),
                      if (slackNotice != null)
                        _MetaChip(
                          icon: Icons.hourglass_bottom,
                          label: slackNotice!,
                          isAlert: isLeastSlack,
                        ),
                      _MetaChip(
                        icon: Icons.priority_high,
                        label: 'Imp: ${task.importance}',
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Column(
              children: [
                FilledButton.tonal(
                  onPressed: onCompleted,
                  style: FilledButton.styleFrom(
                    visualDensity: VisualDensity.compact,
                  ),
                  child: const Text('Done'),
                ),
                const SizedBox(height: 4),
                IconButton(
                  icon: const Icon(Icons.arrow_forward_rounded, size: 16),
                  tooltip: 'Push to tomorrow',
                  onPressed: onPostpone,
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  static String _formatShortDate(DateTime date) {
    final hour = date.hour > 12
        ? date.hour - 12
        : date.hour == 0
        ? 12
        : date.hour;
    final minute = date.minute.toString().padLeft(2, '0');
    final amPm = date.hour >= 12 ? 'PM' : 'AM';
    return '${date.month}/${date.day} $hour:$minute $amPm';
  }
}

class _TodayEventCard extends StatelessWidget {
  const _TodayEventCard({required this.event});

  final FixedEvent event;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppTheme.spacingMd,
        vertical: AppTheme.spacingSm,
      ),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        border: Border.all(color: colorScheme.outlineVariant),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: colorScheme.inverseSurface,
              borderRadius: BorderRadius.circular(AppTheme.radiusSm),
            ),
            child: Text(
              '${_formatTime(event.startTime)} - ${_formatTime(event.endTime)}',
              style: TextStyle(
                color: colorScheme.onInverseSurface,
                fontSize: 11,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          const SizedBox(width: AppTheme.spacingMd),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  event.title,
                  style: textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
                if (event.description.isNotEmpty)
                  Text(
                    event.description,
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
              ],
            ),
          ),
          const Icon(Icons.lock_clock, size: 18),
        ],
      ),
    );
  }

  static String _formatTime(DateTime time) {
    final hour = time.hour > 12
        ? time.hour - 12
        : time.hour == 0
        ? 12
        : time.hour;
    final minute = time.minute.toString().padLeft(2, '0');
    final amPm = time.hour >= 12 ? 'PM' : 'AM';
    return '$hour:$minute $amPm';
  }
}

class _MetaChip extends StatelessWidget {
  const _MetaChip({
    required this.icon,
    required this.label,
    this.isAlert = false,
  });

  final IconData icon;
  final String label;
  final bool isAlert;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: isAlert
            ? Colors.red.withAlpha(25)
            : colorScheme.surfaceContainerHigh,
        borderRadius: BorderRadius.circular(AppTheme.radiusSm),
        border: Border.all(
          color: isAlert ? Colors.red.shade400 : colorScheme.outlineVariant,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            icon,
            size: 12,
            color: isAlert ? Colors.red.shade700 : colorScheme.onSurfaceVariant,
          ),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              color: isAlert
                  ? Colors.red.shade700
                  : colorScheme.onSurfaceVariant,
              fontWeight: isAlert ? FontWeight.bold : FontWeight.normal,
            ),
          ),
        ],
      ),
    );
  }
}
