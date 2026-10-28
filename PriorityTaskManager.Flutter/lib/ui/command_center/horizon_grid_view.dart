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
import '../../providers/user_profile_provider.dart';
import '../../providers/view_provider.dart';
import '../../utils/iterable_extensions.dart';
import '../theme/app_theme.dart';
import 'empty_schedule_hero.dart';
import 'fast_capture_palette.dart';
import 'horizon_event_style.dart';
import 'resizable_divider.dart';
import 'workload_capacity_meter.dart';

const double _hourHeight = 64.0;
const double _compactTaskBlockHeight = 44.0;
const double _compactSplitTaskBlockHeight = 48.0;
const double _timeAxisWidth = 64.0;
const double _gridDividerWidth = 1.0;
const int _timelineStartHour = 0;
const int _timelineEndHour = 24;
const int _timelineHours = _timelineEndHour - _timelineStartHour;
const double _dayHeaderHeight = 72.0;
const double _minDayColumnWidth = 160.0;
const double _trayMinHeight = 140.0;
const double _trayMaxHeight = 480.0;
const double _trayInitialHeight = 260.0;

bool _isSameCalendarDay(DateTime left, DateTime right) =>
    left.year == right.year &&
    left.month == right.month &&
    left.day == right.day;

/// The Week Horizon Calendar Grid View (resolves Issue #56).
/// Renders scheduled task chunks and fixed events as proportional time-blocks
/// on an hourly multi-day grid with synchronized time axis, daily capacity meters,
/// split task part badges, and an explainable bottom-docked "Unscheduled & Risk" tray.
class HorizonGridView extends ConsumerStatefulWidget {
  const HorizonGridView({super.key, required this.listId});

  final String listId;

  @override
  ConsumerState<HorizonGridView> createState() => _HorizonGridViewState();
}

class _HorizonGridViewState extends ConsumerState<HorizonGridView> {
  int _weekOffset = 0;
  bool _isUnscheduledTrayOpen = false;
  double _trayHeight = _trayInitialHeight;
  double? _trayHeightAtDragStart;
  late final ScrollController _timelineVerticalScrollController;
  late final ScrollController _headerHorizontalScrollController;
  late final ScrollController _timelineHorizontalScrollController;
  int? _lastAlignedWorkStartMinutes;
  bool _isSyncingHorizontalScroll = false;

  @override
  void initState() {
    super.initState();
    _timelineVerticalScrollController = ScrollController();
    _headerHorizontalScrollController = ScrollController();
    _timelineHorizontalScrollController = ScrollController();
    _headerHorizontalScrollController.addListener(
      _syncTimelineHorizontalScroll,
    );
    _timelineHorizontalScrollController.addListener(
      _syncHeaderHorizontalScroll,
    );
  }

  @override
  void dispose() {
    _headerHorizontalScrollController.removeListener(
      _syncTimelineHorizontalScroll,
    );
    _timelineHorizontalScrollController.removeListener(
      _syncHeaderHorizontalScroll,
    );
    _headerHorizontalScrollController.dispose();
    _timelineHorizontalScrollController.dispose();
    _timelineVerticalScrollController.dispose();
    super.dispose();
  }

  void _syncTimelineHorizontalScroll() => _syncHorizontalScroll(
    _headerHorizontalScrollController,
    _timelineHorizontalScrollController,
  );

  void _syncHeaderHorizontalScroll() => _syncHorizontalScroll(
    _timelineHorizontalScrollController,
    _headerHorizontalScrollController,
  );

  void _syncHorizontalScroll(ScrollController source, ScrollController target) {
    if (_isSyncingHorizontalScroll ||
        !source.hasClients ||
        !target.hasClients) {
      return;
    }

    final targetPosition = target.position;
    final targetOffset = source.offset
        .clamp(targetPosition.minScrollExtent, targetPosition.maxScrollExtent)
        .toDouble();
    if ((target.offset - targetOffset).abs() < 0.5) {
      return;
    }

    _isSyncingHorizontalScroll = true;
    try {
      target.jumpTo(targetOffset);
    } finally {
      _isSyncingHorizontalScroll = false;
    }
  }

