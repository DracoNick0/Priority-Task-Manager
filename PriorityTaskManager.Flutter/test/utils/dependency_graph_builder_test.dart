import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/utils/dependency_graph_builder.dart';

TaskItem task(
  String id, {
  List<String> dependencies = const [],
  bool completed = false,
  DateTime? dueDate,
  int importance = 5,
  int estimatedMinutes = 60,
}) => TaskItem(
  id: id,
  listId: 'list-1',
  title: id,
  dependencies: dependencies,
  isCompleted: completed,
  dueDate: dueDate,
  importance: importance,
  estimatedDurationMinutes: estimatedMinutes,
);

void main() {
  test('orders a linear chain from prerequisite to final dependent', () {
    final graph = DependencyGraphBuilder.build([
      task('C', dependencies: ['B']),
      task('A'),
      task('B', dependencies: ['A']),
    ]);

    expect(graph.chains, hasLength(1));
    expect(graph.chains.single.roots.map((t) => t.id), ['A']);
    expect(graph.chains.single.steps.map((step) => step.task.id), [
      'A',
      'B',
      'C',
    ]);
    expect(graph.chains.single.steps.map((step) => step.depth), [0, 1, 2]);
    expect(graph.chains.single.hasCycle, isFalse);
    expect(graph.readyCount, 1);
    expect(graph.blockedCount, 2);
  });

  test('keeps branching dependents together at the same depth', () {
    final graph = DependencyGraphBuilder.build([
      task('A'),
      task('B', dependencies: ['A']),
      task('C', dependencies: ['A']),
    ]);

    expect(graph.chains, hasLength(1));
    expect(graph.chains.single.steps.map((step) => step.task.id), [
      'A',
      'B',
      'C',
    ]);
    expect(graph.chains.single.steps.map((step) => step.depth), [0, 1, 1]);
  });

  test('tolerates cycles and includes downstream tasks once', () {
    final graph = DependencyGraphBuilder.build([
      task('A', dependencies: ['B']),
      task('B', dependencies: ['A']),
      task('C', dependencies: ['B']),
    ]);

    expect(graph.chains.single.hasCycle, isTrue);
    expect(graph.chains.single.roots, isEmpty);
    expect(graph.chains.single.steps.map((step) => step.task.id), [
      'A',
      'B',
      'C',
    ]);
    expect(graph.blockedCount, 3);
  });

  test('groups multiple prerequisites with their dependent', () {
    final graph = DependencyGraphBuilder.build([
      task('A'),
      task('B'),
      task('C', dependencies: ['A', 'B']),
    ]);

    expect(graph.chains, hasLength(1));
    expect(graph.chains.single.roots.map((t) => t.id), ['A', 'B']);
    expect(graph.chains.single.steps.last.depth, 1);
    expect(graph.uncompletedPrerequisites['C']!.map((t) => t.id), ['A', 'B']);
  });

  test('reclassifies dependents when a prerequisite completes', () {
    final a = task('A', completed: true);
    final b = task('B', dependencies: ['A']);
    final c = task('C', dependencies: ['B']);
    final active = task('Active', dueDate: DateTime(2026, 11, 30));
    final flexible = task('Flexible');
    final lowPriority = task(
      'Low priority',
      dueDate: DateTime(2026, 11, 30),
      importance: 2,
    );
    final graph = DependencyGraphBuilder.build([
      a,
      b,
      c,
      active,
      flexible,
      lowPriority,
    ]);

    expect(graph.readinessFor(a), ProjectTaskReadiness.completed);
    expect(graph.readinessFor(b), ProjectTaskReadiness.ready);
    expect(graph.readinessFor(c), ProjectTaskReadiness.blocked);
    expect(graph.uncompletedPrerequisites['C']!.single.title, 'B');
    expect(graph.readyStandalone.map((t) => t.id), ['Active']);
    expect(graph.somedayStandalone.map((t) => t.id), [
      'Flexible',
      'Low priority',
    ]);
    expect(graph.completedTasks.map((t) => t.id), ['A']);
    expect(graph.totalCount, 6);
    expect(graph.readyCount, 4);
    expect(graph.blockedCount, 1);

    final updated = DependencyGraphBuilder.build([
      a,
      b.copyWith(isCompleted: true),
      c,
    ]);
    expect(updated.readinessFor(c), ProjectTaskReadiness.ready);
    expect(updated.uncompletedPrerequisites['C'], isEmpty);
  });

  test('does not make a phantom chain for a prerequisite outside the list', () {
    final graph = DependencyGraphBuilder.build([
      task('Only task', dependencies: ['outside-list']),
    ]);

    expect(graph.chains, isEmpty);
    expect(graph.readinessFor(task('Only task')), ProjectTaskReadiness.ready);
  });

  test('rejects self-links and transitive cycles but allows new DAG edges', () {
    final tasks = [
      task('A'),
      task('B', dependencies: ['A']),
      task('C', dependencies: ['B']),
      task('Independent'),
    ];

    bool cycles(String dependent, String prerequisite) =>
        DependencyGraphBuilder.wouldCreateCycle(
          tasks,
          taskId: dependent,
          proposedDependencyId: prerequisite,
        );

    expect(cycles('A', 'A'), isTrue);
    expect(cycles('A', 'C'), isTrue);
    expect(cycles('B', 'C'), isTrue);
    expect(cycles('C', 'A'), isFalse);
    expect(cycles('B', 'Independent'), isFalse);
    expect(() => cycles('B', 'outside-list'), throwsArgumentError);
  });

  test('summarizes each chain task once and excludes completed effort', () {
    final graph = DependencyGraphBuilder.build([
      task('A', completed: true, estimatedMinutes: 45),
      task('B', dependencies: ['A'], estimatedMinutes: 70),
      task('C', dependencies: ['A'], estimatedMinutes: 35),
      task('D', dependencies: ['B', 'C'], estimatedMinutes: 20),
    ]);

    final chain = graph.chains.single;
    expect(chain.totalCount, 4);
    expect(chain.completedCount, 1);
    expect(chain.totalMinutes, 170);
    expect(chain.remainingMinutes, 125);
  });

  test(
    'critical path follows the longest remaining branch and shifts on completion',
    () {
      final a = task('A', estimatedMinutes: 60);
      final b = task('B', dependencies: ['A'], estimatedMinutes: 120);
      final c = task('C', dependencies: ['A'], estimatedMinutes: 40);
      final d = task('D', dependencies: ['B'], estimatedMinutes: 50);
      final e = task('E', dependencies: ['C'], estimatedMinutes: 50);
      final initial = DependencyGraphBuilder.build([
        a,
        b,
        c,
        d,
        e,
      ]).chains.single;

      expect(initial.criticalPathTaskIds, {'A', 'B', 'D'});
      expect(initial.criticalPathEdges, {('A', 'B'), ('B', 'D')});

      final updated = DependencyGraphBuilder.build([
        a,
        b.copyWith(isCompleted: true),
        c,
        d,
        e,
      ]).chains.single;
      expect(updated.criticalPathTaskIds, {'A', 'C', 'E'});
      expect(updated.criticalPathEdges, {('A', 'C'), ('C', 'E')});
    },
  );

  test('critical edges exclude shortcuts and choose the heavier root', () {
    final diamond = DependencyGraphBuilder.build([
      task('A', estimatedMinutes: 10),
      task('B', dependencies: ['A'], estimatedMinutes: 20),
      task('C', dependencies: ['A'], estimatedMinutes: 40),
      task('D', dependencies: ['A', 'B', 'C'], estimatedMinutes: 5),
    ]).chains.single;
    expect(diamond.criticalPathTaskIds, {'A', 'C', 'D'});
    expect(diamond.criticalPathEdges, {('A', 'C'), ('C', 'D')});
    expect(diamond.criticalPathEdges, isNot(contains(('A', 'D'))));

    final multipleRoots = DependencyGraphBuilder.build([
      task('A', estimatedMinutes: 10),
      task('X', estimatedMinutes: 70),
      task('D', dependencies: ['A', 'X'], estimatedMinutes: 5),
    ]).chains.single;
    expect(multipleRoots.criticalPathTaskIds, {'X', 'D'});
    expect(multipleRoots.criticalPathEdges, {('X', 'D')});
  });

  test('completed-only and cyclic chains have no remaining critical path', () {
    final done = DependencyGraphBuilder.build([
      task('A', completed: true),
      task('B', dependencies: ['A'], completed: true),
    ]).chains.single;
    expect(done.criticalPathTaskIds, isEmpty);
    expect(done.criticalPathEdges, isEmpty);

    final cyclic = DependencyGraphBuilder.build([
      task('A', dependencies: ['B']),
      task('B', dependencies: ['A']),
    ]).chains.single;
    expect(cyclic.criticalPathTaskIds, isEmpty);
  });
}
