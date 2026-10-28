import 'package:flutter/material.dart';

import '../../models/fixed_event.dart';
import '../../models/schedule_models.dart';
import 'horizon_event_style.dart';

double eventHoursWithinWorkWindow(
  Iterable<FixedEvent> events, {
  required DateTime windowStart,
  required DateTime windowEnd,
}) {
  if (!windowEnd.isAfter(windowStart)) {
    return 0.0;
  }

  final intervals = <DateTimeRange>[];
  for (final event in events) {
    final start = event.startTime.isAfter(windowStart)
        ? event.startTime
        : windowStart;
    final end = event.endTime.isBefore(windowEnd) ? event.endTime : windowEnd;
    if (end.isAfter(start)) {
      intervals.add(DateTimeRange(start: start, end: end));
    }
  }

  intervals.sort((left, right) => left.start.compareTo(right.start));
  if (intervals.isEmpty) {
    return 0.0;
  }

  var currentStart = intervals.first.start;
  var currentEnd = intervals.first.end;
  var totalSeconds = 0;
  for (final interval in intervals.skip(1)) {
    if (!interval.start.isAfter(currentEnd)) {
      if (interval.end.isAfter(currentEnd)) {
        currentEnd = interval.end;
      }
      continue;
    }

    totalSeconds += currentEnd.difference(currentStart).inSeconds;
    currentStart = interval.start;
    currentEnd = interval.end;
  }
  totalSeconds += currentEnd.difference(currentStart).inSeconds;

  return totalSeconds / 3600.0;
}

double scheduledTaskHoursWithinWorkWindow(
  Iterable<ScheduledTask> tasks, {
  required DateTime windowStart,
  required DateTime windowEnd,
}) {
  var totalSeconds = 0;
  for (final task in tasks) {
    final start = task.startTime.isAfter(windowStart)
        ? task.startTime
        : windowStart;
    final end = task.endTime.isBefore(windowEnd) ? task.endTime : windowEnd;
    if (end.isAfter(start)) {
      totalSeconds += end.difference(start).inSeconds;
    }
  }

  return totalSeconds / 3600.0;
}

class SegmentedCapacityMeter extends StatelessWidget {
  const SegmentedCapacityMeter({
    super.key,
    required this.eventHours,
    required this.taskHours,
    required this.capacityHours,
    required this.loadColor,
    required this.height,
  });

  final double eventHours;
  final double taskHours;
  final double capacityHours;
  final Color loadColor;
  final double height;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final eventFraction = _capacityFraction(eventHours);
    final taskFraction = capacityHours > 0
        ? (taskHours / capacityHours).clamp(0.0, 1.0 - eventFraction).toDouble()
        : 0.0;
    final freeHours = capacityHours > eventHours + taskHours
        ? capacityHours - eventHours - taskHours
        : 0.0;
    final overflowHours = eventHours + taskHours > capacityHours
        ? eventHours + taskHours - capacityHours
        : 0.0;
    final description =
        'Events: ${eventHours.toStringAsFixed(1)}h; '
        'tasks: ${taskHours.toStringAsFixed(1)}h; '
        'free: ${freeHours.toStringAsFixed(1)}h of '
        '${capacityHours.toStringAsFixed(1)}h capacity'
        '${overflowHours > 0 ? '; ${overflowHours.toStringAsFixed(1)}h over capacity' : ''}.';

    return Semantics(
      label: description,
      child: Tooltip(
        message: description,
        excludeFromSemantics: true,
        child: SizedBox(
          width: double.infinity,
          height: height,
          child: LayoutBuilder(
            builder: (context, constraints) {
              final width = constraints.maxWidth;
              return ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (eventFraction > 0)
                      SizedBox(
                        width: width * eventFraction,
                        child: CustomPaint(
                          key: const ValueKey('capacity-meter-events-hatch'),
                          foregroundPainter: const DiagonalHatchPainter(
                            lineColor: horizonEventHatchLineColor,
                            spacing: horizonEventHatchSpacing,
                          ),
                          child: const ColoredBox(
                            key: ValueKey('capacity-meter-events'),
                            color: horizonEventBlockColor,
                          ),
                        ),
                      ),
                    if (taskFraction > 0)
                      SizedBox(
                        width: width * taskFraction,
                        child: ColoredBox(
                          key: const ValueKey('capacity-meter-tasks'),
                          color: loadColor,
                        ),
                      ),
                    Expanded(
                      child: ColoredBox(
                        key: const ValueKey('capacity-meter-free'),
                        color: colorScheme.surfaceContainerHighest,
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ),
    );
  }

  double _capacityFraction(double hours) => capacityHours > 0
      ? (hours / capacityHours).clamp(0.0, 1.0).toDouble()
      : 0.0;
}
