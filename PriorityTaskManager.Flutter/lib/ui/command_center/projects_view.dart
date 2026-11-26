import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../models/task_item.dart';
import '../../providers/app_notifications_provider.dart';
import '../../providers/selection_provider.dart';
import '../../providers/session_provider.dart';
import '../../providers/task_providers.dart';
import '../../utils/dependency_graph_builder.dart';
import '../theme/app_theme.dart';

/// List-scoped backlog and dependency graph, available without a schedule.
class ProjectsView extends ConsumerWidget {
  const ProjectsView({super.key, required this.listId});

  final String listId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tasksAsync = ref.watch(tasksProvider(listId));
    if (tasksAsync.isLoading && !tasksAsync.hasValue) {
      return const Center(child: CircularProgressIndicator());
    }
    if (tasksAsync.hasError) {
      return Center(child: Text('Could not load tasks: ${tasksAsync.error}'));
    }

    final tasks = tasksAsync.requireValue;
    final graph = DependencyGraphBuilder.build(tasks);
    final isGuest =
        ref.watch(sessionControllerProvider).asData?.value.status ==
        SessionStatus.guest;
    final colorScheme = Theme.of(context).colorScheme;

    Widget taskCard(TaskItem task, int? stepNumber) => _ProjectTaskCard(
      key: ValueKey('project-task-${task.id}-$stepNumber'),
      task: task,
      graph: graph,
      isGuest: isGuest,
      stepNumber: stepNumber,
      onTap: () => ref.read(selectedInspectorProvider.notifier).state =
          InspectorTarget(kind: InspectorKind.task, id: task.id, task: task),
      onFollowUp: () =>
          ref.read(selectedInspectorProvider.notifier).state = InspectorTarget(
            kind: InspectorKind.task,
            prerequisiteTaskId: task.id,
          ),
      onToggleCompleted: (completed) async {
        try {
          await ref
              .read(tasksProvider(listId).notifier)
              .setCompleted(task.id, completed);
          if (!context.mounted) return;
          ref.read(appNotificationProvider.notifier).state = AppNotification(
            completed ? 'Task completed' : 'Task reopened',
            icon: completed ? Icons.task_alt : Icons.undo,
          );
        } on Exception catch (error) {
          if (!context.mounted) return;
          ref.read(appNotificationProvider.notifier).state = AppNotification(
            'Could not update task: $error',
            icon: Icons.error_outline,
          );
        }
      },
    );

    Widget standaloneSection(String title, List<TaskItem> items) => Padding(
      padding: const EdgeInsets.only(top: AppTheme.spacingLg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '$title (${items.length})',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: AppTheme.spacingSm),
          for (final task in items) taskCard(task, null),
        ],
      ),
    );

    return ListView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: [
        Text(
          'Projects & Dependency Backlog',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: AppTheme.spacingXs),
        Text(
          isGuest
              ? 'Guest mode: ready tasks are available for manual execution.'
              : 'Readiness reflects prerequisites, not a reserved schedule slot.',
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: colorScheme.onSurfaceVariant),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        Wrap(
          spacing: AppTheme.spacingSm,
          runSpacing: AppTheme.spacingSm,
          children: [
            _SummaryMetric(label: 'Total', count: graph.totalCount),
            _SummaryMetric(label: 'Ready', count: graph.readyCount),
            _SummaryMetric(label: 'Blocked', count: graph.blockedCount),
            _SummaryMetric(
              label: 'Completed',
              count: graph.completedTasks.length,
            ),
          ],
        ),
        if (tasks.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AppTheme.spacingXl),
            child: Column(
              children: [
                const Icon(Icons.account_tree_outlined, size: 40),
                const SizedBox(height: AppTheme.spacingSm),
                const Text('No tasks in this list yet.'),
                const SizedBox(height: AppTheme.spacingMd),
                FilledButton.icon(
                  onPressed: () =>
                      ref.read(selectedInspectorProvider.notifier).state =
                          const InspectorTarget(kind: InspectorKind.task),
                  icon: const Icon(Icons.add),
                  label: const Text('Add a task'),
                ),
              ],
            ),
          ),
        if (graph.chains.isNotEmpty) ...[
          const SizedBox(height: AppTheme.spacingLg),
          Text(
            'Dependency Step-Chains (${graph.chains.length})',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: AppTheme.spacingSm),
          for (final chain in graph.chains)
            _ChainCard(chain: chain, buildCard: taskCard),
        ],
        if (graph.readyStandalone.isNotEmpty)
          standaloneSection('Ready & Active', graph.readyStandalone),
        if (graph.somedayStandalone.isNotEmpty)
          standaloneSection('Someday / Flexible', graph.somedayStandalone),
        const SizedBox(height: AppTheme.spacingLg),
        ExpansionTile(
          key: const ValueKey('projects-completed'),
          tilePadding: EdgeInsets.zero,
          title: Text('Completed Tasks (${graph.completedTasks.length})'),
          children: [
            for (final task in graph.completedTasks) taskCard(task, null),
          ],
        ),
      ],
    );
  }
}

