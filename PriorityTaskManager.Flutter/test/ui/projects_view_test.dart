import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/models/user_profile.dart';
import 'package:priority_task_manager/providers/app_notifications_provider.dart';
import 'package:priority_task_manager/providers/selection_provider.dart';
import 'package:priority_task_manager/providers/session_provider.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/user_profile_provider.dart';
import 'package:priority_task_manager/ui/command_center/projects_view.dart';
import 'package:priority_task_manager/ui/command_center/right_inspector.dart';

class _MemoryTaskRepository implements TaskRepository {
  _MemoryTaskRepository(this.tasks);

  final List<TaskItem> tasks;

  @override
  Future<TaskItem> addTask({
    required String listId,
    required String title,
    String description = '',
    DateTime? dueDate,
    int estimatedDurationMinutes = 60,
    List<String>? dependencies,
    int importance = 5,
    int complexity = 1,
    DateTime? notBefore,
    bool isPinned = false,
    bool isDivisible = false,
    String link = '',
    RecurrenceRule? recurrenceRule,
    String progressionMode = 'RollForwardKeepBacklog',
    int requiredCompletions = 1,
    bool showMissedIndicator = true,
    bool trackStreak = false,
  }) async {
    final created = TaskItem(
      id: 'created',
      listId: listId,
      title: title,
      dependencies: dependencies,
    );
    tasks.add(created);
    return created;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _MemoryTasksNotifier extends TasksNotifier {
  _MemoryTasksNotifier(this.items);

  final List<TaskItem> items;

  @override
  Future<List<TaskItem>> build(String arg) async => List.of(items);

  @override
  Future<void> setCompleted(String taskId, bool completed) async {
    final index = items.indexWhere((task) => task.id == taskId);
    items[index] = items[index].copyWith(isCompleted: completed);
    ref.invalidateSelf();
    await future;
  }

  @override
  Future<void> updateTask(TaskItem task) async {
    final index = items.indexWhere((item) => item.id == task.id);
    items[index] = task;
    ref.invalidateSelf();
    await future;
  }
}

class _Session extends SessionController {
  _Session(this.guest);

  final bool guest;

  @override
  Future<SessionState> build() async => guest
      ? SessionState.guest
      : const SessionState(status: SessionStatus.authenticated);
}

class _Lists extends TaskListsNotifier {
  @override
  Future<List<TaskList>> build() async => [
    TaskList(id: 'list-1', name: 'Work'),
  ];
}

class _Profile extends UserProfileNotifier {
  @override
  Future<UserProfile> build() async => UserProfile();
}

TaskItem _task(
  String id, {
  List<String> dependencies = const [],
  bool completed = false,
  DateTime? dueDate,
  RecurrenceRule? recurrenceRule,
  int estimatedMinutes = 60,
}) => TaskItem(
  id: id,
  listId: 'list-1',
  title: id,
  dependencies: dependencies,
  isCompleted: completed,
  dueDate: dueDate,
  recurrenceRule: recurrenceRule,
  estimatedDurationMinutes: estimatedMinutes,
);

Future<ProviderContainer> _pumpProjects(
  WidgetTester tester,
  List<TaskItem> tasks, {
  bool guest = false,
  bool withInspector = false,
}) async {
  tester.view.physicalSize = const Size(1200, 1600);
  tester.view.devicePixelRatio = 1;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });
  final container = ProviderContainer(
    overrides: [
      tasksProvider.overrideWith(() => _MemoryTasksNotifier(tasks)),
      taskRepositoryProvider.overrideWith(
        (ref) async => _MemoryTaskRepository(tasks),
      ),
      sessionControllerProvider.overrideWith(() => _Session(guest)),
      activeListIdProvider.overrideWith((ref) => 'list-1'),
      taskListsProvider.overrideWith(_Lists.new),
      userProfileProvider.overrideWith(_Profile.new),
    ],
  );
  addTearDown(container.dispose);
  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: container,
      child: MaterialApp(
        home: Scaffold(
          body: withInspector
              ? Row(
                  children: [
                    const Expanded(child: ProjectsView(listId: 'list-1')),
                    const SizedBox(width: 350, child: RightInspector()),
                  ],
                )
              : const ProjectsView(listId: 'list-1'),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return container;
}

void main() {
  testWidgets('shows branching steps, status, backlog, and completed section', (
    tester,
  ) async {
    final container = await _pumpProjects(tester, [
      _task('A'),
      _task('B', dependencies: ['A']),
      _task('C', dependencies: ['A']),
      _task('Active', dueDate: DateTime(2026, 12, 1)),
      _task('Flexible'),
      _task('Done', completed: true),
    ]);

    expect(find.text('Total: 6'), findsOneWidget);
    expect(find.text('Ready: 3'), findsOneWidget);
    expect(find.text('Blocked: 2'), findsOneWidget);
    expect(find.text('Completed: 1'), findsOneWidget);
    expect(find.text('Dependency Step-Chains (1)'), findsOneWidget);
    expect(find.text('Step 1'), findsOneWidget);
    expect(find.text('Step 2'), findsNWidgets(2));
    expect(find.text('Waiting on: A'), findsNWidgets(2));
    expect(find.text('Ready & Active (1)'), findsOneWidget);
    expect(find.text('Someday / Flexible (1)'), findsOneWidget);
    expect(find.text('Completed Tasks (1)'), findsOneWidget);
    expect(find.byKey(const ValueKey('project-task-Done-null')), findsNothing);
    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-B-2')),
        matching: find.text('B'),
      ),
    );
    expect(container.read(selectedInspectorProvider).id, 'B');
    await tester.tap(find.text('Completed Tasks (1)'));
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey('project-task-Done-null')),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('completing a prerequisite unblocks its downstream task', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
    ];
    await _pumpProjects(tester, tasks);

    expect(find.text('Blocked: 1'), findsOneWidget);
    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-A-1')),
        matching: find.byType(Checkbox),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Blocked: 0'), findsOneWidget);
    expect(find.text('Ready: 1'), findsOneWidget);
    expect(find.text('Waiting on: A'), findsNothing);
    expect(tasks.first.isCompleted, isTrue);
    expect(tester.takeException(), isNull);
  });

  testWidgets('continuous rails branch to each dependent and unlink one edge', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B'),
      _task('C', dependencies: ['A', 'B']),
      _task('D', dependencies: ['C']),
    ];
    final container = await _pumpProjects(tester, tasks);

    final aRail = find.byKey(const ValueKey('project-rail-A'));
    final bRail = find.byKey(const ValueKey('project-rail-B'));
    final cRail = find.byKey(const ValueKey('project-rail-C'));
    final dRail = find.byKey(const ValueKey('project-rail-D'));
    expect(tester.getBottomLeft(aRail).dy, tester.getTopLeft(bRail).dy);
    expect(tester.getBottomLeft(bRail).dy, tester.getTopLeft(cRail).dy);
    expect(tester.getBottomLeft(cRail).dy, tester.getTopLeft(dRail).dy);
    expect(
      tester.getSize(cRail).width,
      greaterThan(tester.getSize(aRail).width),
    );

    final unlinkA = find.byKey(const ValueKey('project-unlink-C-A'));
    final unlinkB = find.byKey(const ValueKey('project-unlink-C-B'));
    expect(unlinkA, findsOneWidget);
    expect(unlinkB, findsOneWidget);
    expect(
      tester.getTopRight(unlinkA).dx,
      lessThanOrEqualTo(
        tester.getTopLeft(find.byKey(const ValueKey('project-task-C-2'))).dx,
      ),
    );
    expect(
      find.byTooltip('Remove dependency: C will no longer wait on A'),
      findsOneWidget,
    );
    final mouse = await tester.createGesture(kind: PointerDeviceKind.mouse);
    await mouse.addPointer(
      location: tester.getCenter(unlinkA) - const Offset(26, 0),
    );
    await tester.pump();
    expect(
      tester.widget<IconButton>(unlinkA).color,
      Theme.of(tester.element(unlinkA)).colorScheme.error,
    );
    await mouse.removePointer();

    await tester.tap(unlinkA);
    await tester.pumpAndSettle();
    expect(tasks[2].dependencies, ['B']);
    expect(find.byKey(const ValueKey('project-unlink-C-A')), findsNothing);
    expect(find.byKey(const ValueKey('project-unlink-C-B')), findsOneWidget);

    final notification = container.read(appNotificationProvider)!;
    expect(notification.actionLabel, 'Undo');
    await notification.onAction!();
    await tester.pumpAndSettle();
    expect(tasks[2].dependencies, ['A', 'B']);
    expect(find.byKey(const ValueKey('project-unlink-C-A')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('undo refuses to restore an edge that became cyclic', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
    ];
    final container = await _pumpProjects(tester, tasks);

    await tester.tap(find.byKey(const ValueKey('project-unlink-B-A')));
    await tester.pumpAndSettle();
    final undo = container.read(appNotificationProvider)!.onAction!;
    await container
        .read(tasksProvider('list-1').notifier)
        .updateTask(tasks.first.copyWith(dependencies: ['B']));
    await tester.pumpAndSettle();

    await undo();
    await tester.pumpAndSettle();
    expect(tasks.last.dependencies, isEmpty);
    expect(
      container.read(appNotificationProvider)!.message,
      contains('Could not restore'),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('chain progress, effort, collapse, and hide-completed controls', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A', completed: true, estimatedMinutes: 50),
      _task('B', dependencies: ['A'], estimatedMinutes: 80),
      _task('C', dependencies: ['B'], estimatedMinutes: 30),
    ]);

    expect(find.text('1 of 3 completed (33%)'), findsOneWidget);
    expect(find.text('Total: 2h 40m • Remaining: 1h 50m'), findsOneWidget);
    expect(
      tester
          .widget<LinearProgressIndicator>(find.byType(LinearProgressIndicator))
          .value,
      closeTo(1 / 3, 0.001),
    );
    final originalWidth = tester
        .getSize(find.byKey(const ValueKey('project-rail-B')))
        .width;

    final hideCompleted = find.byKey(
      const ValueKey('project-hide-completed-A'),
    );
    await tester.tap(hideCompleted);
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('project-task-A-1')), findsNothing);
    expect(find.byKey(const ValueKey('project-task-B-2')), findsOneWidget);
    expect(find.byKey(const ValueKey('project-task-C-3')), findsOneWidget);
    expect(
      tester.getSize(find.byKey(const ValueKey('project-rail-B'))).width,
      lessThan(originalWidth),
    );
    expect(tester.widget<FilterChip>(hideCompleted).selected, isTrue);

    await tester.tap(find.byKey(const ValueKey('project-chain-toggle-A')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('project-task-B-2')), findsNothing);
    expect(find.text('1 of 3 completed (33%)'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsNothing);

    await tester.tap(find.byKey(const ValueKey('project-chain-toggle-A')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('project-task-A-1')), findsNothing);
    expect(find.byKey(const ValueKey('project-task-C-3')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('hide-completed is scoped to one chain', (tester) async {
    await _pumpProjects(tester, [
      _task('A', completed: true),
      _task('B', dependencies: ['A']),
      _task('X', completed: true),
      _task('Y', dependencies: ['X']),
    ]);

    await tester.tap(find.byKey(const ValueKey('project-hide-completed-A')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('project-task-A-1')), findsNothing);
    expect(find.byKey(const ValueKey('project-task-X-1')), findsOneWidget);
    expect(find.byKey(const ValueKey('project-task-Y-2')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('a fully completed chain retains its summary when hidden', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A', completed: true),
      _task('B', dependencies: ['A'], completed: true),
    ]);

    expect(find.text('2 of 2 completed (100%)'), findsOneWidget);
    expect(find.text('Total: 2h 0m • Remaining: 0h 0m'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('project-hide-completed-A')));
    await tester.pumpAndSettle();
    expect(find.text('All steps completed.'), findsOneWidget);
    expect(find.byKey(const ValueKey('project-task-A-1')), findsNothing);
    expect(find.byKey(const ValueKey('project-task-B-2')), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('highlights the longest remaining path as tasks change', (
    tester,
  ) async {
    final tasks = [
      _task('A', estimatedMinutes: 60),
      _task('B', dependencies: ['A'], estimatedMinutes: 120),
      _task('C', dependencies: ['A'], estimatedMinutes: 40),
      _task('D', dependencies: ['B'], estimatedMinutes: 50),
      _task('E', dependencies: ['C'], estimatedMinutes: 50),
    ];
    final container = await _pumpProjects(tester, tasks);

    Finder badgeFor(String id, int step) => find.descendant(
      of: find.byKey(ValueKey('project-task-$id-$step')),
      matching: find.text('Critical Path'),
    );

    expect(badgeFor('A', 1), findsOneWidget);
    expect(badgeFor('B', 2), findsOneWidget);
    expect(badgeFor('D', 3), findsOneWidget);
    expect(badgeFor('C', 2), findsNothing);
    expect(badgeFor('E', 3), findsNothing);
    final criticalCard = tester.widget<Card>(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-B-2')),
        matching: find.byType(Card),
      ),
    );
    expect(
      (criticalCard.shape! as RoundedRectangleBorder).side.color,
      Theme.of(tester.element(badgeFor('B', 2))).colorScheme.primary,
    );

    await container
        .read(tasksProvider('list-1').notifier)
        .updateTask(tasks[1].copyWith(isCompleted: true));
    await tester.pumpAndSettle();
    expect(badgeFor('B', 2), findsNothing);
    expect(badgeFor('D', 3), findsNothing);
    expect(badgeFor('A', 1), findsOneWidget);
    expect(badgeFor('C', 2), findsOneWidget);
    expect(badgeFor('E', 3), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('search and status filters show matching backlog tasks', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('Root', dueDate: DateTime(2026, 12, 1)),
      _task('BlockedLeaf', dependencies: ['Root']),
      _task('Active', dueDate: DateTime(2026, 12, 1)),
      _task('Flexible'),
      _task('Done', completed: true),
      _task('DoneLinked', dependencies: ['Root'], completed: true),
    ]);

    await tester.enterText(
      find.byKey(const ValueKey('projects-search')),
      'bLoCkEd',
    );
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey('project-task-BlockedLeaf-2')),
      findsOneWidget,
    );
    expect(find.byKey(const ValueKey('project-task-Root-1')), findsNothing);
    expect(
      find.byKey(const ValueKey('project-task-Active-null')),
      findsNothing,
    );
    expect(find.text('Total: 6'), findsOneWidget);

    await tester.tap(find.byTooltip('Clear search'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('projects-filter-ready')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('project-task-Root-1')), findsOneWidget);
    expect(
      find.byKey(const ValueKey('project-task-Active-null')),
      findsOneWidget,
    );
    expect(
      find.byKey(const ValueKey('project-task-Flexible-null')),
      findsNothing,
    );
    expect(
      find.byKey(const ValueKey('project-task-BlockedLeaf-2')),
      findsNothing,
    );

    await tester.tap(find.byKey(const ValueKey('projects-filter-blocked')));
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey('project-task-BlockedLeaf-2')),
      findsOneWidget,
    );
    expect(find.byKey(const ValueKey('project-task-Root-1')), findsNothing);

    await tester.tap(find.byKey(const ValueKey('projects-filter-someday')));
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey('project-task-Flexible-null')),
      findsOneWidget,
    );
    expect(find.text('Dependency Step-Chains (1)'), findsNothing);

    await tester.tap(find.byKey(const ValueKey('projects-filter-completed')));
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey('project-task-Done-null')),
      findsOneWidget,
    );
    expect(
      find.byKey(const ValueKey('project-task-DoneLinked-null')),
      findsOneWidget,
    );
    expect(
      find.byKey(const ValueKey('project-task-DoneLinked-2')),
      findsNothing,
    );

    await tester.tap(find.byKey(const ValueKey('projects-filter-all')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('project-task-Root-1')), findsOneWidget);
    expect(
      find.byKey(const ValueKey('project-task-BlockedLeaf-2')),
      findsOneWidget,
    );
    expect(find.text('Total: 6'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('creating a follow-up saves the selected prerequisite', (
    tester,
  ) async {
    final tasks = [_task('A', dueDate: DateTime(2026, 12, 1))];
    final container = await _pumpProjects(tester, tasks, withInspector: true);

    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-A-null')),
        matching: find.text('Add Follow-Up'),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Create New Follow-Up'));
    await tester.pumpAndSettle();

    expect(container.read(selectedInspectorProvider).isCreating, isTrue);
    expect(container.read(selectedInspectorProvider).prerequisiteTaskId, 'A');
    expect(find.text('Follow-up to A'), findsOneWidget);
    expect(find.byKey(const ValueKey('prerequisite-chip-A')), findsOneWidget);
    await tester.enterText(
      find.byWidgetPredicate(
        (widget) =>
            widget is TextField && widget.decoration?.labelText == 'Title',
      ),
      'Next step',
    );
    await tester.ensureVisible(find.text('Create'));
    await tester.tap(find.text('Create'));
    await tester.pumpAndSettle();
    expect(tasks.last.title, 'Next step');
    expect(tasks.last.dependencies, ['A']);
    expect(find.text('Waiting on: A'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('blocked tasks keep their follow-up action accessible', (
    tester,
  ) async {
    final container = await _pumpProjects(tester, [
      _task('A'),
      _task('B', dependencies: ['A']),
    ]);

    final semantics = tester.ensureSemantics();
    final followUp = find.descendant(
      of: find.byKey(const ValueKey('project-task-B-2')),
      matching: find.text('Add Follow-Up'),
    );
    try {
      expect(
        tester
            .getSemantics(find.byKey(const ValueKey('project-task-B-2')))
            .flagsCollection
            .isEnabled
            .toBoolOrNull(),
        isNot(false),
      );
      await tester.tap(followUp);
      await tester.pumpAndSettle();
      expect(find.text('Link Existing Task...'), findsOneWidget);
      await tester.tap(find.text('Create New Follow-Up'));
      expect(container.read(selectedInspectorProvider).prerequisiteTaskId, 'B');
      expect(tester.takeException(), isNull);
    } finally {
      semantics.dispose();
    }
  });

  testWidgets(
    'links an existing task while disabling cyclic and recurring choices',
    (tester) async {
      final tasks = [
        _task('A'),
        _task('B', dependencies: ['A']),
        _task('C', dependencies: ['B']),
        _task('Independent'),
        _task(
          'Recurring',
          recurrenceRule: DailyIntervalRecurrenceRule(
            seriesStartDate: DateTime(2026, 11, 1),
            endCondition: NeverEndCondition(),
          ),
        ),
      ];
      await _pumpProjects(tester, tasks);

      await tester.tap(
        find.descendant(
          of: find.byKey(const ValueKey('project-task-C-3')),
          matching: find.text('Add Follow-Up'),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('Link Existing Task...'));
      await tester.pumpAndSettle();

      final cyclic = find.byKey(const ValueKey('link-candidate-A'));
      expect(tester.widget<ListTile>(cyclic).enabled, isFalse);
      expect(find.text('Would create a circular dependency'), findsNWidgets(2));
      expect(
        find.byKey(const ValueKey('link-candidate-Recurring')),
        findsNothing,
      );
      expect(find.byKey(const ValueKey('link-candidate-C')), findsNothing);
      await tester.tap(
        find.byKey(const ValueKey('link-candidate-Independent')),
      );
      await tester.pumpAndSettle();

      expect(tasks[3].dependencies, ['C']);
      expect(find.text('Waiting on: C'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('revalidates a picker choice when other links change', (
    tester,
  ) async {
    final tasks = [_task('A'), _task('B')];
    final container = await _pumpProjects(tester, tasks);

    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-A-null')),
        matching: find.text('Add Follow-Up'),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Link Existing Task...'));
    await tester.pumpAndSettle();
    expect(
      tester
          .widget<ListTile>(find.byKey(const ValueKey('link-candidate-B')))
          .enabled,
      isTrue,
    );

    await container
        .read(tasksProvider('list-1').notifier)
        .updateTask(tasks.first.copyWith(dependencies: ['B']));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('link-candidate-B')));
    await tester.pumpAndSettle();

    expect(tasks.last.dependencies, isEmpty);
    expect(container.read(appNotificationProvider)!.message, contains('cycle'));
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'inspector edits prerequisites and follow-ups without losing draft',
    (tester) async {
      final tasks = [
        _task('A'),
        _task('B', dependencies: ['A']),
        _task('C', dependencies: ['B']),
        _task('Independent'),
      ];
      await _pumpProjects(tester, tasks, withInspector: true);

      await tester.tap(
        find.descendant(
          of: find.byKey(const ValueKey('project-task-B-2')),
          matching: find.text('B'),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Prerequisites'), findsOneWidget);
      expect(find.text('Follow-ups'), findsOneWidget);
      expect(find.byKey(const ValueKey('prerequisite-chip-A')), findsOneWidget);
      expect(find.byKey(const ValueKey('follow-up-chip-C')), findsOneWidget);
      await tester.enterText(
        find.byWidgetPredicate(
          (widget) =>
              widget is TextField && widget.decoration?.labelText == 'Title',
        ),
        'Edited B',
      );

      await tester.tap(find.byKey(const ValueKey('link-prerequisite')));
      await tester.pumpAndSettle();
      expect(
        tester
            .widget<ListTile>(find.byKey(const ValueKey('link-candidate-C')))
            .enabled,
        isFalse,
      );
      await tester.tap(
        find.byKey(const ValueKey('link-candidate-Independent')),
      );
      await tester.pumpAndSettle();
      expect(tasks[1].dependencies, ['A', 'Independent']);
      expect(
        find.byKey(const ValueKey('prerequisite-chip-Independent')),
        findsOneWidget,
      );

      final followUpChip = find.byKey(const ValueKey('follow-up-chip-C'));
      await tester.ensureVisible(followUpChip);
      await tester.tapAt(
        tester.getCenter(followUpChip) +
            Offset(tester.getSize(followUpChip).width / 2 - 14, 0),
      );
      await tester.pumpAndSettle();
      expect(tasks[2].dependencies, isEmpty);
      expect(find.byKey(const ValueKey('follow-up-chip-C')), findsNothing);

      await tester.tap(find.byKey(const ValueKey('link-follow-up')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('link-candidate-C')));
      await tester.pumpAndSettle();
      expect(tasks[2].dependencies, ['B']);
      expect(find.byKey(const ValueKey('follow-up-chip-C')), findsOneWidget);

      final prerequisiteChip = find.byKey(
        const ValueKey('prerequisite-chip-A'),
      );
      await tester.ensureVisible(prerequisiteChip);
      await tester.tapAt(
        tester.getCenter(prerequisiteChip) +
            Offset(tester.getSize(prerequisiteChip).width / 2 - 14, 0),
      );
      await tester.pumpAndSettle();
      expect(tasks[1].dependencies, ['Independent']);

      await tester.ensureVisible(find.text('Save'));
      await tester.tap(find.text('Save'));
      await tester.pumpAndSettle();
      expect(tasks[1].title, 'Edited B');
      expect(tasks[1].dependencies, ['Independent']);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('inspector can remove a missing prerequisite reference', (
    tester,
  ) async {
    final tasks = [
      _task('Orphan', dependencies: ['removed-task']),
    ];
    await _pumpProjects(tester, tasks, withInspector: true);

    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-Orphan-null')),
        matching: find.text('Orphan'),
      ),
    );
    await tester.pumpAndSettle();
    final chip = find.byKey(const ValueKey('prerequisite-chip-removed-task'));
    expect(chip, findsOneWidget);
    await tester.ensureVisible(chip);
    await tester.tapAt(
      tester.getCenter(chip) + Offset(tester.getSize(chip).width / 2 - 14, 0),
    );
    await tester.pumpAndSettle();

    expect(tasks.single.dependencies, isEmpty);
    expect(chip, findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('Guest mode offers manual readiness without a schedule', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A', dueDate: DateTime(2026, 12, 1)),
    ], guest: true);

    expect(find.text('Ready for manual execution'), findsOneWidget);
    expect(find.textContaining('Guest mode:'), findsOneWidget);
    expect(find.text('Ready to Schedule'), findsNothing);
    expect(tester.takeException(), isNull);
  });
}
