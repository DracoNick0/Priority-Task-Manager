import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/schedule_models.dart';
import '../../models/task_item.dart';
import '../../models/task_list.dart';
import '../../providers/engine_status_provider.dart';
import '../../providers/event_providers.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../../providers/view_provider.dart';
import '../../utils/iterable_extensions.dart';
import '../theme/app_theme.dart';
import 'empty_schedule_hero.dart';
import 'fast_capture_palette.dart';

const double _hourHeight = 64.0;
const double _timeAxisWidth = 64.0;
const double _minDayColumnWidth = 140.0;
const double _trayWidth = 270.0;

/// The Week Horizon Calendar Grid View (resolves Issue #56).
/// Renders scheduled task chunks and fixed events as proportional time-blocks
/// on an hourly multi-day grid with synchronized time axis, daily capacity meters,
/// split task part badges, and an explainable "Unscheduled & Risk" side tray.
class HorizonGridView extends ConsumerStatefulWidget {
  const HorizonGridView({super.key, required this.listId});

  final String listId;

  @override
  ConsumerState<HorizonGridView> createState() => _HorizonGridViewState();
}

class _HorizonGridViewState extends ConsumerState<HorizonGridView> {
  int _weekOffset = 0;

  @override
  Widget build(BuildContext context) {
    final now = ref.watch(engineClockProvider).asData?.value ?? DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    final tasksAsync = ref.watch(tasksProvider(widget.listId));
    final eventsAsync = ref.watch(eventsProvider(widget.listId));
    final scheduleAsync = ref.watch(scheduleProvider);

    if (tasksAsync.isLoading && !tasksAsync.hasValue) {
      return const Center(child: CircularProgressIndicator());
    }

    final tasks = tasksAsync.asData?.value ?? const <TaskItem>[];
    final events = eventsAsync.asData?.value ?? const <FixedEvent>[];
    final schedule = scheduleAsync.asData?.value ?? DailySchedule.empty();

    if (!tasksAsync.isLoading &&
        !eventsAsync.isLoading &&
        tasks.isEmpty &&
        events.isEmpty) {
      return EmptyScheduleHero(listId: widget.listId);
    }

    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
    final activeList =
        lists.where((l) => l.id == widget.listId).firstOrNull;

    final workStartMinutes = activeList?.workStartMinutes ?? 9 * 60;
    final workEndMinutes = activeList?.workEndMinutes ?? 17 * 60;
    final startHour = max(0, workStartMinutes ~/ 60);
    final endHour = min(24, (workEndMinutes + 59) ~/ 60);
    final totalHours = max(1, endHour - startHour);

    // Calculate Monday of displayed week
    final currentMonday = today
        .subtract(Duration(days: today.weekday - 1))
        .add(Duration(days: _weekOffset * 7));
    final weekDays = List.generate(
      5,
      (i) => currentMonday.add(Duration(days: i)),
    );

    return Column(
      children: [
        // Top Week Navigation Bar
        _buildNavigationBar(context, now, weekDays.first, weekDays.last),
        const Divider(height: 1),

        // Main Horizon Body: Time Grid + Unscheduled & Risk Tray
        Expanded(
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isCompact = constraints.maxWidth < 900;
              return Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: _buildGridArea(
                      context,
                      now,
                      weekDays,
                      startHour,
                      endHour,
                      totalHours,
                      workStartMinutes,
                      workEndMinutes,
                      tasks,
                      events,
                      schedule,
                    ),
                  ),
                  if (!isCompact) ...[
                    const VerticalDivider(width: 1),
                    SizedBox(
                      width: _trayWidth,
                      child: _buildUnscheduledTray(
                        context,
                        tasks,
                        schedule,
                        workEndMinutes,
                      ),
                    ),
                  ],
                ],
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _buildNavigationBar(
    BuildContext context,
    DateTime now,
    DateTime weekStart,
    DateTime weekEnd,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final rangeText =
        '${_formatDate(weekStart)} – ${_formatDate(weekEnd, includeYear: true)}';

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppTheme.spacingMd,
        vertical: AppTheme.spacingSm,
      ),
      color: colorScheme.surface,
      child: Row(
        children: [
          Icon(Icons.calendar_view_week, size: 22, color: colorScheme.primary),
          const SizedBox(width: AppTheme.spacingSm),
          Text(
            'HORIZON — Week of $rangeText',
            style: textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
          ),
          const Spacer(),
          OutlinedButton(
            onPressed: () => setState(() => _weekOffset--),
            style: OutlinedButton.styleFrom(
              visualDensity: VisualDensity.compact,
              padding: const EdgeInsets.symmetric(horizontal: 10),
            ),
            child: const Text('< Prev Week'),
          ),
          const SizedBox(width: AppTheme.spacingXs),
          FilledButton.tonal(
            onPressed: _weekOffset == 0 ? null : () => setState(() => _weekOffset = 0),
            style: FilledButton.styleFrom(
              visualDensity: VisualDensity.compact,
              padding: const EdgeInsets.symmetric(horizontal: 12),
            ),
            child: const Text('Today'),
          ),
          const SizedBox(width: AppTheme.spacingXs),
          OutlinedButton(
            onPressed: () => setState(() => _weekOffset++),
            style: OutlinedButton.styleFrom(
              visualDensity: VisualDensity.compact,
              padding: const EdgeInsets.symmetric(horizontal: 10),
            ),
            child: const Text('Next Week >'),
          ),
          const SizedBox(width: AppTheme.spacingMd),
          IconButton(
            icon: const Icon(Icons.flash_on_rounded, size: 20),
            tooltip: 'Quick Add (Ctrl + K)',
            onPressed: () => showFastCapturePalette(context),
          ),
        ],
      ),
    );
  }

