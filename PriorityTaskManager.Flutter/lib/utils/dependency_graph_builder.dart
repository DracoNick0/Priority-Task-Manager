import 'dart:collection';

import '../models/task_item.dart';

/// Dependency readiness is based on the tasks in the active list, not on
/// whether the scheduler has found a time slot for them.
enum ProjectTaskReadiness { ready, blocked, completed }

/// One task in a dependency component, at its depth from a prerequisite root.
class DependencyStep {
  const DependencyStep({required this.task, required this.depth});

  final TaskItem task;
  final int depth;
}

/// Tasks connected by prerequisite links, ordered from roots to dependents.
class DependencyChain {
  const DependencyChain({
    required this.roots,
    required this.steps,
    required this.hasCycle,
  });

  final List<TaskItem> roots;
  final List<DependencyStep> steps;
  final bool hasCycle;
}

/// A list-scoped snapshot of chains, independent backlog, and readiness.
class DependencyGraph {
  const DependencyGraph({
    required this.chains,
    required this.readyStandalone,
    required this.somedayStandalone,
    required this.completedTasks,
    required this.uncompletedPrerequisites,
    required this.totalCount,
    required this.readyCount,
    required this.blockedCount,
  });

  final List<DependencyChain> chains;
  final List<TaskItem> readyStandalone;
  final List<TaskItem> somedayStandalone;
  final List<TaskItem> completedTasks;
  final Map<String, List<TaskItem>> uncompletedPrerequisites;
  final int totalCount;
  final int readyCount;
  final int blockedCount;

  ProjectTaskReadiness readinessFor(TaskItem task) {
    if (task.isCompleted) return ProjectTaskReadiness.completed;
    return (uncompletedPrerequisites[task.id]?.isNotEmpty ?? false)
        ? ProjectTaskReadiness.blocked
        : ProjectTaskReadiness.ready;
  }
}

/// Builds a presentation graph without changing tasks or scheduling decisions.
class DependencyGraphBuilder {
  const DependencyGraphBuilder._();

  /// Whether making [proposedDependencyId] a prerequisite of [taskId] loops
  /// back to the dependent through the active list's existing links.
  static bool wouldCreateCycle(
    List<TaskItem> tasks, {
    required String taskId,
    required String proposedDependencyId,
  }) {
    final byId = {for (final task in tasks) task.id: task};
    if (!byId.containsKey(taskId)) {
      throw ArgumentError.value(taskId, 'taskId', 'Task is not in this list');
    }
    if (!byId.containsKey(proposedDependencyId)) {
      throw ArgumentError.value(
        proposedDependencyId,
        'proposedDependencyId',
        'Prerequisite is not in this list',
      );
    }

    final pending = Queue<String>()..add(proposedDependencyId);
    final visited = <String>{};
    while (pending.isNotEmpty) {
      final id = pending.removeFirst();
      if (id == taskId) return true;
      if (!visited.add(id)) continue;
      for (final dependencyId in byId[id]!.dependencies) {
        if (byId.containsKey(dependencyId)) pending.add(dependencyId);
      }
    }
    return false;
  }

  static DependencyGraph build(List<TaskItem> tasks) {
    final byId = {for (final task in tasks) task.id: task};
    final adjacent = {for (final task in tasks) task.id: <String>{}};
    final dependents = {for (final task in tasks) task.id: <String>{}};
    final uncompleted = <String, List<TaskItem>>{};

    for (final task in tasks) {
      final waitingOn = <TaskItem>[];
      for (final prerequisiteId in task.dependencies.toSet()) {
        final prerequisite = byId[prerequisiteId];
        if (prerequisite == null) continue;
        adjacent[task.id]!.add(prerequisiteId);
        adjacent[prerequisiteId]!.add(task.id);
        dependents[prerequisiteId]!.add(task.id);
        if (!prerequisite.isCompleted) waitingOn.add(prerequisite);
      }
      uncompleted[task.id] = waitingOn;
    }

    final chains = <DependencyChain>[];
    final readyStandalone = <TaskItem>[];
    final somedayStandalone = <TaskItem>[];
    final completedTasks = <TaskItem>[];
    final visited = <String>{};

    for (final task in tasks) {
      if (task.isCompleted) completedTasks.add(task);
      if (!visited.add(task.id)) continue;

      final componentIds = <String>[];
      final pending = Queue<String>()..add(task.id);
      while (pending.isNotEmpty) {
        final id = pending.removeFirst();
        componentIds.add(id);
        for (final neighbor in adjacent[id]!) {
          if (visited.add(neighbor)) pending.add(neighbor);
        }
      }

      if (componentIds.length == 1 && adjacent[task.id]!.isEmpty) {
        if (!task.isCompleted) {
          if (task.dueDate == null || task.importance <= 3) {
            somedayStandalone.add(task);
          } else {
            readyStandalone.add(task);
          }
        }
      } else {
        chains.add(_buildChain(componentIds, byId, dependents));
      }
    }

    final blockedCount = tasks
        .where((task) => !task.isCompleted && uncompleted[task.id]!.isNotEmpty)
        .length;
    return DependencyGraph(
      chains: chains,
      readyStandalone: readyStandalone,
      somedayStandalone: somedayStandalone,
      completedTasks: completedTasks,
      uncompletedPrerequisites: uncompleted,
      totalCount: tasks.length,
      readyCount: tasks.length - completedTasks.length - blockedCount,
      blockedCount: blockedCount,
    );
  }

  static DependencyChain _buildChain(
    List<String> componentIds,
    Map<String, TaskItem> byId,
    Map<String, Set<String>> dependents,
  ) {
    final component = componentIds.toSet();
    final indegree = {
      for (final id in componentIds)
        id: byId[id]!.dependencies.toSet().where(component.contains).length,
    };
    final roots = [
      for (final id in componentIds)
        if (indegree[id] == 0) byId[id]!,
    ];
    final ready = Queue<String>()..addAll(roots.map((task) => task.id));
    final remaining = componentIds.toSet();
    final depths = <String, int>{};
    final steps = <DependencyStep>[];
    var hasCycle = false;

    while (remaining.isNotEmpty) {
      if (ready.isEmpty) {
        // A cycle has no root; break one edge for display, not for scheduling.
        hasCycle = true;
        ready.add(componentIds.firstWhere(remaining.contains));
      }
      final id = ready.removeFirst();
      if (!remaining.remove(id)) continue;
      final depth = depths[id] ?? 0;
      steps.add(DependencyStep(task: byId[id]!, depth: depth));
      for (final dependentId in dependents[id]!) {
        if (!remaining.contains(dependentId)) continue;
        final nextDepth = depth + 1;
        if (nextDepth > (depths[dependentId] ?? 0)) {
          depths[dependentId] = nextDepth;
        }
        final count = indegree[dependentId]! - 1;
        indegree[dependentId] = count;
        if (count == 0) ready.add(dependentId);
      }
    }

    return DependencyChain(roots: roots, steps: steps, hasCycle: hasCycle);
  }
}