  void _alignTimelineToWorkStart(int workStartMinutes) {
    if (_lastAlignedWorkStartMinutes == workStartMinutes) {
      return;
    }
    _lastAlignedWorkStartMinutes = workStartMinutes;

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted || !_timelineVerticalScrollController.hasClients) {
        return;
      }

      final position = _timelineVerticalScrollController.position;
      final desiredOffset = workStartMinutes / 60.0 * _hourHeight;
      final targetOffset = desiredOffset
          .clamp(position.minScrollExtent, position.maxScrollExtent)
          .toDouble();
      if ((_timelineVerticalScrollController.offset - targetOffset).abs() >
          0.5) {
        _timelineVerticalScrollController.jumpTo(targetOffset);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final now = ref.watch(engineClockProvider).asData?.value ?? DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final currentMonday = today
        .subtract(Duration(days: today.weekday - 1))
        .add(Duration(days: _weekOffset * 7));

    final tasksAsync = ref.watch(tasksProvider(widget.listId));
    final eventsAsync = ref.watch(eventsProvider(widget.listId));
    final eventOccurrencesAsync = ref.watch(
      eventOccurrencesProvider((widget.listId, currentMonday)),
    );
    final scheduleAsync = ref.watch(scheduleProvider);

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
    final events = eventsAsync.asData?.value ?? const <FixedEvent>[];
    final eventOccurrences =
        eventOccurrencesAsync.asData?.value ?? const <FixedEvent>[];
    final schedule = scheduleAsync.isLoading || scheduleAsync.hasError
        ? DailySchedule.empty()
        : scheduleAsync.asData?.value ?? DailySchedule.empty();

    if (!tasksAsync.isLoading &&
        !eventsAsync.isLoading &&
        tasks.isEmpty &&
        events.isEmpty) {
      return EmptyScheduleHero(listId: widget.listId);
    }

    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
    final activeList = lists.where((l) => l.id == widget.listId).firstOrNull;
    final profileAsync = ref.watch(userProfileProvider);
    final needsProfileSettings =
        activeList == null ||
        activeList.workStartMinutes == null ||
        activeList.workEndMinutes == null ||
        activeList.workDays == null;
    if (profileAsync.hasError && needsProfileSettings) {
      return Center(
        child: Text(
          'Could not load working-day settings: ${profileAsync.error}',
        ),
      );
    }

    final profile = profileAsync.asData?.value;
    final workStartMinutes =
        activeList?.workStartMinutes ?? profile?.workStartMinutes ?? 9 * 60;
    final workEndMinutes =
        activeList?.workEndMinutes ?? profile?.workEndMinutes ?? 17 * 60;
    final workDays =
        activeList?.workDays ??
        profile?.workDays ??
        const [
          DateTime.monday,
          DateTime.tuesday,
          DateTime.wednesday,
          DateTime.thursday,
          DateTime.friday,
        ];
    _alignTimelineToWorkStart(workStartMinutes);
    const startHour = _timelineStartHour;
    const endHour = _timelineEndHour;
    const totalHours = _timelineHours;

    final weekDays = List.generate(
      DateTime.sunday,
      (i) => currentMonday.add(Duration(days: i)),
    );

    return Column(
      children: [
        // Top Week Navigation Bar
        _buildNavigationBar(
          context,
          now,
          weekDays.first,
          weekDays.last,
          unscheduledCount: schedule.unscheduledTaskIds.toSet().length,
          isUnscheduledTrayOpen: _isUnscheduledTrayOpen,
          onToggleUnscheduledTray: () =>
              setState(() => _isUnscheduledTrayOpen = !_isUnscheduledTrayOpen),
        ),
        const Divider(height: 1),
        SizedBox(
          height: scheduleAsync.isLoading || scheduleAsync.hasError ? 36 : 0,
          child: scheduleAsync.isLoading || scheduleAsync.hasError
              ? _buildScheduleStatus(context, scheduleAsync)
              : null,
        ),

        // Main Horizon Body: Time Grid + Unscheduled & Risk Tray
        Expanded(
          child: LayoutBuilder(
            builder: (context, constraints) {
              final trayMaxHeight = (constraints.maxHeight * 0.7)
                  .clamp(_trayMinHeight, _trayMaxHeight)
                  .toDouble();
              final trayHeight = _trayHeight
                  .clamp(_trayMinHeight, trayMaxHeight)
                  .toDouble();

              return Column(
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
                      workDays,
                      tasks,
                      eventOccurrences,
                      schedule,
                    ),
                  ),
                  if (_isUnscheduledTrayOpen) ...[
                    ResizableDivider(
                      key: const ValueKey('unscheduled-tray-divider'),
                      axis: Axis.vertical,
                      onDragStart: () => _trayHeightAtDragStart = _trayHeight,
                      onDragUpdate: (totalOffset) {
                        final proposed =
                            (_trayHeightAtDragStart ?? _trayHeight) -
                            totalOffset;
                        setState(() {
                          _trayHeight = proposed
                              .clamp(_trayMinHeight, trayMaxHeight)
                              .toDouble();
                        });
                      },
                      onDragEnd: () => _trayHeightAtDragStart = null,
                    ),
                    SizedBox(
                      key: const ValueKey('unscheduled-tray'),
                      width: double.infinity,
                      height: trayHeight,
                      child: _buildUnscheduledTray(
                        context,
                        tasks,
                        schedule,
                        workEndMinutes,
                        onClose: () =>
                            setState(() => _isUnscheduledTrayOpen = false),
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

  Widget _buildScheduleStatus(
    BuildContext context,
    AsyncValue<DailySchedule> scheduleAsync,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final isLoading = scheduleAsync.isLoading;
    final message = isLoading
        ? 'Updating schedule...'
        : 'Could not compute schedule: ${scheduleAsync.error}';
    final foregroundColor = isLoading
        ? colorScheme.onSurfaceVariant
        : colorScheme.onErrorContainer;

    return Tooltip(
      message: message,
      child: Semantics(
        liveRegion: true,
        label: message,
        child: Material(
          color: isLoading
              ? colorScheme.surfaceContainerLow
              : colorScheme.errorContainer,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            child: Row(
              children: [
                if (isLoading)
                  SizedBox(
                    width: 16,
                    height: 16,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: foregroundColor,
                    ),
                  )
                else
                  Icon(Icons.error_outline, size: 18, color: foregroundColor),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    message,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: foregroundColor),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildNavigationBar(
    BuildContext context,
    DateTime now,
    DateTime weekStart,
    DateTime weekEnd, {
    required int unscheduledCount,
    required bool isUnscheduledTrayOpen,
    required VoidCallback onToggleUnscheduledTray,
  }) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final rangeText =
        '${_formatDate(weekStart)} – ${_formatDate(weekEnd, includeYear: true)}';

    return LayoutBuilder(
      builder: (context, constraints) {
        final isCompact = constraints.maxWidth < 900;
        final isStacked = isCompact;
        final horizontalPadding = isStacked
            ? AppTheme.spacingSm
            : AppTheme.spacingMd;
        final buttonPadding = isStacked ? 6.0 : 10.0;

        final previousWeekButton = OutlinedButton(
          onPressed: () => setState(() => _weekOffset--),
          style: OutlinedButton.styleFrom(
            visualDensity: VisualDensity.compact,
            padding: EdgeInsets.symmetric(horizontal: buttonPadding),
          ),
          child: const Text('< Prev Week'),
        );
        final todayButton = FilledButton.tonal(
          onPressed: _weekOffset == 0
              ? null
              : () => setState(() => _weekOffset = 0),
          style: FilledButton.styleFrom(
            visualDensity: VisualDensity.compact,
            padding: EdgeInsets.symmetric(horizontal: isStacked ? 8 : 12),
          ),
          child: const Text('Today'),
        );
        final nextWeekButton = OutlinedButton(
          onPressed: () => setState(() => _weekOffset++),
          style: OutlinedButton.styleFrom(
            visualDensity: VisualDensity.compact,
            padding: EdgeInsets.symmetric(horizontal: buttonPadding),
          ),
          child: const Text('Next Week >'),
        );
        final quickAddButton = IconButton(
          icon: const Icon(Icons.flash_on_rounded, size: 20),
          tooltip: 'Quick Add (Ctrl + K)',
          onPressed: () => showFastCapturePalette(context),
        );
        final hasUnscheduledTasks = unscheduledCount > 0;
        final unscheduledButton = Tooltip(
          message: isUnscheduledTrayOpen
              ? 'Hide Unscheduled & Risk'
              : 'Show Unscheduled & Risk',
          child: TextButton.icon(
            onPressed: onToggleUnscheduledTray,
            style: TextButton.styleFrom(
              visualDensity: VisualDensity.compact,
              padding: const EdgeInsets.symmetric(horizontal: 8),
              foregroundColor: hasUnscheduledTasks ? colorScheme.error : null,
            ),
            icon: const Icon(Icons.report_problem_outlined),
            label: Text(
              hasUnscheduledTasks
                  ? 'Unscheduled & Risk ($unscheduledCount)'
                  : 'Unscheduled & Risk',
            ),
          ),
        );
        final title = Expanded(
          child: Text(
            'HORIZON — Week of $rangeText',
            style: textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
            overflow: TextOverflow.ellipsis,
            maxLines: 1,
          ),
        );

        return Container(
          padding: EdgeInsets.symmetric(
            horizontal: horizontalPadding,
            vertical: AppTheme.spacingSm,
          ),
          color: colorScheme.surface,
          child: isStacked
              ? Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Row(
                      children: [
                        Icon(
                          Icons.calendar_view_week,
                          size: 22,
                          color: colorScheme.primary,
                        ),
                        const SizedBox(width: AppTheme.spacingSm),
                        title,
                        unscheduledButton,
                        quickAddButton,
                      ],
                    ),
                    const SizedBox(height: AppTheme.spacingXs),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        previousWeekButton,
                        todayButton,
                        nextWeekButton,
                      ],
                    ),
                  ],
                )
              : Row(
                  children: [
                    Icon(
                      Icons.calendar_view_week,
                      size: 22,
                      color: colorScheme.primary,
                    ),
                    const SizedBox(width: AppTheme.spacingSm),
                    title,
                    previousWeekButton,
                    const SizedBox(width: AppTheme.spacingXs),
                    todayButton,
                    const SizedBox(width: AppTheme.spacingXs),
                    nextWeekButton,
                    const SizedBox(width: AppTheme.spacingSm),
                    unscheduledButton,
                    const SizedBox(width: AppTheme.spacingXs),
                    quickAddButton,
                  ],
                ),
        );
      },
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
    List<int> workDays,
    List<TaskItem> tasks,
    List<FixedEvent> events,
    DailySchedule schedule,
  ) {
    final gridHeight = totalHours * _hourHeight;
    final workStartOffset =
        workStartMinutes.clamp(0, 24 * 60) / 60.0 * _hourHeight;

    return LayoutBuilder(
      builder: (context, constraints) {
        final availableWidth = constraints.maxWidth.isFinite
            ? constraints.maxWidth
            : 0.0;
        final dayColumnWidth = max(
          _minDayColumnWidth,
          (availableWidth -
                  _timeAxisWidth -
                  (weekDays.length + 1) * _gridDividerWidth) /
              weekDays.length,
        );
        final viewportHeight = max(
          0.0,
          constraints.maxHeight - _dayHeaderHeight,
        );
        final bottomPadding = max(
          0.0,
          workStartOffset + viewportHeight - gridHeight,
        );

        return Column(
          children: [
            SizedBox(
              height: _dayHeaderHeight,
              child: SingleChildScrollView(
                key: const ValueKey('horizon-header-horizontal-scroll'),
                controller: _headerHorizontalScrollController,
                scrollDirection: Axis.horizontal,
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _buildHourlyRulerHeader(context),
                    const VerticalDivider(width: _gridDividerWidth),
                    for (final day in weekDays) ...[
                      SizedBox(
                        width: dayColumnWidth,
                        child: _buildDayHeader(
                          context,
                          day,
                          now,
                          workDays.contains(day.weekday),
                          workStartMinutes,
                          workEndMinutes,
                          events,
                          schedule,
                        ),
                      ),
                      const VerticalDivider(width: _gridDividerWidth),
                    ],
                  ],
                ),
              ),
            ),
            Expanded(
              child: SingleChildScrollView(
                key: const ValueKey('horizon-timeline-vertical-scroll'),
                controller: _timelineVerticalScrollController,
                scrollDirection: Axis.vertical,
                child: Padding(
                  padding: EdgeInsets.only(bottom: bottomPadding),
                  child: SingleChildScrollView(
                    key: const ValueKey('horizon-timeline-horizontal-scroll'),
                    controller: _timelineHorizontalScrollController,
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        _buildHourlyRuler(
                          context,
                          startHour,
                          endHour,
                          totalHours,
                        ),
                        const VerticalDivider(width: _gridDividerWidth),
                        for (final day in weekDays) ...[
                          SizedBox(
                            width: dayColumnWidth,
                            child: _buildDayTimelineColumn(
                              context,
                              day,
                              now,
                              startHour,
                              endHour,
                              totalHours,
                              workDays.contains(day.weekday),
                              workStartMinutes,
                              workEndMinutes,
                              tasks,
                              events,
                              schedule,
                            ),
                          ),
                          const VerticalDivider(width: _gridDividerWidth),
                        ],
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _buildHourlyRulerHeader(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final colorScheme = Theme.of(context).colorScheme;

    return SizedBox(
      width: _timeAxisWidth,
      child: Container(
        height: _dayHeaderHeight,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          border: Border(bottom: BorderSide(color: colorScheme.outlineVariant)),
        ),
        child: Text(
          'TIME',
          style: textTheme.labelSmall?.copyWith(
            fontWeight: FontWeight.bold,
            color: colorScheme.onSurfaceVariant,
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
      height: totalHours * _hourHeight,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
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

  Widget _buildDayHeader(
    BuildContext context,
    DateTime day,
    DateTime now,
    bool isWorkingDay,
    int workStartMinutes,
    int workEndMinutes,
    List<FixedEvent> events,
    DailySchedule schedule,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final isToday = _isSameCalendarDay(day, now);
    var plannedTaskHours = 0.0;
    var meetingHours = 0.0;
    var totalCapacity = 0.0;
    var totalUsed = 0.0;
    int? capacityPercent;
    var isOverloaded = false;
    var meterColor = colorScheme.onSurfaceVariant;
    if (isWorkingDay) {
      final dayEvents = events
          .where((event) => _isSameCalendarDay(event.startTime, day))
          .toList();
      final allScheduled = [...schedule.todayTasks, ...schedule.futureTasks];
      final dayScheduled = allScheduled
          .where((scheduled) => _isSameCalendarDay(scheduled.startTime, day))
          .toList();

      final workdayStart = DateTime(
        day.year,
        day.month,
        day.day,
      ).add(Duration(minutes: workStartMinutes));
      final workdayEnd = DateTime(
        day.year,
        day.month,
        day.day,
      ).add(Duration(minutes: workEndMinutes));
      final capacityWindowStart = isToday && now.isAfter(workdayStart)
          ? now
          : workdayStart;
      plannedTaskHours = scheduledTaskHoursWithinWorkWindow(
        dayScheduled,
        windowStart: capacityWindowStart,
        windowEnd: workdayEnd,
      );
      meetingHours = eventHoursWithinWorkWindow(
        dayEvents,
        windowStart: capacityWindowStart,
        windowEnd: workdayEnd,
      );
      totalCapacity = max(
        0.0,
        workdayEnd.difference(capacityWindowStart).inSeconds / 3600.0,
      ).toDouble();
      totalUsed = plannedTaskHours + meetingHours;
      capacityPercent = totalCapacity > 0
          ? ((totalUsed / totalCapacity) * 100).round()
          : null;
      isOverloaded = capacityPercent == null
          ? totalUsed > 0
          : capacityPercent > 100;

      if (isOverloaded) {
        meterColor = Colors.red;
      } else if (capacityPercent == null) {
        meterColor = colorScheme.onSurfaceVariant;
      } else if (capacityPercent < 85) {
        meterColor = Colors.green;
      } else {
        meterColor = Colors.amber;
      }
    }

    return Container(
      key: ValueKey('horizon-day-header-${day.year}-${day.month}-${day.day}'),
      height: _dayHeaderHeight,
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      decoration: BoxDecoration(
        color: isToday
            ? colorScheme.primaryContainer.withAlpha(50)
            : colorScheme.surfaceContainerLowest,
        border: Border(bottom: BorderSide(color: colorScheme.outlineVariant)),
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
              if (isWorkingDay)
                Text(
                  capacityPercent == null ? '0h left' : '$capacityPercent%',
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: meterColor,
                  ),
                ),
            ],
          ),
          if (isWorkingDay) ...[
            const SizedBox(height: 3),
            SegmentedCapacityMeter(
              key: ValueKey(
                'horizon-capacity-meter-${day.year}-${day.month}-${day.day}',
              ),
              eventHours: meetingHours,
              taskHours: plannedTaskHours,
              capacityHours: totalCapacity,
              loadColor: meterColor,
              height: 5,
            ),
            const SizedBox(height: 3),
            Text(
              isOverloaded
                  ? '! OVERLOAD ${totalUsed.toStringAsFixed(1)}h'
                  : capacityPercent == null
                  ? '0.0h remaining'
                  : '${totalUsed.toStringAsFixed(1)}h / ${totalCapacity.toStringAsFixed(1)}h',
              style: TextStyle(
                fontSize: 10,
                fontWeight: isOverloaded ? FontWeight.bold : FontWeight.normal,
                color: isOverloaded
                    ? Colors.red.shade700
                    : colorScheme.onSurfaceVariant,
              ),
              overflow: TextOverflow.ellipsis,
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
    bool isWorkingDay,
    int workStartMinutes,
    int workEndMinutes,
    List<TaskItem> tasks,
    List<FixedEvent> events,
    DailySchedule schedule,
  ) {
    final colorScheme = Theme.of(context).colorScheme;

    final currentTimeMinutes = now.hour * 60 + now.minute + now.second / 60.0;
    final timelineStartMinutes = startHour * 60;
    final timelineEndMinutes = endHour * 60;
    final isCurrentTimeVisible =
        _isSameCalendarDay(day, now) &&
        currentTimeMinutes >= timelineStartMinutes &&
        currentTimeMinutes < timelineEndMinutes;
    final currentTimeTop =
        (currentTimeMinutes - timelineStartMinutes) / 60 * _hourHeight;
    final boundedWorkStartMinutes = workStartMinutes.clamp(
      timelineStartMinutes,
      timelineEndMinutes,
    );
    final boundedWorkEndMinutes = workEndMinutes.clamp(
      boundedWorkStartMinutes,
      timelineEndMinutes,
    );
    final workBandTop =
        (boundedWorkStartMinutes - timelineStartMinutes) / 60 * _hourHeight;
    final workBandHeight =
        (boundedWorkEndMinutes - boundedWorkStartMinutes) / 60 * _hourHeight;
    final dayEvents = events
        .where((event) => _isSameCalendarDay(event.startTime, day))
        .toList();

    // Collect scheduled tasks for this day
    final allScheduled = [...schedule.todayTasks, ...schedule.futureTasks];
    final dayScheduled = allScheduled
        .where((scheduled) => _isSameCalendarDay(scheduled.startTime, day))
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

    final columnHeight = totalHours * _hourHeight;

    return SizedBox(
      height: columnHeight,
      child: Stack(
        key: ValueKey('horizon-timeline-${day.year}-${day.month}-${day.day}'),
        children: [
          if (isWorkingDay && workBandHeight > 0)
            Positioned(
              top: workBandTop,
              left: 0,
              right: 0,
              height: workBandHeight,
              child: ColoredBox(
                key: ValueKey(
                  'horizon-working-hours-${day.year}-${day.month}-${day.day}',
                ),
                color: colorScheme.primaryContainer.withAlpha(50),
              ),
            ),
          for (int hour = 0; hour < totalHours; hour++)
            Positioned(
              top: hour * _hourHeight,
              left: 0,
              right: 0,
              child: Container(
                height: 1,
                color: colorScheme.outlineVariant.withAlpha(90),
              ),
            ),
          for (final event in dayEvents)
            _buildPositionedEvent(context, event, startHour, endHour),
          for (final scheduled in dayScheduled)
            _buildPositionedTask(
              context,
              scheduled,
              startHour,
              endHour,
              tasksById[scheduled.id],
              fragmentGroups[scheduled.id] ?? const [],
            ),
          if (isCurrentTimeVisible)
            Positioned(
              top: currentTimeTop,
              left: 0,
              right: 0,
              child: Semantics(
                label: 'Current time',
                child: Container(
                  key: const ValueKey('horizon-current-time-indicator'),
                  height: 2,
                  color: colorScheme.error,
                ),
              ),
            ),
        ],
      ),
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
    final durationMinutes = endMinutes - startMinutes;
    final blockDurationMinutes = max(20, durationMinutes);
    final height = (blockDurationMinutes / 60.0 * _hourHeight).clamp(
      24.0,
      240.0,
    );

    return Positioned(
      top: top,
      left: 3,
      right: 3,
      height: height,
      child: GestureDetector(
        onTap: () {
          ref.read(selectedInspectorProvider.notifier).state = InspectorTarget(
            kind: InspectorKind.event,
            id: event.id,
          );
        },
        child: Container(
          decoration: BoxDecoration(
            color: horizonEventBlockColor,
            borderRadius: BorderRadius.circular(AppTheme.radiusSm),
            border: Border.all(color: Colors.black54),
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(AppTheme.radiusSm),
            child: CustomPaint(
              painter: const DiagonalHatchPainter(
                lineColor: horizonEventHatchLineColor,
                spacing: horizonEventHatchSpacing,
              ),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.lock_clock,
                          size: 12,
                          color: Colors.amber,
                        ),
                        const SizedBox(width: 3),
                        Expanded(
                          child: Text(
                            event.title,
                            maxLines: 1,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 11,
                              fontWeight: FontWeight.bold,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${durationMinutes}m',
                          style: const TextStyle(
                            color: Colors.white70,
                            fontSize: 10,
                            fontWeight: FontWeight.w600,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
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
    final durationHeight = durationMinutes / 60.0 * _hourHeight;
    final height = durationHeight < 24.0 ? 24.0 : durationHeight;

    final isSplit = fragments.length > 1;
    final isCompact =
        height <
        (isSplit ? _compactSplitTaskBlockHeight : _compactTaskBlockHeight);
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
            ref
                .read(selectedInspectorProvider.notifier)
                .state = InspectorTarget(
              kind: InspectorKind.task,
              id: task.id,
              task: task,
            );
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
          padding: EdgeInsets.symmetric(
            horizontal: 6,
            vertical: isCompact ? 0 : 4,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      task?.title ?? 'Task',
                      maxLines: isCompact || isSplit ? 1 : 2,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: colorScheme.onPrimaryContainer,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  if (isCompact && isSplit) ...[
                    const SizedBox(width: 4),
                    Tooltip(
                      message: 'Part $partIndex of $partTotal',
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 3,
                          vertical: 1,
                        ),
                        decoration: BoxDecoration(
                          color: colorScheme.primary,
                          borderRadius: BorderRadius.circular(3),
                        ),
                        child: Text(
                          'P$partIndex/$partTotal',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            fontSize: 9,
                            color: colorScheme.onPrimary,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 4),
                  ],
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
              if (isSplit && !isCompact) ...[
                const SizedBox(height: 2),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 4,
                    vertical: 1,
                  ),
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
    int workEndMinutes, {
    VoidCallback? onClose,
  }) {
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
              Icon(
                Icons.report_problem_outlined,
                size: 16,
                color: Colors.amber.shade800,
              ),
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
              if (onClose != null) ...[
                const SizedBox(width: 4),
                IconButton(
                  icon: const Icon(Icons.close),
                  tooltip: 'Close Unscheduled & Risk',
                  visualDensity: VisualDensity.compact,
                  onPressed: onClose,
                ),
              ],
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
      reason =
          'Requires ${task.estimatedDurationMinutes}m, exceeding available daily windows';
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
                    final lists =
                        ref.read(taskListsProvider).asData?.value ?? [];
                    final activeList = lists
                        .where((l) => l.id == widget.listId)
                        .firstOrNull;
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
                        slackThresholdPressing:
                            activeList.slackThresholdPressing,
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
                  child: const Text(
                    'Extend Hours',
                    style: TextStyle(fontSize: 10),
                  ),
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
                      dueDate: DateTime(
                        now.year,
                        now.month,
                        now.day + 1,
                        17,
                        0,
                      ),
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
                    ref
                        .read(tasksProvider(widget.listId).notifier)
                        .updateTask(updated);
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
    final displayHour = hour > 12
        ? hour - 12
        : hour == 0
        ? 12
        : hour;
    final amPm = hour >= 12 ? 'PM' : 'AM';
    return '${displayHour.toString().padLeft(2, '0')}:00 $amPm';
  }
}
