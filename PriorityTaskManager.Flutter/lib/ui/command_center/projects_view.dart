import 'dart:math' as math;

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
import 'link_existing_task_dialog.dart';

/// List-scoped backlog and dependency graph, available without a schedule.
class ProjectsView extends ConsumerStatefulWidget {
  const ProjectsView({super.key, required this.listId});

  final String listId;

  @override
  ConsumerState<ProjectsView> createState() => _ProjectsViewState();
}

class _ProjectsViewState extends ConsumerState<ProjectsView> {
  TaskItem? _taskById(List<TaskItem>? tasks, String id) {
    if (tasks == null) return null;
    for (final task in tasks) {
      if (task.id == id) return task;
    }
    return null;
  }

  Future<void> _unlinkDependency(
    TaskItem dependent,
    TaskItem prerequisite,
  ) async {
    final container = ProviderScope.containerOf(context, listen: false);
    final provider = tasksProvider(widget.listId);
    final tasks = container.read(provider).asData?.value;
    final current = _taskById(tasks, dependent.id);
    if (current == null || !current.dependencies.contains(prerequisite.id)) {
      container
          .read(appNotificationProvider.notifier)
          .state = const AppNotification(
        'This dependency is no longer available.',
        icon: Icons.error_outline,
      );
      return;
    }

    final previousIndex = current.dependencies.indexOf(prerequisite.id);
    try {
      await container
          .read(provider.notifier)
          .updateTask(
            current.copyWith(
              dependencies: current.dependencies
                  .where((id) => id != prerequisite.id)
                  .toList(),
            ),
          );
      if (!mounted) return;
      container.read(appNotificationProvider.notifier).state = AppNotification(
        'Dependency removed: ${dependent.title} no longer waits on ${prerequisite.title}',
        icon: Icons.link_off,
        actionLabel: 'Undo',
        onAction: () async {
          final latestTasks = container.read(provider).asData?.value;
          final latest = _taskById(latestTasks, dependent.id);
          final latestPrerequisite = _taskById(latestTasks, prerequisite.id);
          if (latestTasks == null ||
              latest == null ||
              latestPrerequisite == null ||
              latest.recurrenceRule != null ||
              latest.isRecurringOccurrence ||
              latestPrerequisite.recurrenceRule != null ||
              latestPrerequisite.isRecurringOccurrence ||
              latest.dependencies.contains(prerequisite.id) ||
              DependencyGraphBuilder.wouldCreateCycle(
                latestTasks,
                taskId: dependent.id,
                proposedDependencyId: prerequisite.id,
              )) {
            container
                .read(appNotificationProvider.notifier)
                .state = const AppNotification(
              'Could not restore this dependency; the tasks or links changed.',
              icon: Icons.error_outline,
            );
            return;
          }
          final restored = List<String>.of(latest.dependencies)
            ..insert(
              math.min(previousIndex, latest.dependencies.length),
              prerequisite.id,
            );
          await container
              .read(provider.notifier)
              .updateTask(latest.copyWith(dependencies: restored));
          container.read(appNotificationProvider.notifier).state =
              const AppNotification('Dependency restored', icon: Icons.link);
        },
      );
    } on Exception catch (error) {
      if (!mounted) return;
      container.read(appNotificationProvider.notifier).state = AppNotification(
        'Could not remove dependency: $error',
        icon: Icons.error_outline,
      );
    }
  }

