import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/schedule_models.dart';
import '../../models/task_item.dart';
import '../../models/task_list.dart';
import '../../providers/app_notifications_provider.dart';
import '../../providers/engine_status_provider.dart';
import '../../providers/event_providers.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../../providers/view_provider.dart';
import '../../utils/iterable_extensions.dart';
import '../theme/app_theme.dart';

enum DailyRitualType { morningKickoff, eveningWindDown }

/// Tracks dates on which morning and evening rituals were executed
/// to prevent duplicate popups on the same calendar day.
class DailyRitualsState {
  const DailyRitualsState({
    this.lastMorningDate,
    this.lastEveningDate,
  });

  final String? lastMorningDate;
  final String? lastEveningDate;

  DailyRitualsState copyWith({
    String? lastMorningDate,
    String? lastEveningDate,
  }) {
    return DailyRitualsState(
      lastMorningDate: lastMorningDate ?? this.lastMorningDate,
      lastEveningDate: lastEveningDate ?? this.lastEveningDate,
    );
  }
}

final dailyRitualsStateProvider =
    StateProvider<DailyRitualsState>((ref) => const DailyRitualsState());

/// Opens either the Morning Kickoff or Evening Wind-Down dialog.
Future<void> showDailyRitualDialog(
  BuildContext context, {
  required DailyRitualType type,
  required String listId,
}) {
  return showDialog<void>(
    context: context,
    builder: (context) => DailyRitualDialog(type: type, listId: listId),
  );
}

/// Modal dialog for Daily Rituals:
/// - Morning Kickoff: Workload snapshot, least-slack attention, start-day action
/// - Evening Wind-Down: Accomplishment summary, midnight deadline handling, wrap-up
class DailyRitualDialog extends ConsumerWidget {
  const DailyRitualDialog({
    super.key,
    required this.type,
    required this.listId,
  });

  final DailyRitualType type;
  final String listId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final now = ref.watch(engineClockProvider).asData?.value ?? DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    final tasks =
        ref.watch(tasksProvider(listId)).asData?.value ?? const <TaskItem>[];
    final events =
        ref.watch(eventsProvider(listId)).asData?.value ?? const <FixedEvent>[];
    final schedule =
        ref.watch(scheduleProvider).asData?.value ?? DailySchedule.empty();

    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
    final activeList = lists.where((l) => l.id == listId).firstOrNull;

    final workStartMinutes = activeList?.workStartMinutes ?? 9 * 60;
    final workEndMinutes = activeList?.workEndMinutes ?? 17 * 60;

