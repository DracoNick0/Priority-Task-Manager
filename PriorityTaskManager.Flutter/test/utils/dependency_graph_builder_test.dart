import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/utils/dependency_graph_builder.dart';

TaskItem task(
  String id, {
  List<String> dependencies = const [],
  bool completed = false,
  DateTime? dueDate,
  int importance = 5,
}) => TaskItem(
  id: id,
  listId: 'list-1',
  title: id,
  dependencies: dependencies,
  isCompleted: completed,
  dueDate: dueDate,
  importance: importance,
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
}
