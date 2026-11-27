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
}) => TaskItem(
  id: id,
  listId: 'list-1',
  title: id,
  dependencies: dependencies,
  isCompleted: completed,
  dueDate: dueDate,
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

    expect(container.read(selectedInspectorProvider).isCreating, isTrue);
    expect(container.read(selectedInspectorProvider).prerequisiteTaskId, 'A');
    expect(find.text('Follow-up to A'), findsOneWidget);
    await tester.tap(find.text('Advanced Settings'));
    await tester.pumpAndSettle();
    expect(
      tester.widget<FilterChip>(find.widgetWithText(FilterChip, 'A')).selected,
      isTrue,
    );
    await tester.enterText(find.byType(TextField).first, 'Next step');
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
      expect(
        tester.getSemantics(followUp).flagsCollection.isEnabled.toBoolOrNull(),
        isTrue,
      );
      await tester.tap(followUp);
      expect(container.read(selectedInspectorProvider).prerequisiteTaskId, 'B');
      expect(tester.takeException(), isNull);
    } finally {
      semantics.dispose();
    }
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