class _SummaryMetric extends StatelessWidget {
  const _SummaryMetric({required this.label, required this.count});

  final String label;
  final int count;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(
      horizontal: AppTheme.spacingSm,
      vertical: AppTheme.spacingXs,
    ),
    decoration: BoxDecoration(
      color: Theme.of(context).colorScheme.surfaceContainerLow,
      borderRadius: BorderRadius.circular(AppTheme.radiusMd),
    ),
    child: Text(
      '$label: $count',
      style: Theme.of(context).textTheme.labelMedium,
    ),
  );
}

class _ChainCard extends StatelessWidget {
  const _ChainCard({required this.chain, required this.buildCard});

  final DependencyChain chain;
  final Widget Function(TaskItem, int?) buildCard;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final rootLabel = chain.roots.isEmpty
        ? 'Dependency loop'
        : chain.roots.map((root) => root.title).join(' + ');
    return Card(
      margin: const EdgeInsets.only(bottom: AppTheme.spacingMd),
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingMd),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.account_tree_outlined, color: colorScheme.primary),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Text(
                    rootLabel,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                ),
                Text(
                  '${chain.steps.length} steps',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
            if (chain.hasCycle)
              Padding(
                padding: const EdgeInsets.symmetric(
                  vertical: AppTheme.spacingSm,
                ),
                child: Text(
                  'Circular dependencies detected. Review links in the inspector.',
                  style: Theme.of(
                    context,
                  ).textTheme.bodySmall?.copyWith(color: colorScheme.error),
                ),
              ),
            const SizedBox(height: AppTheme.spacingSm),
            for (var index = 0; index < chain.steps.length; index++)
              Padding(
                padding: EdgeInsets.only(
                  left:
                      chain.steps[index].depth.clamp(0, 4) * AppTheme.spacingMd,
                ),
                child: IntrinsicHeight(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      SizedBox(
                        width: AppTheme.spacingLg,
                        child: CustomPaint(
                          painter: _StepConnectorPainter(
                            color: colorScheme.outline,
                            isRoot: chain.steps[index].depth == 0,
                            hasNext: index < chain.steps.length - 1,
                          ),
                        ),
                      ),
                      Expanded(
                        child: buildCard(
                          chain.steps[index].task,
                          chain.steps[index].depth + 1,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _StepConnectorPainter extends CustomPainter {
  const _StepConnectorPainter({
    required this.color,
    required this.isRoot,
    required this.hasNext,
  });

  final Color color;
  final bool isRoot;
  final bool hasNext;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = 2
      ..style = PaintingStyle.stroke;
    final middle = size.height / 2;
    if (isRoot) {
      canvas.drawCircle(Offset(8, middle), 3, paint);
    } else {
      canvas.drawLine(const Offset(8, 0), Offset(8, middle), paint);
    }
    canvas.drawLine(Offset(8, middle), Offset(size.width, middle), paint);
    if (hasNext) {
      canvas.drawLine(Offset(8, middle), Offset(8, size.height), paint);
    }
  }

  @override
  bool shouldRepaint(_StepConnectorPainter oldDelegate) =>
      color != oldDelegate.color ||
      isRoot != oldDelegate.isRoot ||
      hasNext != oldDelegate.hasNext;
}

class _ProjectTaskCard extends StatelessWidget {
  const _ProjectTaskCard({
    super.key,
    required this.task,
    required this.graph,
    required this.isGuest,
    required this.stepNumber,
    required this.onTap,
    required this.onFollowUp,
    required this.onToggleCompleted,
  });

  final TaskItem task;
  final DependencyGraph graph;
  final bool isGuest;
  final int? stepNumber;
  final VoidCallback onTap;
  final VoidCallback onFollowUp;
  final ValueChanged<bool> onToggleCompleted;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final readiness = graph.readinessFor(task);
    final waitingOn = graph.uncompletedPrerequisites[task.id]!;
    final waitingNames = waitingOn.map((item) => item.title).join(', ');
    final isBlocked = readiness == ProjectTaskReadiness.blocked;
    final isCompleted = readiness == ProjectTaskReadiness.completed;
    final statusColor = isBlocked
        ? (Theme.of(context).brightness == Brightness.dark
              ? Colors.amber.shade300
              : Colors.amber.shade800)
        : colorScheme.secondary;
    final statusLabel = isCompleted
        ? 'Completed'
        : isBlocked
        ? 'Blocked by: $waitingNames'
        : isGuest
        ? 'Ready for manual execution'
        : 'Ready to Schedule';

    return Card(
      margin: const EdgeInsets.only(bottom: AppTheme.spacingSm),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.spacingSm),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ExcludeSemantics(
                excluding: isBlocked,
                child: Checkbox(
                  visualDensity: VisualDensity.compact,
                  value: task.isCompleted,
                  onChanged:
                      (isBlocked && !isCompleted) || task.recurrenceRule != null
                      ? null
                      : (value) {
                          if (value != null) onToggleCompleted(value);
                        },
                ),
              ),
              const SizedBox(width: AppTheme.spacingXs),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (stepNumber != null)
                      Text(
                        'Step $stepNumber',
                        style: Theme.of(context).textTheme.labelSmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    Text(
                      task.title,
                      style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        decoration: isCompleted
                            ? TextDecoration.lineThrough
                            : null,
                      ),
                    ),
                    const SizedBox(height: AppTheme.spacingXs),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: AppTheme.spacingSm,
                        vertical: AppTheme.spacingXs,
                      ),
                      decoration: BoxDecoration(
                        color: statusColor.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(AppTheme.radiusSm),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            isCompleted
                                ? Icons.check_circle_outline
                                : isBlocked
                                ? Icons.lock_outline
                                : Icons.check_circle_outline,
                            size: 16,
                            color: statusColor,
                          ),
                          const SizedBox(width: AppTheme.spacingXs),
                          Flexible(
                            child: Text(
                              statusLabel,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: Theme.of(context).textTheme.labelSmall
                                  ?.copyWith(color: statusColor),
                            ),
                          ),
                        ],
                      ),
                    ),
                    if (isBlocked)
                      Text(
                        'Waiting on: $waitingNames',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    if (task.dueDate != null)
                      Text(
                        'Due ${DateFormat.yMMMd().format(task.dueDate!)}',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    if (task.recurrenceRule == null &&
                        !task.isRecurringOccurrence)
                      TextButton.icon(
                        onPressed: onFollowUp,
                        icon: const Icon(Icons.add, size: 16),
                        label: const Text('Add Follow-Up'),
                        style: TextButton.styleFrom(
                          visualDensity: VisualDensity.compact,
                          padding: EdgeInsets.zero,
                        ),
                      ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