  Future<void> _linkExistingFollowUp(TaskItem prerequisite) async {
    final provider = tasksProvider(widget.listId);
    final tasks = ref.read(provider).asData?.value;
    final currentPrerequisite = _taskById(tasks, prerequisite.id);
    if (tasks == null ||
        currentPrerequisite == null ||
        currentPrerequisite.recurrenceRule != null ||
        currentPrerequisite.isRecurringOccurrence) {
      ref.read(appNotificationProvider.notifier).state = const AppNotification(
        'Only one-off tasks in this list can be linked.',
        icon: Icons.error_outline,
      );
      return;
    }

    final selected = await showLinkExistingTaskDialog(
      context,
      tasks: tasks,
      task: currentPrerequisite,
      direction: DependencyLinkDirection.followUp,
    );
    if (!mounted || selected == null) return;

    final latestTasks = ref.read(provider).asData?.value;
    final latestPrerequisite = _taskById(latestTasks, prerequisite.id);
    final dependent = _taskById(latestTasks, selected.id);
    if (latestTasks == null ||
        latestPrerequisite == null ||
        dependent == null ||
        latestPrerequisite.recurrenceRule != null ||
        latestPrerequisite.isRecurringOccurrence ||
        dependent.recurrenceRule != null ||
        dependent.isRecurringOccurrence ||
        dependent.dependencies.contains(prerequisite.id) ||
        DependencyGraphBuilder.wouldCreateCycle(
          latestTasks,
          taskId: dependent.id,
          proposedDependencyId: prerequisite.id,
        )) {
      ref.read(appNotificationProvider.notifier).state = const AppNotification(
        'Could not link these tasks; they changed or would form a cycle.',
        icon: Icons.error_outline,
      );
      return;
    }
    try {
      await ref
          .read(provider.notifier)
          .updateTask(
            dependent.copyWith(
              dependencies: [...dependent.dependencies, prerequisite.id],
            ),
          );
      if (!mounted) return;
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        '${dependent.title} now waits on ${prerequisite.title}',
        icon: Icons.link,
      );
    } on Exception catch (error) {
      if (!mounted) return;
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        'Could not link tasks: $error',
        icon: Icons.error_outline,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final tasksAsync = ref.watch(tasksProvider(widget.listId));
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
      onLinkExistingFollowUp: () => _linkExistingFollowUp(task),
      onToggleCompleted: (completed) async {
        try {
          await ref
              .read(tasksProvider(widget.listId).notifier)
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
            _ChainCard(
              key: ValueKey(
                'project-chain-${chain.roots.isEmpty ? chain.steps.first.task.id : chain.roots.first.id}',
              ),
              chain: chain,
              buildCard: taskCard,
              onUnlink: _unlinkDependency,
            ),
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

class _ChainCard extends StatefulWidget {
  const _ChainCard({
    super.key,
    required this.chain,
    required this.buildCard,
    required this.onUnlink,
  });

  final DependencyChain chain;
  final Widget Function(TaskItem, int?) buildCard;
  final void Function(TaskItem dependent, TaskItem prerequisite) onUnlink;

  @override
  State<_ChainCard> createState() => _ChainCardState();
}

class _ChainCardState extends State<_ChainCard> {
  bool _isExpanded = true;
  bool _hideCompleted = false;

  String _formatEffort(int minutes) => '${minutes ~/ 60}h ${minutes % 60}m';

  @override
  Widget build(BuildContext context) {
    final chain = widget.chain;
    final colorScheme = Theme.of(context).colorScheme;
    final rootLabel = chain.roots.isEmpty
        ? 'Dependency loop'
        : chain.roots.map((root) => root.title).join(' + ');
    final visibleSteps = [
      for (final step in chain.steps)
        if (!_hideCompleted || !step.task.isCompleted) step,
    ];
    final visibleDepths = <String, int>{};
    for (final step in visibleSteps) {
      visibleDepths[step.task.id] = step.task.dependencies
          .map((id) => visibleDepths[id])
          .whereType<int>()
          .fold(
            0,
            (depth, prerequisiteDepth) =>
                math.max(depth, prerequisiteDepth + 1),
          );
    }
    final indices = {
      for (var index = 0; index < visibleSteps.length; index++)
        visibleSteps[index].task.id: index,
    };
    final lanes = <String, double>{};
    final cardOffsets = <String, double>{};
    var offset = 0.0;
    final maxDepth = visibleDepths.isEmpty
        ? 0
        : visibleDepths.values.reduce(math.max);
    for (var depth = 0; depth <= maxDepth; depth++) {
      final atDepth = visibleSteps
          .where((step) => visibleDepths[step.task.id] == depth)
          .toList();
      if (atDepth.isEmpty) continue;
      final width = 44.0 + (atDepth.length - 1) * 12.0;
      for (var index = 0; index < atDepth.length; index++) {
        final id = atDepth[index].task.id;
        lanes[id] = offset + 12 + index * 12;
        cardOffsets[id] = offset + width;
      }
      offset += width;
    }
    final links = <_RailLink>[];
    for (var index = 0; index < visibleSteps.length; index++) {
      final dependent = visibleSteps[index].task;
      final incoming = dependent.dependencies
          .toSet()
          .where(indices.containsKey)
          .toList();
      for (var position = 0; position < incoming.length; position++) {
        final prerequisiteId = incoming[position];
        links.add(
          _RailLink(
            prerequisite: visibleSteps[indices[prerequisiteId]!].task,
            dependent: dependent,
            fromIndex: indices[prerequisiteId]!,
            toIndex: index,
            railX: lanes[prerequisiteId]!,
            branchOffset: (position - (incoming.length - 1) / 2) * 28,
          ),
        );
      }
    }
    final minWidth = math.max(320.0, offset + 260);
    final progress = chain.completedCount / chain.totalCount;
    final summary =
        '${chain.completedCount} of ${chain.totalCount} completed (${(progress * 100).round()}%)';
    final chainId = chain.roots.isEmpty
        ? chain.steps.first.task.id
        : chain.roots.first.id;
    return Card(
      margin: const EdgeInsets.only(bottom: AppTheme.spacingMd),
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingMd),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Tooltip(
              message: _isExpanded ? 'Collapse chain' : 'Expand chain',
              child: InkWell(
                key: ValueKey('project-chain-toggle-$chainId'),
                onTap: () => setState(() => _isExpanded = !_isExpanded),
                borderRadius: BorderRadius.circular(AppTheme.radiusSm),
                child: Row(
                  children: [
                    Icon(
                      Icons.account_tree_outlined,
                      color: colorScheme.primary,
                    ),
                    const SizedBox(width: AppTheme.spacingSm),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            rootLabel,
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                            style: Theme.of(context).textTheme.titleSmall,
                          ),
                          Text(
                            summary,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                    Icon(_isExpanded ? Icons.expand_less : Icons.expand_more),
                  ],
                ),
              ),
            ),
            if (_isExpanded) ...[
              const SizedBox(height: AppTheme.spacingSm),
              LinearProgressIndicator(
                value: progress,
                minHeight: 6,
                borderRadius: BorderRadius.circular(4),
              ),
              const SizedBox(height: AppTheme.spacingXs),
              Text(
                'Total: ${_formatEffort(chain.totalMinutes)} • Remaining: ${_formatEffort(chain.remainingMinutes)}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              Row(
                children: [
                  FilterChip(
                    key: ValueKey('project-hide-completed-$chainId'),
                    label: Text(
                      _hideCompleted
                          ? '${chain.completedCount} completed hidden'
                          : 'Hide completed steps',
                    ),
                    selected: _hideCompleted,
                    onSelected: chain.completedCount == 0
                        ? null
                        : (value) => setState(() => _hideCompleted = value),
                  ),
                  const Spacer(),
                  Text(
                    '${chain.totalCount} steps',
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
              if (visibleSteps.isEmpty)
                const Text('All steps completed.')
              else
                LayoutBuilder(
                  builder: (context, constraints) => SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: SizedBox(
                      width: math.max(constraints.maxWidth, minWidth),
                      child: Column(
                        children: [
                          for (
                            var index = 0;
                            index < visibleSteps.length;
                            index++
                          )
                            _DependencyRailRow(
                              step: visibleSteps[index],
                              index: index,
                              leadingWidth:
                                  cardOffsets[visibleSteps[index].task.id]!,
                              nodeX: lanes[visibleSteps[index].task.id]!,
                              activeLinks: links
                                  .where(
                                    (link) =>
                                        link.fromIndex <= index &&
                                        index <= link.toIndex,
                                  )
                                  .toList(),
                              incomingLinks: links
                                  .where((link) => link.toIndex == index)
                                  .toList(),
                              color: colorScheme.outline,
                              hoverColor: colorScheme.error,
                              onUnlink: widget.onUnlink,
                              child: widget.buildCard(
                                visibleSteps[index].task,
                                visibleSteps[index].depth + 1,
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

class _RailLink {
  const _RailLink({
    required this.prerequisite,
    required this.dependent,
    required this.fromIndex,
    required this.toIndex,
    required this.railX,
    required this.branchOffset,
  });

  final TaskItem prerequisite;
  final TaskItem dependent;
  final int fromIndex;
  final int toIndex;
  final double railX;
  final double branchOffset;
}

class _DependencyRailRow extends StatefulWidget {
  const _DependencyRailRow({
    required this.step,
    required this.index,
    required this.leadingWidth,
    required this.nodeX,
    required this.activeLinks,
    required this.incomingLinks,
    required this.color,
    required this.hoverColor,
    required this.onUnlink,
    required this.child,
  });

  final DependencyStep step;
  final int index;
  final double leadingWidth;
  final double nodeX;
  final List<_RailLink> activeLinks;
  final List<_RailLink> incomingLinks;
  final Color color;
  final Color hoverColor;
  final void Function(TaskItem, TaskItem) onUnlink;
  final Widget child;

  @override
  State<_DependencyRailRow> createState() => _DependencyRailRowState();
}

class _DependencyRailRowState extends State<_DependencyRailRow> {
  String? _hoveredPrerequisiteId;

  @override
  Widget build(BuildContext context) => IntrinsicHeight(
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SizedBox(
          width: widget.leadingWidth,
          child: Stack(
            fit: StackFit.expand,
            children: [
              CustomPaint(
                key: ValueKey('project-rail-${widget.step.task.id}'),
                painter: _RailConnectorPainter(
                  color: widget.color,
                  hoverColor: widget.hoverColor,
                  rowIndex: widget.index,
                  nodeX: widget.nodeX,
                  activeLinks: widget.activeLinks,
                  incomingLinks: widget.incomingLinks,
                  hoveredPrerequisiteId: _hoveredPrerequisiteId,
                ),
              ),
              for (final link in widget.incomingLinks)
                Positioned.fill(
                  child: Align(
                    alignment: Alignment.centerRight,
                    child: Transform.translate(
                      offset: Offset(0, link.branchOffset),
                      child: MouseRegion(
                        onEnter: (_) => setState(
                          () => _hoveredPrerequisiteId = link.prerequisite.id,
                        ),
                        onExit: (_) =>
                            setState(() => _hoveredPrerequisiteId = null),
                        child: Tooltip(
                          message:
                              'Remove dependency: ${link.dependent.title} will no longer wait on ${link.prerequisite.title}',
                          child: SizedBox(
                            width: math.max(
                              28,
                              widget.leadingWidth - link.railX,
                            ),
                            height: 28,
                            child: Align(
                              alignment: Alignment.centerRight,
                              child: IconButton(
                                key: ValueKey(
                                  'project-unlink-${link.dependent.id}-${link.prerequisite.id}',
                                ),
                                icon: const Icon(Icons.close, size: 16),
                                color:
                                    _hoveredPrerequisiteId ==
                                        link.prerequisite.id
                                    ? widget.hoverColor
                                    : widget.color,
                                padding: EdgeInsets.zero,
                                constraints: const BoxConstraints.tightFor(
                                  width: 28,
                                  height: 28,
                                ),
                                onPressed: () => widget.onUnlink(
                                  link.dependent,
                                  link.prerequisite,
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ),
        Expanded(
          child: ConstrainedBox(
            constraints: BoxConstraints(
              minHeight: widget.incomingLinks.length * 28 + 24,
            ),
            child: widget.child,
          ),
        ),
      ],
    ),
  );
}

class _RailConnectorPainter extends CustomPainter {
  const _RailConnectorPainter({
    required this.color,
    required this.hoverColor,
    required this.rowIndex,
    required this.nodeX,
    required this.activeLinks,
    required this.incomingLinks,
    required this.hoveredPrerequisiteId,
  });

  final Color color;
  final Color hoverColor;
  final int rowIndex;
  final double nodeX;
  final List<_RailLink> activeLinks;
  final List<_RailLink> incomingLinks;
  final String? hoveredPrerequisiteId;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = 2
      ..style = PaintingStyle.stroke;
    final middle = size.height / 2;
    for (final link in activeLinks) {
      final start = link.fromIndex == rowIndex ? middle : 0.0;
      final end = link.toIndex == rowIndex
          ? middle + link.branchOffset - 8
          : size.height;
      if (start < end) {
        canvas.drawLine(
          Offset(link.railX, start),
          Offset(link.railX, end),
          paint,
        );
      }
    }
    canvas.drawCircle(Offset(nodeX, middle), 3, paint);
    canvas.drawLine(Offset(nodeX, middle), Offset(size.width, middle), paint);
    for (final link in incomingLinks) {
      final branchY = middle + link.branchOffset;
      paint.color = hoveredPrerequisiteId == link.prerequisite.id
          ? hoverColor
          : color;
      final branch = Path()
        ..moveTo(link.railX, branchY - 8)
        ..quadraticBezierTo(link.railX, branchY, link.railX + 8, branchY)
        ..lineTo(size.width, branchY);
      canvas.drawPath(branch, paint);
    }
  }

  @override
  bool shouldRepaint(_RailConnectorPainter oldDelegate) =>
      color != oldDelegate.color ||
      hoverColor != oldDelegate.hoverColor ||
      rowIndex != oldDelegate.rowIndex ||
      nodeX != oldDelegate.nodeX ||
      activeLinks != oldDelegate.activeLinks ||
      incomingLinks != oldDelegate.incomingLinks ||
      hoveredPrerequisiteId != oldDelegate.hoveredPrerequisiteId;
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
    required this.onLinkExistingFollowUp,
    required this.onToggleCompleted,
  });

  final TaskItem task;
  final DependencyGraph graph;
  final bool isGuest;
  final int? stepNumber;
  final VoidCallback onTap;
  final VoidCallback onFollowUp;
  final VoidCallback onLinkExistingFollowUp;
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
                      PopupMenuButton<_FollowUpAction>(
                        tooltip: 'Follow-up actions',
                        onSelected: (action) {
                          switch (action) {
                            case _FollowUpAction.create:
                              onFollowUp();
                            case _FollowUpAction.link:
                              onLinkExistingFollowUp();
                          }
                        },
                        itemBuilder: (context) => const [
                          PopupMenuItem(
                            value: _FollowUpAction.create,
                            child: Text('Create New Follow-Up'),
                          ),
                          PopupMenuItem(
                            value: _FollowUpAction.link,
                            child: Text('Link Existing Task...'),
                          ),
                        ],
                        child: const Padding(
                          padding: EdgeInsets.symmetric(vertical: 8),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(Icons.add, size: 16),
                              SizedBox(width: AppTheme.spacingXs),
                              Text('Add Follow-Up'),
                              Icon(Icons.arrow_drop_down, size: 18),
                            ],
                          ),
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

enum _FollowUpAction { create, link }