  Widget _buildGridArea(
    BuildContext context,
    DateTime now,
    List<DateTime> weekDays,
    int startHour,
    int endHour,
    int totalHours,
    int workStartMinutes,
    int workEndMinutes,
    List<TaskItem> tasks,
    List<FixedEvent> events,
    DailySchedule schedule,
  ) {
    final gridHeight = totalHours * _hourHeight;

    return SingleChildScrollView(
      scrollDirection: Axis.vertical,
      child: SizedBox(
        height: gridHeight + 72, // 72 for sticky header
        child: SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Left Synchronized Hourly Ruler
              _buildHourlyRuler(context, startHour, endHour, totalHours),
              const VerticalDivider(width: 1),

              // Day Columns
              for (final day in weekDays) ...[
                SizedBox(
                  width: max(_minDayColumnWidth, 160.0),
                  child: _buildDayTimelineColumn(
                    context,
                    day,
                    now,
                    startHour,
                    endHour,
                    totalHours,
                    workStartMinutes,
                    workEndMinutes,
                    tasks,
                    events,
                    schedule,
                  ),
                ),
                const VerticalDivider(width: 1),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHourlyRuler(
    BuildContext context,
    int startHour,
    int endHour,
    int totalHours,
  ) {
    final textTheme = Theme.of(context).textTheme;
    final colorScheme = Theme.of(context).colorScheme;

    return SizedBox(
      width: _timeAxisWidth,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // Header spacer
          Container(
            height: 72,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              border: Border(
                bottom: BorderSide(color: colorScheme.outlineVariant),
              ),
            ),
            child: Text(
              'TIME',
              style: textTheme.labelSmall?.copyWith(
                fontWeight: FontWeight.bold,
                color: colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          // Hour markers
          for (int h = startHour; h < endHour; h++) ...[
            SizedBox(
              height: _hourHeight,
              child: Align(
                alignment: Alignment.topCenter,
                child: Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(
                    _formatHourLabel(h),
                    style: textTheme.labelSmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildDayTimelineColumn(
    BuildContext context,
    DateTime day,
    DateTime now,
    int startHour,
    int endHour,
    int totalHours,
    int workStartMinutes,
    int workEndMinutes,
    List<TaskItem> tasks,
    List<FixedEvent> events,
    DailySchedule schedule,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    bool isSameDay(DateTime a, DateTime b) =>
        a.year == b.year && a.month == b.month && a.day == b.day;

    final isToday = isSameDay(day, now);
    final dayEvents =
        events.where((e) => isSameDay(e.startTime, day)).toList();

    // Collect scheduled tasks for this day
    final allScheduled = [...schedule.todayTasks, ...schedule.futureTasks];
    final dayScheduled = allScheduled
        .where((st) => isSameDay(st.startTime, day))
        .toList();

    // Fragment counts for split task badging
    final fragmentGroups = <String, List<ScheduledTask>>{};
    for (final s in allScheduled) {
      (fragmentGroups[s.id] ??= []).add(s);
    }
    for (final group in fragmentGroups.values) {
      group.sort((a, b) => a.startTime.compareTo(b.startTime));
    }

    final tasksById = {
      for (final t in tasks) t.id: t,
      for (final s in schedule.unscheduledTasks) s.id: s,
    };

    // Calculate capacity
    final plannedTaskHours =
        dayScheduled.fold<double>(0.0, (sum, st) => sum + st.chunkHours);
    final meetingHours = dayEvents.fold<double>(
      0.0,
      (sum, e) => sum + (e.endTime.difference(e.startTime).inMinutes / 60.0),
    );
    final totalCapacity = (workEndMinutes - workStartMinutes) / 60.0;
    final totalUsed = plannedTaskHours + meetingHours;
    final capacityPercent = totalCapacity > 0
        ? ((totalUsed / totalCapacity) * 100).round()
        : 100;

    Color meterColor;
    if (capacityPercent < 85) {
      meterColor = Colors.green;
    } else if (capacityPercent <= 100) {
      meterColor = Colors.amber;
    } else {
      meterColor = Colors.red;
    }

    final isOverloaded = capacityPercent > 100;
    final columnHeight = totalHours * _hourHeight;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Sticky Day Header
        Container(
          height: 72,
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
          decoration: BoxDecoration(
            color: isToday
                ? colorScheme.primaryContainer.withAlpha(50)
                : colorScheme.surfaceContainerLowest,
            border: Border(
              bottom: BorderSide(color: colorScheme.outlineVariant),
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      _formatWeekdayHeader(day),
                      style: textTheme.labelMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                        color: isToday ? colorScheme.primary : null,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    '$capacityPercent%',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: meterColor,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 3),
              ClipRRect(
                borderRadius: BorderRadius.circular(3),
                child: LinearProgressIndicator(
                  value: (totalUsed / totalCapacity).clamp(0.0, 1.0),
                  backgroundColor: colorScheme.surfaceContainerHighest,
                  valueColor: AlwaysStoppedAnimation<Color>(meterColor),
                  minHeight: 5,
                ),
              ),
              const SizedBox(height: 3),
              Text(
                isOverloaded
                    ? '! OVERLOAD ${totalUsed.toStringAsFixed(1)}h'
                    : '${totalUsed.toStringAsFixed(1)}h / ${totalCapacity.toStringAsFixed(1)}h',
                style: TextStyle(
                  fontSize: 10,
                  fontWeight: isOverloaded ? FontWeight.bold : FontWeight.normal,
                  color: isOverloaded ? Colors.red.shade700 : colorScheme.onSurfaceVariant,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),

        // Timeline Block Area with hour grid lines
        SizedBox(
          height: columnHeight,
          child: Stack(
            children: [
              // Horizontal hour grid lines
              for (int h = 0; h < totalHours; h++) ...[
                Positioned(
                  top: h * _hourHeight,
                  left: 0,
                  right: 0,
                  child: Container(
                    height: 1,
                    color: colorScheme.outlineVariant.withAlpha(90),
                  ),
                ),
              ],

              // Fixed Events
              for (final event in dayEvents)
                _buildPositionedEvent(
                  context,
                  event,
                  startHour,
                  endHour,
                ),

              // Scheduled Task Chunks
              for (final scheduled in dayScheduled)
                _buildPositionedTask(
                  context,
                  scheduled,
                  startHour,
                  endHour,
                  tasksById[scheduled.id],
                  fragmentGroups[scheduled.id] ?? const [],
                ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildPositionedEvent(
    BuildContext context,
    FixedEvent event,
    int startHour,
    int endHour,
  ) {
    final startMinutes = event.startTime.hour * 60 + event.startTime.minute;
    final endMinutes = event.endTime.hour * 60 + event.endTime.minute;
    final baseMinutes = startHour * 60;

    final top = max(0.0, (startMinutes - baseMinutes) / 60.0 * _hourHeight);
    final durationMinutes = max(20, endMinutes - startMinutes);
    final height = (durationMinutes / 60.0 * _hourHeight).clamp(24.0, 240.0);

    return Positioned(
      top: top,
      left: 3,
      right: 3,
      height: height,
      child: GestureDetector(
        onTap: () {
          ref.read(selectedInspectorProvider.notifier).state =
              InspectorTarget(kind: InspectorKind.event, id: event.id);
        },
        child: Container(
          decoration: BoxDecoration(
            color: const Color(0xFF2C3440),
            borderRadius: BorderRadius.circular(AppTheme.radiusSm),
            border: Border.all(color: Colors.black54),
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(AppTheme.radiusSm),
            child: CustomPaint(
              painter: DiagonalHatchPainter(
                lineColor: Colors.white.withAlpha(25),
                spacing: 12.0,
              ),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.lock_clock, size: 12, color: Colors.amber),
                        const SizedBox(width: 3),
                        Expanded(
                          child: Text(
                            event.title,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 11,
                              fontWeight: FontWeight.bold,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ),
                    if (height > 36)
                      Text(
                        '${durationMinutes}m fixed commitment',
                        style: const TextStyle(
                          color: Colors.white70,
                          fontSize: 10,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildPositionedTask(
    BuildContext context,
    ScheduledTask scheduled,
    int startHour,
    int endHour,
    TaskItem? task,
    List<ScheduledTask> fragments,
  ) {
    final colorScheme = Theme.of(context).colorScheme;

    final startMinutes =
        scheduled.startTime.hour * 60 + scheduled.startTime.minute;
    final endMinutes = scheduled.endTime.hour * 60 + scheduled.endTime.minute;
    final baseMinutes = startHour * 60;

    final top = max(0.0, (startMinutes - baseMinutes) / 60.0 * _hourHeight);
    final durationMinutes = max(20, endMinutes - startMinutes);
    final height = (durationMinutes / 60.0 * _hourHeight).clamp(24.0, 300.0);

    final isSplit = fragments.length > 1;
    final partIndex = isSplit ? fragments.indexOf(scheduled) + 1 : 1;
    final partTotal = fragments.length;

    return Positioned(
      top: top,
      left: 3,
      right: 3,
      height: height,
      child: GestureDetector(
        onTap: () {
          if (task != null) {
            ref.read(selectedInspectorProvider.notifier).state =
                InspectorTarget(kind: InspectorKind.task, id: task.id, task: task);
          }
        },
        child: Container(
          decoration: BoxDecoration(
            color: colorScheme.primaryContainer.withAlpha(160),
            borderRadius: BorderRadius.circular(AppTheme.radiusSm),
            border: Border.all(
              color: isSplit ? colorScheme.primary : colorScheme.outlineVariant,
              width: isSplit ? 1.5 : 1.0,
            ),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      task?.title ?? 'Task',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: colorScheme.onPrimaryContainer,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    '${(scheduled.chunkHours * 60).round()}m',
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w600,
                      color: colorScheme.onPrimaryContainer,
                    ),
                  ),
                ],
              ),
              if (isSplit) ...[
                const SizedBox(height: 2),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                  decoration: BoxDecoration(
                    color: colorScheme.primary,
                    borderRadius: BorderRadius.circular(3),
                  ),
                  child: Text(
                    'Part $partIndex of $partTotal',
                    style: TextStyle(
                      fontSize: 9,
                      color: colorScheme.onPrimary,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildUnscheduledTray(
    BuildContext context,
    List<TaskItem> tasks,
    DailySchedule schedule,
    int workEndMinutes,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final tasksById = {for (final t in tasks) t.id: t};
    for (final unscheduled in schedule.unscheduledTasks) {
      tasksById[unscheduled.id] = unscheduled;
    }

    final unscheduledTasks = schedule.unscheduledTaskIds
        .toSet()
        .map((id) => tasksById[id])
        .whereType<TaskItem>()
        .toList();

    return Container(
      color: colorScheme.surfaceContainerLowest,
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(Icons.report_problem_outlined,
                  size: 16, color: Colors.amber.shade800),
              const SizedBox(width: AppTheme.spacingXs),
              Expanded(
                child: Text(
                  'UNSCHEDULED & RISK',
                  style: textTheme.labelSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: colorScheme.onSurface,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              const SizedBox(width: 4),
              Text(
                '(${unscheduledTasks.length})',
                style: textTheme.labelSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTheme.spacingSm),
          Expanded(
            child: unscheduledTasks.isEmpty
                ? Center(
                    child: Text(
                      'All tasks fit within your scheduled work windows!',
                      style: textTheme.bodySmall?.copyWith(
                        color: Colors.green.shade700,
                      ),
                      textAlign: TextAlign.center,
                    ),
                  )
                : ListView.separated(
                    itemCount: unscheduledTasks.length,
                    separatorBuilder: (context, index) =>
                        const SizedBox(height: AppTheme.spacingSm),
                    itemBuilder: (context, index) {
                      final task = unscheduledTasks[index];
                      return _buildUnscheduledItem(
                        context,
                        task,
                        tasksById,
                        workEndMinutes,
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _buildUnscheduledItem(
    BuildContext context,
    TaskItem task,
    Map<String, TaskItem> tasksById,
    int workEndMinutes,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    // Diagnose reason
    String reason;
    String constraintType;
    IconData icon;
    Color accentColor;

    final blockedDep = task.dependencies
        .map((depId) => tasksById[depId])
        .where((dep) => dep != null && !dep.isCompleted)
        .firstOrNull;

    if (blockedDep != null) {
      constraintType = 'BLOCKED BY DEPENDENCY';
      reason = 'Prerequisite "${blockedDep.title}" must complete first';
      icon = Icons.lock_outline;
      accentColor = Colors.orange;
    } else if (task.dueDate != null && task.dueDate!.isBefore(DateTime.now())) {
      constraintType = 'DEADLINE PASSED';
      reason = 'Target deadline was ${_formatDate(task.dueDate!)}';
      icon = Icons.warning_rounded;
      accentColor = Colors.red;
    } else {
      constraintType = 'CAPACITY LIMIT';
      reason = 'Requires ${task.estimatedDurationMinutes}m, exceeding available daily windows';
      icon = Icons.hourglass_empty;
      accentColor = Colors.amber.shade800;
    }

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusSm),
        side: BorderSide(color: accentColor.withAlpha(120)),
      ),
      color: accentColor.withAlpha(15),
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingSm),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, size: 14, color: accentColor),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    constraintType,
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: accentColor,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              task.title,
              style: textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.bold,
                fontSize: 12,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              reason,
              style: textTheme.bodySmall?.copyWith(
                fontSize: 11,
                color: colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 6),
            Wrap(
              spacing: 4,
              children: [
                OutlinedButton(
                  onPressed: () {
                    // Extend hours
                    final lists = ref.read(taskListsProvider).asData?.value ?? [];
                    final activeList = lists.where((l) => l.id == widget.listId).firstOrNull;
                    if (activeList != null) {
                      final updated = TaskList(
                        id: activeList.id,
                        name: activeList.name,
                        description: activeList.description,
                        sortOption: activeList.sortOption,
                        schedulingMode: activeList.schedulingMode,
                        workStartMinutes: activeList.workStartMinutes,
                        workEndMinutes: (workEndMinutes + 60).clamp(0, 24 * 60),
                        workDays: activeList.workDays,
                        slackThresholdDire: activeList.slackThresholdDire,
                        slackThresholdPressing: activeList.slackThresholdPressing,
                        slackThresholdFocus: activeList.slackThresholdFocus,
                        slackThresholdSafe: activeList.slackThresholdSafe,
                        simulatedTime: activeList.simulatedTime,
                        lastSimulatedTime: activeList.lastSimulatedTime,
                      );
                      ref.read(taskListsProvider.notifier).updateList(updated);
                    }
                  },
                  style: OutlinedButton.styleFrom(
                    visualDensity: VisualDensity.compact,
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                  ),
                  child: const Text('Extend Hours', style: TextStyle(fontSize: 10)),
                ),
                OutlinedButton(
                  onPressed: () {
                    // Defer to tomorrow
                    final now = DateTime.now();
                    final updated = TaskItem(
                      id: task.id,
                      listId: task.listId,
                      title: task.title,
                      description: task.description,
                      dueDate: DateTime(now.year, now.month, now.day + 1, 17, 0),
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
                    ref.read(tasksProvider(widget.listId).notifier).updateTask(updated);
                  },
                  style: OutlinedButton.styleFrom(
                    visualDensity: VisualDensity.compact,
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                  ),
                  child: const Text('Defer', style: TextStyle(fontSize: 10)),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  static String _formatDate(DateTime date, {bool includeYear = false}) {
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
    final m = months[date.month - 1];
    return includeYear ? '$m ${date.day}, ${date.year}' : '$m ${date.day}';
  }

  static String _formatWeekdayHeader(DateTime date) {
    const weekdays = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];
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
    final wd = weekdays[date.weekday - 1];
    final m = months[date.month - 1];
    return '$wd $m ${date.day}';
  }

  static String _formatHourLabel(int hour) {
    final displayHour = hour > 12 ? hour - 12 : hour == 0 ? 12 : hour;
    final amPm = hour >= 12 ? 'PM' : 'AM';
    return '${displayHour.toString().padLeft(2, '0')}:00 $amPm';
  }
}

/// Custom painter for diagonal hatch marks on immovable fixed event cards.
class DiagonalHatchPainter extends CustomPainter {
  const DiagonalHatchPainter({required this.lineColor, this.spacing = 10.0});

  final Color lineColor;
  final double spacing;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = lineColor
      ..strokeWidth = 1.0;

    for (double x = -size.height; x < size.width; x += spacing) {
      canvas.drawLine(
        Offset(x, 0),
        Offset(x + size.height, size.height),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant DiagonalHatchPainter oldDelegate) =>
      oldDelegate.lineColor != lineColor || oldDelegate.spacing != spacing;
}