    return Dialog(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusLg),
      ),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 640),
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppTheme.spacingLg),
          child: type == DailyRitualType.morningKickoff
              ? _buildMorningKickoff(
                  context,
                  ref,
                  now,
                  today,
                  tasks,
                  events,
                  schedule,
                  workStartMinutes,
                  workEndMinutes,
                )
              : _buildEveningWindDown(
                  context,
                  ref,
                  now,
                  today,
                  tasks,
                  workEndMinutes,
                ),
        ),
      ),
    );
  }

  Widget _buildMorningKickoff(
    BuildContext context,
    WidgetRef ref,
    DateTime now,
    DateTime today,
    List<TaskItem> tasks,
    List<FixedEvent> events,
    DailySchedule schedule,
    int workStartMinutes,
    int workEndMinutes,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    bool isSameDay(DateTime a, DateTime b) =>
        a.year == b.year && a.month == b.month && a.day == b.day;

    final todayEvents =
        events.where((e) => isSameDay(e.startTime, today)).toList();

    double plannedWorkHours;
    int taskCount;
    if (schedule.todayTasks.isNotEmpty) {
      plannedWorkHours = schedule.todayTasks
          .fold<double>(0.0, (sum, t) => sum + t.chunkHours);
      taskCount = schedule.todayTasks.map((t) => t.id).toSet().length;
    } else {
      final incompleteToday = tasks.where((t) =>
          !t.isCompleted &&
          (t.dueDate == null || isSameDay(t.dueDate!, today)));
      plannedWorkHours = incompleteToday.fold<double>(
          0.0, (sum, t) => sum + (t.estimatedDurationMinutes / 60.0));
      taskCount = incompleteToday.length;
    }

    final meetingHours = todayEvents.fold<double>(
      0.0,
      (sum, e) => sum + (e.endTime.difference(e.startTime).inMinutes / 60.0),
    );

    final totalCapacity = (workEndMinutes - workStartMinutes) / 60.0;
    final freeBuffer = max(0.0, totalCapacity - plannedWorkHours - meetingHours);

    // Find least-slack task
    final leastSlackTitle = schedule.leastSlackTask != 'None'
        ? schedule.leastSlackTask
        : tasks
            .where((t) => !t.isCompleted)
            .firstOrNull
            ?.title;

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Header
        Row(
          children: [
            Icon(Icons.wb_sunny_outlined, size: 26, color: Colors.orange.shade700),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: Text(
                'Good Morning! Here is your plan for today',
                style: textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            IconButton(
              icon: const Icon(Icons.close, size: 20),
              onPressed: () => Navigator.of(context).pop(),
            ),
          ],
        ),
        const Divider(),
        const SizedBox(height: AppTheme.spacingSm),

        // Workload Snapshot
        Text(
          "TODAY'S WORKLOAD SNAPSHOT:",
          style: textTheme.labelSmall?.copyWith(
            fontWeight: FontWeight.bold,
            color: colorScheme.primary,
            letterSpacing: 1.1,
          ),
        ),
        const SizedBox(height: AppTheme.spacingSm),
        Container(
          padding: const EdgeInsets.all(AppTheme.spacingMd),
          decoration: BoxDecoration(
            color: colorScheme.surfaceContainerLow,
            borderRadius: BorderRadius.circular(AppTheme.radiusMd),
            border: Border.all(color: colorScheme.outlineVariant),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _bulletPoint(
                'Scheduled Work: ${plannedWorkHours.toStringAsFixed(1)} hours across $taskCount task(s)',
              ),
              const SizedBox(height: 4),
              _bulletPoint(
                'Fixed Events: ${meetingHours.toStringAsFixed(1)} hours (${todayEvents.length} meeting(s))',
              ),
              const SizedBox(height: 4),
              _bulletPoint(
                'Free Buffer: ${freeBuffer.toStringAsFixed(1)} hours (${freeBuffer >= 1.0 ? "Healthy margin for interruptions" : "Tight schedule"})',
                color: freeBuffer >= 1.0
                    ? Colors.green.shade700
                    : Colors.amber.shade800,
              ),
            ],
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),

        // Least Slack Attention
        if (leastSlackTitle != null) ...[
          Container(
            padding: const EdgeInsets.all(AppTheme.spacingMd),
            decoration: BoxDecoration(
              color: Colors.red.withAlpha(20),
              borderRadius: BorderRadius.circular(AppTheme.radiusMd),
              border: Border.all(color: Colors.red.withAlpha(80)),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.priority_high, size: 20, color: Colors.red),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'LEAST SLACK ATTENTION:',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                          color: Colors.red,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '"$leastSlackTitle" has the tightest deadline buffer. Recommended for your first focus block.',
                        style: textTheme.bodySmall?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: AppTheme.spacingLg),
        ],

        // Actions
        Wrap(
          alignment: WrapAlignment.end,
          spacing: AppTheme.spacingSm,
          runSpacing: AppTheme.spacingXs,
          children: [
            OutlinedButton(
              onPressed: () {
                Navigator.of(context).pop();
                ref.read(selectedInspectorProvider.notifier).state =
                    InspectorTarget(kind: InspectorKind.list, id: listId);
              },
              child: const Text('Adjust Hours'),
            ),
            OutlinedButton(
              onPressed: () async {
                // Defer low priority (importance <= 4)
                final taskNotifier = ref.read(tasksProvider(listId).notifier);
                final tomorrow = now.add(const Duration(days: 1));
                final lowPriority = tasks
                    .where((t) => !t.isCompleted && t.importance <= 4)
                    .toList();
                for (final t in lowPriority) {
                  final updated = TaskItem(
                    id: t.id,
                    listId: t.listId,
                    title: t.title,
                    description: t.description,
                    dueDate: DateTime(
                      tomorrow.year,
                      tomorrow.month,
                      tomorrow.day,
                      17,
                      0,
                    ),
                    estimatedDurationMinutes: t.estimatedDurationMinutes,
                    importance: t.importance,
                    complexity: t.complexity,
                    notBefore: t.notBefore,
                    isPinned: t.isPinned,
                    isDivisible: t.isDivisible,
                    link: t.link,
                    dependencies: t.dependencies,
                  );
                  await taskNotifier.updateTask(updated);
                }
                if (context.mounted) {
                  Navigator.of(context).pop();
                  ref.read(appNotificationProvider.notifier).state =
                      AppNotification(
                    'Deferred ${lowPriority.length} low-priority task(s) to tomorrow',
                    icon: Icons.auto_awesome,
                  );
                }
              },
              child: const Text('Defer Low Priority'),
            ),
            FilledButton(
              onPressed: () {
                Navigator.of(context).pop();
                ref.read(appNotificationProvider.notifier).state =
                    const AppNotification(
                  'Day locked in! Focus on least-slack task first',
                  icon: Icons.rocket_launch,
                );
              },
              child: const Text('Start My Day'),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildEveningWindDown(
    BuildContext context,
    WidgetRef ref,
    DateTime now,
    DateTime today,
    List<TaskItem> tasks,
    int workEndMinutes,
  ) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final completedTasks = tasks.where((t) => t.isCompleted).toList();
    final incompleteTasks = tasks.where((t) => !t.isCompleted).toList();

    // Midnight deadline categorization
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

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Header
        Row(
          children: [
            Icon(Icons.bedtime_outlined, size: 26, color: colorScheme.primary),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: Text(
                'Workday Complete — Evening Wrap-Up',
                style: textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            IconButton(
              icon: const Icon(Icons.close, size: 20),
              onPressed: () => Navigator.of(context).pop(),
            ),
          ],
        ),
        const Divider(),
        const SizedBox(height: AppTheme.spacingSm),

        // Accomplishments Summary
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
            borderRadius: BorderRadius.circular(AppTheme.radiusMd),
            border: Border.all(color: Colors.green.withAlpha(80)),
          ),
          child: Row(
            children: [
              const Icon(Icons.task_alt, color: Colors.green, size: 22),
              const SizedBox(width: AppTheme.spacingSm),
              Expanded(
                child: Text(
                  'Completed ${completedTasks.length} task(s) (${completedHours.toStringAsFixed(1)} hours of focus work)',
                  style: textTheme.bodyMedium?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
        ),

        // Midnight Deadlines Attention
        if (dueTonight.isNotEmpty) ...[
          const SizedBox(height: AppTheme.spacingMd),
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
              padding: const EdgeInsets.all(AppTheme.spacingSm),
              margin: const EdgeInsets.only(bottom: 6),
              decoration: BoxDecoration(
                color: Colors.red.withAlpha(20),
                borderRadius: BorderRadius.circular(AppTheme.radiusSm),
                border: Border.all(color: Colors.red.withAlpha(70)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    task.title,
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'Effort: ~${task.estimatedDurationMinutes}m · Due tonight',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Wrap(
                    spacing: 6,
                    runSpacing: 4,
                    children: [
                      FilledButton.tonal(
                        onPressed: () async {
                          // Open 2h evening window
                          final lists =
                              ref.read(taskListsProvider).asData?.value ?? [];
                          final activeList =
                              lists.where((l) => l.id == listId).firstOrNull;
                          if (activeList != null) {
                            final extEnd = min(24 * 60, (now.hour * 60 + now.minute) + 120);
                            final updated = TaskList(
                              id: activeList.id,
                              name: activeList.name,
                              workStartMinutes: activeList.workStartMinutes,
                              workEndMinutes: extEnd,
                              workDays: activeList.workDays,
                            );
                            await ref
                                .read(taskListsProvider.notifier)
                                .updateList(updated);
                          }
                          if (context.mounted) Navigator.of(context).pop();
                        },
                        style: FilledButton.styleFrom(
                          visualDensity: VisualDensity.compact,
                        ),
                        child: const Text('Complete Tonight (+2h Window)'),
                      ),
                      OutlinedButton(
                        onPressed: () async {
                          await ref
                              .read(tasksProvider(listId).notifier)
                              .setCompleted(task.id, true);
                          if (context.mounted) Navigator.of(context).pop();
                        },
                        style: OutlinedButton.styleFrom(
                          visualDensity: VisualDensity.compact,
                        ),
                        child: const Text('Mark Done'),
                      ),
                      TextButton(
                        onPressed: () async {
                          // Postpone to tomorrow
                          final tomorrow = now.add(const Duration(days: 1));
                          final updated = TaskItem(
                            id: task.id,
                            listId: task.listId,
                            title: task.title,
                            dueDate: DateTime(
                              tomorrow.year,
                              tomorrow.month,
                              tomorrow.day,
                              17,
                              0,
                            ),
                            estimatedDurationMinutes: task.estimatedDurationMinutes,
                            importance: task.importance,
                          );
                          await ref
                              .read(tasksProvider(listId).notifier)
                              .updateTask(updated);
                          if (context.mounted) Navigator.of(context).pop();
                        },
                        child: const Text('Postpone (Late)'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ],

        // Flexible Work
        if (dueLater.isNotEmpty) ...[
          const SizedBox(height: AppTheme.spacingMd),
          Text(
            'FLEXIBLE WORK (${dueLater.length} item(s) due later):',
            style: textTheme.labelSmall?.copyWith(
              fontWeight: FontWeight.bold,
              color: colorScheme.onSurfaceVariant,
              letterSpacing: 1.1,
            ),
          ),
          const SizedBox(height: AppTheme.spacingSm),
          Wrap(
            spacing: AppTheme.spacingSm,
            children: [
              OutlinedButton.icon(
                onPressed: () async {
                  final tomorrow = now.add(const Duration(days: 1));
                  final taskNotifier = ref.read(tasksProvider(listId).notifier);
                  for (final t in dueLater) {
                    final updated = TaskItem(
                      id: t.id,
                      listId: t.listId,
                      title: t.title,
                      dueDate: DateTime(
                        tomorrow.year,
                        tomorrow.month,
                        tomorrow.day,
                        17,
                        0,
                      ),
                      estimatedDurationMinutes: t.estimatedDurationMinutes,
                      importance: t.importance,
                    );
                    await taskNotifier.updateTask(updated);
                  }
                  if (context.mounted) {
                    Navigator.of(context).pop();
                    ref.read(appNotificationProvider.notifier).state =
                        AppNotification(
                      'Pushed ${dueLater.length} task(s) to tomorrow morning',
                      icon: Icons.arrow_forward,
                    );
                  }
                },
                icon: const Icon(Icons.arrow_forward, size: 16),
                label: const Text('Push to Tomorrow Morning'),
              ),
              OutlinedButton.icon(
                onPressed: () async {
                  final taskNotifier = ref.read(tasksProvider(listId).notifier);
                  for (final t in dueLater) {
                    final updated = TaskItem(
                      id: t.id,
                      listId: t.listId,
                      title: t.title,
                      dueDate: null, // Move to backlog
                      estimatedDurationMinutes: t.estimatedDurationMinutes,
                      importance: t.importance,
                    );
                    await taskNotifier.updateTask(updated);
                  }
                  if (context.mounted) {
                    Navigator.of(context).pop();
                    ref.read(appNotificationProvider.notifier).state =
                        AppNotification(
                      'Moved ${dueLater.length} task(s) to Backlog',
                      icon: Icons.folder_outlined,
                    );
                  }
                },
                icon: const Icon(Icons.folder_outlined, size: 16),
                label: const Text('Move to Backlog'),
              ),
            ],
          ),
        ],
        const SizedBox(height: AppTheme.spacingLg),

        // Bottom Actions
        Wrap(
          alignment: WrapAlignment.end,
          spacing: AppTheme.spacingSm,
          runSpacing: AppTheme.spacingXs,
          children: [
            OutlinedButton(
              onPressed: () {
                Navigator.of(context).pop();
                ref.read(viewModeProvider.notifier).state = ViewMode.horizon;
              },
              child: const Text("Preview Tomorrow's Plan"),
            ),
            FilledButton(
              onPressed: () {
                Navigator.of(context).pop();
                ref.read(appNotificationProvider.notifier).state =
                    const AppNotification(
                  'Evening wrap-up complete! Have a restful evening.',
                  icon: Icons.nightlight_round,
                );
              },
              child: const Text('Save & Wrap Up'),
            ),
          ],
        ),
      ],
    );
  }

  Widget _bulletPoint(String text, {Color? color}) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('• ', style: TextStyle(fontWeight: FontWeight.bold)),
        Expanded(
          child: Text(
            text,
            style: TextStyle(
              fontSize: 13,
              fontWeight: color != null ? FontWeight.bold : FontWeight.normal,
              color: color,
            ),
          ),
        ),
      ],
    );
  }
}
