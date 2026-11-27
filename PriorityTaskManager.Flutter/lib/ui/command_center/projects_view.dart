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

enum _ProjectsFilter { all, ready, blocked, someday, completed }

/// List-scoped backlog and dependency graph, available without a schedule.
class ProjectsView extends ConsumerStatefulWidget {
  const ProjectsView({super.key, required this.listId});

  final String listId;

  @override
  ConsumerState<ProjectsView> createState() => _ProjectsViewState();
}

class _ProjectsViewState extends ConsumerState<ProjectsView> {
  final _searchController = TextEditingController();
  String _searchQuery = '';
  _ProjectsFilter _filter = _ProjectsFilter.all;
  String? _draggedTaskId;

  @override
  void didUpdateWidget(covariant ProjectsView oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.listId != widget.listId) {
      _searchController.clear();
      _searchQuery = '';
      _filter = _ProjectsFilter.all;
      _draggedTaskId = null;
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  TaskItem? _taskById(List<TaskItem>? tasks, String id) {
    if (tasks == null) return null;
    for (final task in tasks) {
      if (task.id == id) return task;
    }
    return null;
  }

  bool _isOneOff(TaskItem task) =>
      task.recurrenceRule == null && !task.isRecurringOccurrence;

  String? _dropLinkError(
    List<TaskItem>? tasks,
    String dependentId,
    String prerequisiteId,
  ) {
    final dependent = _taskById(tasks, dependentId);
    final prerequisite = _taskById(tasks, prerequisiteId);
    if (tasks == null || dependent == null || prerequisite == null) {
      return 'These tasks are no longer available in this list.';
    }
    if (!_isOneOff(dependent) || !_isOneOff(prerequisite)) {
      return 'Recurring tasks cannot be linked.';
    }
    if (dependent.dependencies.contains(prerequisiteId)) {
      return 'These tasks are already linked.';
    }
    if (DependencyGraphBuilder.wouldCreateCycle(
      tasks,
      taskId: dependentId,
      proposedDependencyId: prerequisiteId,
    )) {
      return 'This link would create a circular dependency.';
    }
    return null;
  }

  Future<void> _linkDroppedTask(
    String dependentId,
    String prerequisiteId,
  ) async {
    final container = ProviderScope.containerOf(context, listen: false);
    final provider = tasksProvider(widget.listId);
    final currentTasks = container.read(provider).asData?.value;
    final dependent = _taskById(currentTasks, dependentId);
    final prerequisite = _taskById(currentTasks, prerequisiteId);
    final error = _dropLinkError(currentTasks, dependentId, prerequisiteId);
    if (error != null || dependent == null || prerequisite == null) {
      container.read(appNotificationProvider.notifier).state = AppNotification(
        'Could not link tasks: ${error ?? 'Tasks are unavailable.'}',
        icon: Icons.error_outline,
      );
      return;
    }

    try {
      await container
          .read(provider.notifier)
          .updateTask(
            dependent.copyWith(
              dependencies: [...dependent.dependencies, prerequisiteId],
            ),
          );
      container.read(appNotificationProvider.notifier).state = AppNotification(
        '${dependent.title} now waits on ${prerequisite.title}',
        icon: Icons.link,
        actionLabel: 'Undo',
        onAction: () async {
          final latest = _taskById(
            container.read(provider).asData?.value,
            dependentId,
          );
          if (latest == null || !latest.dependencies.contains(prerequisiteId)) {
            container
                .read(appNotificationProvider.notifier)
                .state = const AppNotification(
              'Could not undo this link; the task or dependency changed.',
              icon: Icons.error_outline,
            );
            return;
          }
          try {
            await container
                .read(provider.notifier)
                .updateTask(
                  latest.copyWith(
                    dependencies: latest.dependencies
                        .where((id) => id != prerequisiteId)
                        .toList(),
                  ),
                );
            container.read(appNotificationProvider.notifier).state =
                const AppNotification('Link undone', icon: Icons.link_off);
          } on Exception catch (error) {
            container
                .read(appNotificationProvider.notifier)
                .state = AppNotification(
              'Could not undo link: $error',
              icon: Icons.error_outline,
            );
          }
        },
      );
    } on Exception catch (error) {
      container.read(appNotificationProvider.notifier).state = AppNotification(
        'Could not link tasks: $error',
        icon: Icons.error_outline,
      );
    }
  }

  Future<void> _detachDroppedTask(String taskId) async {
    final container = ProviderScope.containerOf(context, listen: false);
    final provider = tasksProvider(widget.listId);
    final current = _taskById(container.read(provider).asData?.value, taskId);
    if (current == null ||
        !_isOneOff(current) ||
        current.dependencies.isEmpty) {
      container
          .read(appNotificationProvider.notifier)
          .state = const AppNotification(
        'Could not detach this task; its links changed.',
        icon: Icons.error_outline,
      );
      return;
    }
    final previousDependencies = List<String>.of(current.dependencies);
    try {
      await container
          .read(provider.notifier)
          .updateTask(current.copyWith(dependencies: []));
      container.read(appNotificationProvider.notifier).state = AppNotification(
        '${current.title} detached from its prerequisites',
        icon: Icons.link_off,
        actionLabel: 'Undo',
        onAction: () async {
          final latestTasks = container.read(provider).asData?.value;
          final latest = _taskById(latestTasks, taskId);
          if (latestTasks == null ||
              latest == null ||
              !_isOneOff(latest) ||
              previousDependencies.any((id) {
                final prerequisite = _taskById(latestTasks, id);
                return prerequisite == null ||
                    !_isOneOff(prerequisite) ||
                    (!latest.dependencies.contains(id) &&
                        DependencyGraphBuilder.wouldCreateCycle(
                          latestTasks,
                          taskId: taskId,
                          proposedDependencyId: id,
                        ));
              })) {
            container
                .read(appNotificationProvider.notifier)
                .state = const AppNotification(
              'Could not restore these links; the tasks or dependencies changed.',
              icon: Icons.error_outline,
            );
            return;
          }
          final restored = [
            ...previousDependencies,
            ...latest.dependencies.where(
              (id) => !previousDependencies.contains(id),
            ),
          ];
          try {
            await container
                .read(provider.notifier)
                .updateTask(latest.copyWith(dependencies: restored));
            container.read(appNotificationProvider.notifier).state =
                const AppNotification('Links restored', icon: Icons.link);
          } on Exception catch (error) {
            container
                .read(appNotificationProvider.notifier)
                .state = AppNotification(
              'Could not restore links: $error',
              icon: Icons.error_outline,
            );
          }
        },
      );
    } on Exception catch (error) {
      container.read(appNotificationProvider.notifier).state = AppNotification(
        'Could not detach task: $error',
        icon: Icons.error_outline,
      );
    }
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

  Future<void> _linkExistingTask(
    TaskItem task,
    DependencyLinkDirection direction,
  ) async {
    final provider = tasksProvider(widget.listId);
    final tasks = ref.read(provider).asData?.value;
    final currentTask = _taskById(tasks, task.id);
    if (tasks == null || currentTask == null || !_isOneOff(currentTask)) {
      ref.read(appNotificationProvider.notifier).state = const AppNotification(
        'Only one-off tasks in this list can be linked.',
        icon: Icons.error_outline,
      );
      return;
    }

    final selected = await showLinkExistingTaskDialog(
      context,
      tasks: tasks,
      task: currentTask,
      direction: direction,
    );
    if (!mounted || selected == null) return;

    final latestTasks = ref.read(provider).asData?.value;
    final isFollowUp = direction == DependencyLinkDirection.followUp;
    final dependentId = isFollowUp ? selected.id : task.id;
    final prerequisiteId = isFollowUp ? task.id : selected.id;
    final dependent = _taskById(latestTasks, dependentId);
    final prerequisite = _taskById(latestTasks, prerequisiteId);
    if (latestTasks == null ||
        dependent == null ||
        prerequisite == null ||
        _dropLinkError(latestTasks, dependentId, prerequisiteId) != null) {
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
              dependencies: [...dependent.dependencies, prerequisiteId],
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

  Future<void> _archiveTask(TaskItem task) async {
    if (ref.read(sessionControllerProvider).asData?.value.status !=
        SessionStatus.authenticated) {
      ref.read(appNotificationProvider.notifier).state = const AppNotification(
        'Archive is available after signing in.',
        icon: Icons.info_outline,
      );
      return;
    }

    final provider = tasksProvider(widget.listId);
    final current = _taskById(ref.read(provider).asData?.value, task.id);
    if (current == null) {
      ref.read(appNotificationProvider.notifier).state = const AppNotification(
        'Could not archive this task; it is no longer available.',
        icon: Icons.error_outline,
      );
      return;
    }
    if (!_isOneOff(current)) {
      ref.read(selectedInspectorProvider.notifier).state = InspectorTarget(
        kind: InspectorKind.task,
        id: current.id,
        task: current,
      );
      ref.read(appNotificationProvider.notifier).state = const AppNotification(
        'Use the inspector archive action to choose a recurring-task scope.',
        icon: Icons.info_outline,
      );
      return;
    }

    try {
      await ref.read(provider.notifier).archiveTask(current.id);
      if (!mounted) return;
      if (ref.read(selectedInspectorProvider).id == current.id) {
        ref.read(selectedInspectorProvider.notifier).state =
            const InspectorTarget.none();
      }
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        '${current.title} archived',
        icon: Icons.archive_outlined,
      );
    } on Exception catch (error) {
      if (!mounted) return;
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        'Could not archive task: $error',
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
    final somedayIds = graph.somedayStandalone.map((task) => task.id).toSet();
    bool matches(TaskItem task) {
      if (!task.title.toLowerCase().contains(_searchQuery)) return false;
      return switch (_filter) {
        _ProjectsFilter.all => true,
        _ProjectsFilter.ready =>
          graph.readinessFor(task) == ProjectTaskReadiness.ready &&
              !somedayIds.contains(task.id),
        _ProjectsFilter.blocked =>
          graph.readinessFor(task) == ProjectTaskReadiness.blocked,
        _ProjectsFilter.someday => somedayIds.contains(task.id),
        _ProjectsFilter.completed =>
          graph.readinessFor(task) == ProjectTaskReadiness.completed,
      };
    }

    final matchingIds = {
      for (final task in tasks)
        if (matches(task)) task.id,
    };
    final visibleChains = _filter == _ProjectsFilter.completed
        ? <DependencyChain>[]
        : [
            for (final chain in graph.chains)
              if (chain.steps.any((step) => matchingIds.contains(step.task.id)))
                chain,
          ];
    final readyStandalone = graph.readyStandalone.where(matches).toList();
    final somedayStandalone = graph.somedayStandalone.where(matches).toList();
    final completedTasks = graph.completedTasks.where(matches).toList();
    final criticalTaskIds = {
      for (final chain in graph.chains) ...chain.criticalPathTaskIds,
    };
    final draggedTask = _draggedTaskId == null
        ? null
        : _taskById(tasks, _draggedTaskId!);

    Widget taskCard(TaskItem task, int? stepNumber) {
      final card = _ProjectTaskCard(
        key: ValueKey('project-task-${task.id}-$stepNumber'),
        task: task,
        graph: graph,
        isGuest: isGuest,
        stepNumber: stepNumber,
        isCriticalPath: stepNumber != null && criticalTaskIds.contains(task.id),
        onTap: () => ref.read(selectedInspectorProvider.notifier).state =
            InspectorTarget(kind: InspectorKind.task, id: task.id, task: task),
        onFollowUp: () => ref.read(selectedInspectorProvider.notifier).state =
            InspectorTarget(
              kind: InspectorKind.task,
              prerequisiteTaskId: task.id,
            ),
        onLinkExistingFollowUp: () =>
            _linkExistingTask(task, DependencyLinkDirection.followUp),
        onLinkPrerequisite: () =>
            _linkExistingTask(task, DependencyLinkDirection.prerequisite),
        onLinkFollowUp: () =>
            _linkExistingTask(task, DependencyLinkDirection.followUp),
        onDetach: () => _detachDroppedTask(task.id),
        onArchive: () => _archiveTask(task),
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
      final draggable = _isOneOff(task)
          ? LongPressDraggable<String>(
              key: ValueKey('project-drag-${task.id}-$stepNumber'),
              data: task.id,
              dragAnchorStrategy: pointerDragAnchorStrategy,
              onDragStarted: () => setState(() => _draggedTaskId = task.id),
              onDragEnd: (_) {
                if (mounted) setState(() => _draggedTaskId = null);
              },
              feedback: Material(
                color: Colors.transparent,
                child: Opacity(
                  opacity: 0.8,
                  child: Card(
                    elevation: 8,
                    color: colorScheme.surfaceContainerHigh,
                    child: Padding(
                      padding: const EdgeInsets.all(AppTheme.spacingSm),
                      child: SizedBox(
                        width: 200,
                        child: Row(
                          children: [
                            const Icon(Icons.drag_indicator),
                            const SizedBox(width: AppTheme.spacingXs),
                            Expanded(
                              child: Text(
                                task.title,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              childWhenDragging: Opacity(opacity: 0.4, child: card),
              child: card,
            )
          : card;
      return DragTarget<String>(
        key: ValueKey('project-drop-${task.id}-$stepNumber'),
        onWillAcceptWithDetails: (details) =>
            _dropLinkError(
              ref.read(tasksProvider(widget.listId)).asData?.value,
              details.data,
              task.id,
            ) ==
            null,
        onAcceptWithDetails: (details) =>
            _linkDroppedTask(details.data, task.id),
        builder: (context, candidates, rejected) {
          final valid = candidates.isNotEmpty && _draggedTaskId != task.id;
          final invalid = rejected.isNotEmpty && _draggedTaskId != task.id;
          if (!valid && !invalid) return draggable;
          final indicatorColor = valid
              ? colorScheme.primary
              : colorScheme.error;
          final message = valid
              ? 'Make follow-up to ${task.title}'
              : _dropLinkError(tasks, rejected.first, task.id) ??
                    'Cannot link these tasks.';
          return DecoratedBox(
            decoration: BoxDecoration(
              border: Border.all(color: indicatorColor, width: 2),
              borderRadius: BorderRadius.circular(AppTheme.radiusMd),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                draggable,
                Padding(
                  padding: const EdgeInsets.all(AppTheme.spacingXs),
                  child: Row(
                    children: [
                      Icon(
                        valid ? Icons.link : Icons.block,
                        size: 18,
                        color: indicatorColor,
                      ),
                      const SizedBox(width: AppTheme.spacingXs),
                      Flexible(
                        child: Text(
                          message,
                          style: Theme.of(context).textTheme.labelSmall
                              ?.copyWith(color: indicatorColor),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          );
        },
      );
    }

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
        TextField(
          key: const ValueKey('projects-search'),
          controller: _searchController,
          decoration: InputDecoration(
            labelText: 'Search tasks by title',
            prefixIcon: const Icon(Icons.search),
            suffixIcon: _searchQuery.isEmpty
                ? null
                : IconButton(
                    tooltip: 'Clear search',
                    icon: const Icon(Icons.clear),
                    onPressed: () {
                      _searchController.clear();
                      setState(() => _searchQuery = '');
                    },
                  ),
          ),
          onChanged: (value) =>
              setState(() => _searchQuery = value.trim().toLowerCase()),
        ),
        const SizedBox(height: AppTheme.spacingSm),
        Wrap(
          spacing: AppTheme.spacingSm,
          runSpacing: AppTheme.spacingXs,
          children: [
            for (final filter in _ProjectsFilter.values)
              ChoiceChip(
                key: ValueKey('projects-filter-${filter.name}'),
                label: Text(switch (filter) {
                  _ProjectsFilter.all => 'All',
                  _ProjectsFilter.ready => 'Ready',
                  _ProjectsFilter.blocked => 'Blocked',
                  _ProjectsFilter.someday => 'Someday',
                  _ProjectsFilter.completed => 'Completed',
                }),
                selected: _filter == filter,
                onSelected: (selected) => setState(
                  () => _filter = selected ? filter : _ProjectsFilter.all,
                ),
              ),
          ],
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
        if (tasks.isNotEmpty && matchingIds.isEmpty)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: AppTheme.spacingLg),
            child: Text('No tasks match your search or filter.'),
          ),
        if (visibleChains.isNotEmpty) ...[
          const SizedBox(height: AppTheme.spacingLg),
          Text(
            'Dependency Step-Chains (${visibleChains.length})',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: AppTheme.spacingSm),
          for (final chain in visibleChains)
            _ChainCard(
              key: ValueKey(
                'project-chain-${chain.roots.isEmpty ? chain.steps.first.task.id : chain.roots.first.id}',
              ),
              chain: chain,
              visibleTaskIds: matchingIds,
              buildCard: taskCard,
              onUnlink: _unlinkDependency,
            ),
        ],
        if (draggedTask != null &&
            _isOneOff(draggedTask) &&
            draggedTask.dependencies.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(top: AppTheme.spacingSm),
            child: DragTarget<String>(
              key: const ValueKey('project-detach-zone'),
              onWillAcceptWithDetails: (details) {
                final current = _taskById(
                  ref.read(tasksProvider(widget.listId)).asData?.value,
                  details.data,
                );
                return details.data == _draggedTaskId &&
                    current != null &&
                    _isOneOff(current) &&
                    current.dependencies.isNotEmpty;
              },
              onAcceptWithDetails: (details) =>
                  _detachDroppedTask(details.data),
              builder: (context, candidates, rejected) {
                final active = candidates.isNotEmpty;
                final color = active
                    ? colorScheme.primary
                    : colorScheme.outline;
                return Container(
                  padding: const EdgeInsets.all(AppTheme.spacingMd),
                  decoration: BoxDecoration(
                    border: Border.all(color: color, width: active ? 2 : 1),
                    borderRadius: BorderRadius.circular(AppTheme.radiusMd),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.link_off, color: color),
                      const SizedBox(width: AppTheme.spacingSm),
                      Flexible(
                        child: Text(
                          'Drop to detach from chain (make standalone)',
                          style: Theme.of(
                            context,
                          ).textTheme.bodyMedium?.copyWith(color: color),
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
          ),
        if (readyStandalone.isNotEmpty)
          standaloneSection('Ready & Active', readyStandalone),
        if (somedayStandalone.isNotEmpty)
          standaloneSection('Someday / Flexible', somedayStandalone),
        if (_filter == _ProjectsFilter.all ||
            _filter == _ProjectsFilter.completed) ...[
          const SizedBox(height: AppTheme.spacingLg),
          ExpansionTile(
            key: ValueKey('projects-completed-${_filter.name}'),
            initiallyExpanded: _filter == _ProjectsFilter.completed,
            tilePadding: EdgeInsets.zero,
            title: Text('Completed Tasks (${completedTasks.length})'),
            children: [for (final task in completedTasks) taskCard(task, null)],
          ),
        ],
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
    required this.visibleTaskIds,
    required this.buildCard,
    required this.onUnlink,
  });

  final DependencyChain chain;
  final Set<String> visibleTaskIds;
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
        if (widget.visibleTaskIds.contains(step.task.id) &&
            (!_hideCompleted || !step.task.isCompleted))
          step,
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
            isCritical: chain.criticalPathEdges.contains((
              prerequisiteId,
              dependent.id,
            )),
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
                              criticalColor: colorScheme.primary,
                              isCriticalNode: chain.criticalPathTaskIds
                                  .contains(visibleSteps[index].task.id),
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
    required this.isCritical,
  });

  final TaskItem prerequisite;
  final TaskItem dependent;
  final int fromIndex;
  final int toIndex;
  final double railX;
  final double branchOffset;
  final bool isCritical;
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
    required this.criticalColor,
    required this.isCriticalNode,
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
  final Color criticalColor;
  final bool isCriticalNode;
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
                  criticalColor: widget.criticalColor,
                  rowIndex: widget.index,
                  nodeX: widget.nodeX,
                  activeLinks: widget.activeLinks,
                  incomingLinks: widget.incomingLinks,
                  isCriticalNode: widget.isCriticalNode,
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
                                    : link.isCritical
                                    ? widget.criticalColor
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
    required this.criticalColor,
    required this.rowIndex,
    required this.nodeX,
    required this.activeLinks,
    required this.incomingLinks,
    required this.isCriticalNode,
    required this.hoveredPrerequisiteId,
  });

  final Color color;
  final Color hoverColor;
  final Color criticalColor;
  final int rowIndex;
  final double nodeX;
  final List<_RailLink> activeLinks;
  final List<_RailLink> incomingLinks;
  final bool isCriticalNode;
  final String? hoveredPrerequisiteId;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = 2
      ..style = PaintingStyle.stroke;
    final middle = size.height / 2;
    for (final isCritical in [false, true]) {
      for (final link in activeLinks) {
        if (link.isCritical != isCritical) continue;
        paint.color = isCritical ? criticalColor : color;
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
    }
    paint.color = isCriticalNode ? criticalColor : color;
    canvas.drawCircle(Offset(nodeX, middle), 3, paint);
    canvas.drawLine(Offset(nodeX, middle), Offset(size.width, middle), paint);
    for (final link in incomingLinks) {
      final branchY = middle + link.branchOffset;
      paint.color = hoveredPrerequisiteId == link.prerequisite.id
          ? hoverColor
          : link.isCritical
          ? criticalColor
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
      criticalColor != oldDelegate.criticalColor ||
      isCriticalNode != oldDelegate.isCriticalNode ||
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
    required this.isCriticalPath,
    required this.onTap,
    required this.onFollowUp,
    required this.onLinkExistingFollowUp,
    required this.onLinkPrerequisite,
    required this.onLinkFollowUp,
    required this.onDetach,
    required this.onArchive,
    required this.onToggleCompleted,
  });

  final TaskItem task;
  final DependencyGraph graph;
  final bool isGuest;
  final int? stepNumber;
  final bool isCriticalPath;
  final VoidCallback onTap;
  final VoidCallback onFollowUp;
  final VoidCallback onLinkExistingFollowUp;
  final VoidCallback onLinkPrerequisite;
  final VoidCallback onLinkFollowUp;
  final VoidCallback onDetach;
  final VoidCallback onArchive;
  final ValueChanged<bool> onToggleCompleted;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final readiness = graph.readinessFor(task);
    final waitingOn = graph.uncompletedPrerequisites[task.id]!;
    final waitingNames = waitingOn.map((item) => item.title).join(', ');
    final isBlocked = readiness == ProjectTaskReadiness.blocked;
    final isCompleted = readiness == ProjectTaskReadiness.completed;
    final isOneOff = task.recurrenceRule == null && !task.isRecurringOccurrence;
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
      shape: isCriticalPath
          ? RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(AppTheme.radiusMd),
              side: BorderSide(color: colorScheme.primary, width: 1.5),
            )
          : null,
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
                  onChanged: (isBlocked && !isCompleted) || !isOneOff
                      ? null
                      : (value) {
                          if (value != null) onToggleCompleted(value);
                        },
                ),
              ),
              const SizedBox(width: AppTheme.spacingXs),
              if (task.recurrenceRule == null &&
                  !task.isRecurringOccurrence) ...[
                Tooltip(
                  message: 'Drag ${task.title} to link or detach',
                  child: Icon(
                    Icons.drag_indicator,
                    size: 20,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(width: AppTheme.spacingXs),
              ],
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
                    if (isCriticalPath)
                      Padding(
                        padding: const EdgeInsets.only(top: AppTheme.spacingXs),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(
                              Icons.route_outlined,
                              size: 16,
                              color: colorScheme.primary,
                            ),
                            const SizedBox(width: AppTheme.spacingXs),
                            Text(
                              'Critical Path',
                              style: Theme.of(context).textTheme.labelSmall
                                  ?.copyWith(color: colorScheme.primary),
                            ),
                          ],
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
                    if (isOneOff)
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
              PopupMenuButton<_ProjectCardAction>(
                key: ValueKey('project-actions-${task.id}-$stepNumber'),
                tooltip: 'Task actions',
                icon: const Icon(Icons.more_horiz),
                onSelected: (action) {
                  switch (action) {
                    case _ProjectCardAction.linkPrerequisite:
                      onLinkPrerequisite();
                    case _ProjectCardAction.linkFollowUp:
                      onLinkFollowUp();
                    case _ProjectCardAction.detach:
                      onDetach();
                    case _ProjectCardAction.toggleCompleted:
                      onToggleCompleted(!isCompleted);
                    case _ProjectCardAction.archive:
                      onArchive();
                  }
                },
                itemBuilder: (context) => [
                  PopupMenuItem(
                    value: _ProjectCardAction.linkPrerequisite,
                    enabled: isOneOff,
                    child: const Text('Link Prerequisite...'),
                  ),
                  PopupMenuItem(
                    value: _ProjectCardAction.linkFollowUp,
                    enabled: isOneOff,
                    child: const Text('Link Follow-Up...'),
                  ),
                  if (stepNumber != null || task.dependencies.isNotEmpty)
                    PopupMenuItem(
                      value: _ProjectCardAction.detach,
                      enabled: isOneOff && task.dependencies.isNotEmpty,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Detach from Chain'),
                          if (task.dependencies.isEmpty)
                            Text(
                              'No incoming links to remove',
                              style: Theme.of(context).textTheme.labelSmall,
                            ),
                        ],
                      ),
                    ),
                  PopupMenuItem(
                    value: _ProjectCardAction.toggleCompleted,
                    enabled: isOneOff && (!isBlocked || isCompleted),
                    child: Text(isCompleted ? 'Reopen' : 'Mark Complete'),
                  ),
                  const PopupMenuItem(
                    value: _ProjectCardAction.archive,
                    child: Text('Archive Task'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

enum _FollowUpAction { create, link }

enum _ProjectCardAction {
  linkPrerequisite,
  linkFollowUp,
  detach,
  toggleCompleted,
  archive,
}
