import 'dart:typed_data';
import 'dart:ui' as ui;

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

  @override
  Future<void> archiveTask(String taskId) async {
    items.removeWhere((task) => task.id == taskId);
    ref.invalidateSelf();
    await future;
  }

  @override
  Future<void> deleteTask(String taskId) async {
    items.removeWhere((task) => task.id == taskId);
    for (var index = 0; index < items.length; index++) {
      items[index] = items[index].copyWith(
        dependencies: items[index].dependencies
            .where((id) => id != taskId)
            .toList(),
      );
    }
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
  bool occurrence = false,
  int estimatedMinutes = 60,
}) => TaskItem(
  id: id,
  listId: 'list-1',
  title: id,
  dependencies: dependencies,
  isCompleted: completed,
  dueDate: dueDate,
  recurrenceRule: recurrenceRule,
  seriesId: occurrence ? 'series-1' : null,
  occurrenceDate: occurrence ? DateTime(2026, 11, 1) : null,
  estimatedDurationMinutes: estimatedMinutes,
);

Future<ProviderContainer> _pumpProjects(
  WidgetTester tester,
  List<TaskItem> tasks, {
  bool guest = false,
  bool withInspector = false,
  Size size = const Size(1200, 1600),
}) async {
  tester.view.physicalSize = size;
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

Future<TestGesture> _startTaskDrag(
  WidgetTester tester,
  String taskId,
  int? stepNumber,
) async {
  final source = find.byKey(ValueKey('project-drag-$taskId-$stepNumber'));
  final gesture = await tester.startGesture(tester.getCenter(source));
  await tester.pump(kLongPressTimeout + const Duration(milliseconds: 100));
  return gesture;
}

Future<TestGesture> _startHandleDrag(
  WidgetTester tester,
  String taskId,
  int? stepNumber, {
  ui.PointerDeviceKind kind = ui.PointerDeviceKind.touch,
}) async {
  final source = find.byKey(
    ValueKey('project-drag-handle-$taskId-$stepNumber'),
  );
  final gesture = await tester.startGesture(
    tester.getCenter(source),
    kind: kind,
  );
  await gesture.moveBy(const Offset(0, -40));
  await tester.pump();
  return gesture;
}

Future<void> _openTaskActions(
  WidgetTester tester,
  String taskId,
  int? stepNumber,
) async {
  await tester.tap(find.byKey(ValueKey('project-actions-$taskId-$stepNumber')));
  await tester.pumpAndSettle();
}

typedef _RailPixels = ({ByteData data, int width, int height});

Future<_RailPixels> _paintRail(WidgetTester tester, String taskId) async {
  final rail = find.byKey(ValueKey('project-rail-$taskId'));
  final size = tester.getSize(rail);
  final recorder = ui.PictureRecorder();
  final painter = tester.widget<CustomPaint>(rail).painter!;
  painter.paint(ui.Canvas(recorder), size);
  final picture = recorder.endRecording();
  ByteData? data;
  try {
    data = await tester.runAsync<ByteData?>(() async {
      final image = await picture.toImage(
        size.width.ceil(),
        size.height.ceil(),
      );
      try {
        return await image.toByteData(format: ui.ImageByteFormat.rawRgba);
      } finally {
        image.dispose();
      }
    });
  } finally {
    picture.dispose();
  }
  if (data == null) throw StateError('Could not read rail pixels.');
  return (data: data, width: size.width.ceil(), height: size.height.ceil());
}

Color _railPixel(_RailPixels rail, int x, double y) {
  final index = (y.round() * rail.width + x) * 4;
  return Color.fromARGB(
    rail.data.getUint8(index + 3),
    rail.data.getUint8(index),
    rail.data.getUint8(index + 1),
    rail.data.getUint8(index + 2),
  );
}

int _railAlpha(_RailPixels rail, int x, double y) =>
    rail.data.getUint8((y.round() * rail.width + x) * 4 + 3);

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
    expect(find.text('Blocked by: A'), findsNWidgets(2));
    expect(find.text('Waiting on: A'), findsNothing);
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
    expect(find.text('Blocked by: A'), findsNothing);
    expect(tasks.first.isCompleted, isTrue);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'keeps the drag affordance below the checkbox and path beside step',
    (tester) async {
      await _pumpProjects(tester, [
        _task('A', estimatedMinutes: 120),
        _task('B', dependencies: ['A']),
      ]);

      final card = find.byKey(const ValueKey('project-task-A-1'));
      final checkbox = find.descendant(
        of: card,
        matching: find.byType(Checkbox),
      );
      final handle = find.descendant(
        of: card,
        matching: find.byIcon(Icons.drag_indicator),
      );
      final step = find.descendant(of: card, matching: find.text('Step 1'));
      final path = find.descendant(
        of: card,
        matching: find.text('Critical Path'),
      );
      final followUp = find.byKey(const ValueKey('project-follow-up-A-1'));

      expect(
        tester.getTopLeft(handle).dy,
        greaterThanOrEqualTo(tester.getBottomLeft(checkbox).dy),
      );
      expect(
        tester.getCenter(handle).dx,
        closeTo(tester.getCenter(checkbox).dx, 1),
      );
      expect(tester.getCenter(path).dy, closeTo(tester.getCenter(step).dy, 1));
      expect(
        tester.getTopLeft(path).dx,
        greaterThan(tester.getTopRight(step).dx),
      );
      expect(
        find.descendant(of: card, matching: find.text('+ Follow-Up')),
        findsOneWidget,
      );
      expect(
        tester.getTopLeft(followUp).dy,
        greaterThanOrEqualTo(tester.getBottomLeft(handle).dy),
      );
      expect(
        tester.getBottomRight(card).dx - tester.getBottomRight(followUp).dx,
        lessThan(24),
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('keeps task actions reachable on a narrow Projects viewport', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A'),
      _task('B', dependencies: ['A']),
    ], size: const Size(320, 720));

    final actions = find.byKey(const ValueKey('project-actions-B-2'));
    await tester.ensureVisible(actions);
    await tester.pumpAndSettle();
    await tester.tap(actions);
    await tester.pumpAndSettle();
    expect(find.text('Link Prerequisite...'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('centers visible incoming points before the outgoing endpoint', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A'),
      _task('B'),
      _task('C', dependencies: ['A', 'B']),
      _task('D', dependencies: ['C']),
    ]);

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

    final root = await _paintRail(tester, 'A');
    final merged = await _paintRail(tester, 'C');
    final leaf = await _paintRail(tester, 'D');
    final colors = Theme.of(tester.element(cRail)).colorScheme;
    expect(_railAlpha(root, root.width - 3, root.height / 2), greaterThan(0));
    expect(_railAlpha(root, root.width - 3, root.height / 2 + 28), 0);
    expect(merged.height, greaterThanOrEqualTo(108));
    for (final offset in [-28.0, 0.0, 28.0]) {
      expect(
        _railAlpha(merged, merged.width - 3, merged.height / 2 + offset),
        greaterThan(0),
      );
    }
    expect(_railAlpha(merged, merged.width - 3, merged.height / 2 - 14), 0);
    expect(_railAlpha(merged, merged.width - 3, merged.height / 2 + 14), 0);
    expect(
      _railPixel(merged, merged.width - 3, merged.height / 2 - 28),
      colors.primary,
    );
    expect(
      _railPixel(merged, merged.width - 3, merged.height / 2),
      colors.outline,
    );
    expect(
      _railPixel(merged, merged.width - 3, merged.height / 2 + 28),
      colors.primary,
    );
    expect(_railAlpha(leaf, leaf.width - 3, leaf.height / 2), greaterThan(0));
    expect(_railAlpha(leaf, leaf.width - 29, leaf.height / 2 + 2), 0);
    expect(find.byKey(const ValueKey('project-unlink-C-A')), findsNothing);
    expect(find.byKey(const ValueKey('project-unlink-C-B')), findsNothing);
    expect(find.byTooltip('C depends on A. Hover to unlink.'), findsOneWidget);
    expect(find.byTooltip('C depends on B. Hover to unlink.'), findsOneWidget);

    final branchBeforeHover = await _paintRail(tester, 'C');
    final branchX = branchBeforeHover.width - 3;
    final firstBranchY = branchBeforeHover.height / 2 - 28;
    final secondBranchY = branchBeforeHover.height / 2;
    final mouse = await tester.createGesture(kind: PointerDeviceKind.mouse);
    await mouse.addPointer(
      location: tester.getCenter(
        find.byKey(const ValueKey('project-link-C-B')),
      ),
    );
    await tester.pump();
    expect(find.byKey(const ValueKey('project-unlink-C-B')), findsOneWidget);
    expect(find.byKey(const ValueKey('project-unlink-C-A')), findsNothing);
    final branchAfterHover = await _paintRail(tester, 'C');
    expect(
      _railPixel(branchAfterHover, branchX, firstBranchY),
      _railPixel(branchBeforeHover, branchX, firstBranchY),
    );
    expect(
      _railPixel(branchAfterHover, branchX, secondBranchY),
      isNot(_railPixel(branchBeforeHover, branchX, secondBranchY)),
    );
    await mouse.removePointer();
    expect(tester.takeException(), isNull);
  });

  testWidgets('one root shares a centered outgoing point with two dependents', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A'),
      _task('B', dependencies: ['A']),
      _task('C', dependencies: ['A']),
    ]);

    final root = await _paintRail(tester, 'A');
    expect(_railAlpha(root, root.width - 3, root.height / 2), greaterThan(0));
    expect(_railAlpha(root, root.width - 3, root.height / 2 - 28), 0);
    expect(_railAlpha(root, root.width - 3, root.height / 2 + 28), 0);
    expect(tester.takeException(), isNull);
  });

  testWidgets('gaps unrelated rails but keeps DAG branches and joins intact', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A'),
      _task('B', dependencies: ['A'], estimatedMinutes: 120),
      _task('C', dependencies: ['A'], estimatedMinutes: 30),
      _task('D', dependencies: ['B', 'C']),
    ]);

    final colors = Theme.of(
      tester.element(find.byKey(const ValueKey('project-rail-D'))),
    ).colorScheme;
    final b = await _paintRail(tester, 'B');
    final c = await _paintRail(tester, 'C');
    final d = await _paintRail(tester, 'D');
    final bBranchY = b.height / 2 - 14;
    final cBranchY = c.height / 2 - 14;
    final dFirstBranchY = d.height / 2 - 14;
    final dSecondBranchY = d.height / 2 + 14;

    void expectCrossing(
      _RailPixels rail,
      int verticalX,
      double branchY,
      Color verticalColor,
      Color branchColor,
    ) {
      for (final offset in [-3.0, 3.0]) {
        expect(_railAlpha(rail, verticalX, branchY + offset), 0);
      }
      for (final offset in [-8.0, 8.0]) {
        expect(_railPixel(rail, verticalX, branchY + offset), verticalColor);
      }
      for (final offset in [-3, 3]) {
        expect(_railPixel(rail, verticalX + offset, branchY), branchColor);
      }
    }

    expectCrossing(c, 56, cBranchY, colors.primary, colors.outline);
    expectCrossing(d, 68, dFirstBranchY, colors.outline, colors.primary);
    expect(_railAlpha(b, 12, bBranchY - 3), greaterThan(0));
    expect(_railAlpha(b, 12, bBranchY + 3), greaterThan(0));
    expect(_railAlpha(c, 12, cBranchY - 9), greaterThan(0));
    expect(_railAlpha(d, 56, dFirstBranchY - 9), greaterThan(0));
    expect(_railPixel(b, b.width - 3, bBranchY), colors.primary);
    expect(_railPixel(c, c.width - 3, cBranchY), colors.outline);
    expect(_railPixel(d, d.width - 3, dFirstBranchY), colors.primary);
    expect(_railPixel(d, d.width - 3, dSecondBranchY), colors.outline);

    final mouse = await tester.createGesture(kind: PointerDeviceKind.mouse);
    await mouse.addPointer(
      location: tester.getCenter(
        find.byKey(const ValueKey('project-link-D-C')),
      ),
    );
    await tester.pump();
    final hoveredD = await _paintRail(tester, 'D');
    expect(
      _railPixel(hoveredD, hoveredD.width - 3, dFirstBranchY),
      colors.primary,
    );
    expect(
      _railPixel(hoveredD, hoveredD.width - 3, dSecondBranchY),
      colors.tertiary,
    );
    expect(_railAlpha(hoveredD, 68, dFirstBranchY - 3), 0);
    await mouse.removePointer();
    expect(tester.takeException(), isNull);
  });

  testWidgets('hovered rail button unlinks one dependency with Undo', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B'),
      _task('C', dependencies: ['A', 'B']),
    ];
    final container = await _pumpProjects(tester, tasks, guest: true);
    final mouse = await tester.createGesture(kind: PointerDeviceKind.mouse);
    await mouse.addPointer(
      location: tester.getCenter(
        find.byKey(const ValueKey('project-link-C-A')),
      ),
    );
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey('project-unlink-C-A')));
    await tester.pumpAndSettle();

    expect(tasks[2].dependencies, ['B']);
    expect(find.byKey(const ValueKey('project-link-C-A')), findsNothing);
    expect(find.byKey(const ValueKey('project-link-C-B')), findsOneWidget);
    final notification = container.read(appNotificationProvider)!;
    expect(notification.actionLabel, 'Undo');
    await notification.onAction!();
    await tester.pumpAndSettle();
    expect(tasks[2].dependencies, containsAll(['A', 'B']));
    expect(find.byKey(const ValueKey('project-link-C-A')), findsOneWidget);
    await mouse.removePointer();
    expect(tester.takeException(), isNull);
  });

  testWidgets('inspector chips remove one rail link', (tester) async {
    final tasks = [
      _task('A'),
      _task('B'),
      _task('C', dependencies: ['A', 'B']),
      _task('D', dependencies: ['C']),
    ];
    await _pumpProjects(tester, tasks, withInspector: true);

    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-C-2')),
        matching: find.text('C'),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('prerequisite-chip-A')), findsOneWidget);
    expect(find.byKey(const ValueKey('prerequisite-chip-B')), findsOneWidget);
    expect(find.byKey(const ValueKey('follow-up-chip-D')), findsOneWidget);
    final chip = find.byKey(const ValueKey('prerequisite-chip-A'));
    await tester.ensureVisible(chip);
    await tester.tapAt(
      tester.getCenter(chip) + Offset(tester.getSize(chip).width / 2 - 14, 0),
    );
    await tester.pumpAndSettle();

    expect(tasks[2].dependencies, ['B']);
    expect(find.byKey(const ValueKey('project-link-C-A')), findsNothing);
    expect(find.byKey(const ValueKey('project-link-C-B')), findsOneWidget);
    expect(find.byKey(const ValueKey('prerequisite-chip-B')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('search and hide-completed leave no phantom rail connections', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('A', completed: true),
      _task('B', dependencies: ['A']),
      _task('C', dependencies: ['B']),
    ]);

    await tester.tap(find.byKey(const ValueKey('project-hide-completed-A')));
    await tester.pumpAndSettle();
    final visibleRoot = await _paintRail(tester, 'B');
    expect(find.byKey(const ValueKey('project-rail-A')), findsNothing);
    expect(
      _railAlpha(visibleRoot, visibleRoot.width - 3, visibleRoot.height / 2),
      greaterThan(0),
    );
    expect(find.byKey(const ValueKey('project-link-B-A')), findsNothing);

    await tester.enterText(find.byKey(const ValueKey('projects-search')), 'B');
    await tester.pumpAndSettle();
    final isolated = await _paintRail(tester, 'B');
    expect(find.byKey(const ValueKey('project-rail-C')), findsNothing);
    expect(_railAlpha(isolated, isolated.width - 3, isolated.height / 2), 0);
    expect(_railAlpha(isolated, 12, isolated.height / 2), 0);
    expect(tester.takeException(), isNull);
  });

  testWidgets('filtered critical edges do not tint another visible link', (
    tester,
  ) async {
    await _pumpProjects(tester, [
      _task('Visible root'),
      _task(
        'Hidden heavy',
        dependencies: ['Visible root'],
        estimatedMinutes: 120,
      ),
      _task('Visible leaf', dependencies: ['Visible root']),
    ]);

    await tester.enterText(
      find.byKey(const ValueKey('projects-search')),
      'Visible',
    );
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey('project-rail-Hidden heavy')),
      findsNothing,
    );
    final rootFinder = find.byKey(const ValueKey('project-rail-Visible root'));
    final root = await _paintRail(tester, 'Visible root');
    expect(
      _railPixel(root, root.width - 3, root.height / 2),
      Theme.of(tester.element(rootFinder)).colorScheme.outline,
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

  testWidgets('dragging a task onto another links it with Undo', (
    tester,
  ) async {
    final tasks = [
      _task('A', dueDate: DateTime(2026, 12, 1)),
      _task('B', dueDate: DateTime(2026, 12, 1)),
    ];
    final container = await _pumpProjects(tester, tasks);

    expect(find.byTooltip('Drag B to link or detach'), findsOneWidget);
    final gesture = await _startTaskDrag(tester, 'B', null);
    await gesture.moveTo(
      tester.getCenter(find.byKey(const ValueKey('project-drop-A-null'))),
    );
    await tester.pump();
    expect(find.text('Make follow-up to A'), findsOneWidget);
    await gesture.up();
    await tester.pumpAndSettle();

    expect(tasks.last.dependencies, ['A']);
    expect(find.byKey(const ValueKey('project-task-B-2')), findsOneWidget);
    final notification = container.read(appNotificationProvider)!;
    expect(notification.actionLabel, 'Undo');
    await notification.onAction!();
    await tester.pumpAndSettle();
    expect(tasks.last.dependencies, isEmpty);
    expect(find.byKey(const ValueKey('project-task-B-null')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  for (final guest in [false, true]) {
    testWidgets('drag handle links immediately with Undo in '
        '${guest ? 'Guest' : 'authenticated'} mode', (tester) async {
      final tasks = [
        _task('A', dueDate: DateTime(2026, 12, 1)),
        _task('B', dueDate: DateTime(2026, 12, 1)),
      ];
      final container = await _pumpProjects(tester, tasks, guest: guest);

      final gesture = await _startHandleDrag(
        tester,
        'B',
        null,
        kind: guest ? ui.PointerDeviceKind.touch : ui.PointerDeviceKind.mouse,
      );
      await gesture.moveTo(
        tester.getCenter(find.byKey(const ValueKey('project-drop-A-null'))),
      );
      await tester.pump();
      expect(find.text('Make follow-up to A'), findsOneWidget);
      await gesture.up();
      await tester.pumpAndSettle();

      expect(tasks.last.dependencies, ['A']);
      final notification = container.read(appNotificationProvider)!;
      expect(notification.actionLabel, 'Undo');
      await notification.onAction!();
      await tester.pumpAndSettle();
      expect(tasks.last.dependencies, isEmpty);
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('drag handle detaches immediately with Undo', (tester) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
    ];
    final container = await _pumpProjects(tester, tasks);

    final gesture = await _startHandleDrag(tester, 'B', 2);
    final zone = find.byKey(const ValueKey('project-detach-zone'));
    expect(zone, findsOneWidget);
    await gesture.moveTo(tester.getCenter(zone));
    await tester.pump();
    await gesture.up();
    await tester.pumpAndSettle();

    expect(tasks.last.dependencies, isEmpty);
    final notification = container.read(appNotificationProvider)!;
    expect(notification.actionLabel, 'Undo');
    await notification.onAction!();
    await tester.pumpAndSettle();
    expect(tasks.last.dependencies, ['A']);
    expect(tester.takeException(), isNull);
  });

  testWidgets('drag target rejects a cyclic dependency before saving', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
      _task('C', dependencies: ['B']),
    ];
    final container = await _pumpProjects(tester, tasks);

    final gesture = await _startTaskDrag(tester, 'A', 1);
    await gesture.moveTo(
      tester.getCenter(find.byKey(const ValueKey('project-drop-C-3'))),
    );
    await tester.pump();
    expect(
      find.text('This link would create a circular dependency.'),
      findsOneWidget,
    );
    expect(
      find.descendant(
        of: find.byKey(const ValueKey('project-drop-C-3')),
        matching: find.byIcon(Icons.block),
      ),
      findsOneWidget,
    );
    await gesture.up();
    await tester.pumpAndSettle();

    expect(tasks.first.dependencies, isEmpty);
    expect(container.read(appNotificationProvider), isNull);
    expect(tester.takeException(), isNull);
  });

  testWidgets('drop revalidates links changed during the drag', (tester) async {
    final tasks = [
      _task('A', dueDate: DateTime(2026, 12, 1)),
      _task('B', dueDate: DateTime(2026, 12, 1)),
    ];
    final container = await _pumpProjects(tester, tasks);
    final gesture = await _startTaskDrag(tester, 'B', null);
    await gesture.moveTo(
      tester.getCenter(find.byKey(const ValueKey('project-drop-A-null'))),
    );
    await tester.pump();
    expect(find.text('Make follow-up to A'), findsOneWidget);

    await container
        .read(tasksProvider('list-1').notifier)
        .updateTask(tasks.first.copyWith(dependencies: ['B']));
    await tester.pumpAndSettle();
    await gesture.moveTo(
      tester.getCenter(find.byKey(const ValueKey('project-drop-A-2'))),
    );
    await tester.pump();
    expect(
      find.text('This link would create a circular dependency.'),
      findsOneWidget,
    );
    await gesture.up();
    await tester.pumpAndSettle();

    expect(tasks.last.dependencies, isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('recurring tasks and occurrences cannot be dragged or linked', (
    tester,
  ) async {
    final tasks = [
      _task('A', dueDate: DateTime(2026, 12, 1)),
      _task(
        'Recurring',
        recurrenceRule: DailyIntervalRecurrenceRule(
          seriesStartDate: DateTime(2026, 11, 1),
          endCondition: NeverEndCondition(),
        ),
      ),
      _task('Occurrence', occurrence: true),
    ];
    await _pumpProjects(tester, tasks);
    expect(
      find.byKey(const ValueKey('project-drag-Recurring-null')),
      findsNothing,
    );
    expect(
      find.byKey(const ValueKey('project-drag-Occurrence-null')),
      findsNothing,
    );
    expect(
      find.byKey(const ValueKey('project-drag-handle-Recurring-null')),
      findsNothing,
    );
    expect(
      find.byKey(const ValueKey('project-drag-handle-Occurrence-null')),
      findsNothing,
    );

    for (final id in ['Recurring', 'Occurrence']) {
      final gesture = await _startTaskDrag(tester, 'A', null);
      await gesture.moveTo(
        tester.getCenter(find.byKey(ValueKey('project-drop-$id-null'))),
      );
      await tester.pump();
      expect(find.text('Recurring tasks cannot be linked.'), findsOneWidget);
      await gesture.up();
      await tester.pumpAndSettle();
    }
    expect(tasks[1].dependencies, isEmpty);
    expect(tasks[2].dependencies, isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('dropping on the backlog detaches all incoming links with Undo', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B'),
      _task('C', dependencies: ['A', 'B']),
    ];
    final container = await _pumpProjects(tester, tasks);

    final gesture = await _startTaskDrag(tester, 'C', 2);
    final zone = find.byKey(const ValueKey('project-detach-zone'));
    expect(zone, findsOneWidget);
    await gesture.moveTo(tester.getCenter(zone));
    await tester.pump();
    await gesture.up();
    await tester.pumpAndSettle();

    expect(tasks.last.dependencies, isEmpty);
    expect(find.byKey(const ValueKey('project-task-C-null')), findsOneWidget);
    final notification = container.read(appNotificationProvider)!;
    expect(notification.actionLabel, 'Undo');
    await notification.onAction!();
    await tester.pumpAndSettle();
    expect(tasks.last.dependencies, ['A', 'B']);
    expect(find.byKey(const ValueKey('project-task-C-2')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('detach Undo refuses links that became cyclic', (tester) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
    ];
    final container = await _pumpProjects(tester, tasks);
    final gesture = await _startTaskDrag(tester, 'B', 2);
    await gesture.moveTo(
      tester.getCenter(find.byKey(const ValueKey('project-detach-zone'))),
    );
    await tester.pump();
    await gesture.up();
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

  testWidgets('card actions link both directions and detach prerequisites', (
    tester,
  ) async {
    final tasks = [_task('A'), _task('B'), _task('C')];
    final container = await _pumpProjects(tester, tasks);

    await _openTaskActions(tester, 'B', null);
    expect(find.text('Link Prerequisite...'), findsOneWidget);
    expect(find.text('Link Follow-Up...'), findsOneWidget);
    expect(find.text('Mark Complete'), findsNothing);
    expect(find.text('Archive Task'), findsOneWidget);
    await tester.tap(find.text('Link Prerequisite...'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('link-candidate-A')));
    await tester.pumpAndSettle();
    expect(tasks[1].dependencies, ['A']);

    await _openTaskActions(tester, 'B', 2);
    expect(find.text('Detach from Chain'), findsOneWidget);
    await tester.tap(find.text('Link Follow-Up...'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('link-candidate-C')));
    await tester.pumpAndSettle();
    expect(tasks[2].dependencies, ['B']);

    await _openTaskActions(tester, 'B', 2);
    await tester.tap(find.text('Detach from Chain'));
    await tester.pumpAndSettle();
    expect(tasks[1].dependencies, isEmpty);
    expect(tasks[2].dependencies, ['B']);
    expect(find.byKey(const ValueKey('project-task-B-1')), findsOneWidget);
    expect(container.read(appNotificationProvider)!.actionLabel, 'Undo');
    expect(tester.takeException(), isNull);
  });

  testWidgets('card prerequisite picker disables cyclic candidates', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
      _task('C', dependencies: ['B']),
    ];
    await _pumpProjects(tester, tasks);

    await _openTaskActions(tester, 'A', 1);
    await tester.tap(find.text('Link Prerequisite...'));
    await tester.pumpAndSettle();
    expect(
      tester
          .widget<ListTile>(find.byKey(const ValueKey('link-candidate-C')))
          .enabled,
      isFalse,
    );
    expect(find.text('Would create a circular dependency'), findsNWidgets(2));
    expect(tasks.first.dependencies, isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('card actions omit completion and allow reopening', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
    ];
    await _pumpProjects(tester, tasks);

    await _openTaskActions(tester, 'B', 2);
    expect(find.text('Mark Complete'), findsNothing);
    expect(find.text('Reopen'), findsNothing);
    final blockedCheckbox = find.descendant(
      of: find.byKey(const ValueKey('project-task-B-2')),
      matching: find.byType(Checkbox),
    );
    expect(tester.widget<Checkbox>(blockedCheckbox).onChanged, isNull);
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();

    await _openTaskActions(tester, 'A', 1);
    expect(find.text('No incoming links to remove'), findsOneWidget);
    expect(find.text('Mark Complete'), findsNothing);
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();
    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-A-1')),
        matching: find.byType(Checkbox),
      ),
    );
    await tester.pumpAndSettle();
    expect(tasks.first.isCompleted, isTrue);

    await _openTaskActions(tester, 'B', 2);
    expect(find.text('Mark Complete'), findsNothing);
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();
    await tester.tap(
      find.descendant(
        of: find.byKey(const ValueKey('project-task-B-2')),
        matching: find.byType(Checkbox),
      ),
    );
    await tester.pumpAndSettle();
    expect(tasks.last.isCompleted, isTrue);

    await tester.tap(find.byKey(const ValueKey('projects-filter-completed')));
    await tester.pumpAndSettle();
    await _openTaskActions(tester, 'B', null);
    await tester.tap(find.text('Reopen'));
    await tester.pumpAndSettle();
    expect(tasks.last.isCompleted, isFalse);
    await tester.tap(find.byKey(const ValueKey('projects-filter-all')));
    await tester.pumpAndSettle();
    await _openTaskActions(tester, 'B', 2);
    expect(find.text('Mark Complete'), findsNothing);
    expect(find.text('Reopen'), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('Archive Task archives authenticated one-off tasks', (
    tester,
  ) async {
    final tasks = [_task('A', dueDate: DateTime(2026, 12, 1))];
    final container = await _pumpProjects(tester, tasks, withInspector: true);
    await tester.tap(find.text('A').first);
    await tester.pumpAndSettle();
    expect(container.read(selectedInspectorProvider).id, 'A');

    await _openTaskActions(tester, 'A', null);
    await tester.tap(find.text('Archive Task'));
    await tester.pumpAndSettle();
    expect(tasks, isEmpty);
    expect(container.read(selectedInspectorProvider).kind, InspectorKind.none);
    expect(container.read(appNotificationProvider)!.message, 'A archived');
    expect(tester.takeException(), isNull);
  });

  testWidgets('Delete Task permanently removes a Guest task and its links', (
    tester,
  ) async {
    final guestTasks = [
      _task('Guest Task'),
      _task('Dependent', dependencies: ['Guest Task']),
    ];
    final guestContainer = await _pumpProjects(tester, guestTasks, guest: true);
    await _openTaskActions(tester, 'Guest Task', 1);
    expect(find.text('Archive Task'), findsNothing);
    await tester.tap(find.text('Delete Task'));
    await tester.pumpAndSettle();
    expect(guestTasks, hasLength(1));
    expect(guestTasks.single.dependencies, isEmpty);
    expect(
      guestContainer.read(appNotificationProvider)!.message,
      'Guest Task deleted',
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('Archive Task routes recurring series to the scoped inspector', (
    tester,
  ) async {
    final tasks = [
      _task(
        'Recurring',
        recurrenceRule: DailyIntervalRecurrenceRule(
          seriesStartDate: DateTime(2026, 11, 1),
          endCondition: NeverEndCondition(),
        ),
      ),
    ];
    final container = await _pumpProjects(tester, tasks, withInspector: true);
    await _openTaskActions(tester, 'Recurring', null);
    final linkAction = find.ancestor(
      of: find.text('Link Prerequisite...'),
      matching: find.byWidgetPredicate((widget) => widget is PopupMenuItem),
    );
    expect(tester.widget<PopupMenuItem>(linkAction).enabled, isFalse);
    await tester.tap(find.text('Archive Task'));
    await tester.pumpAndSettle();

    expect(tasks, hasLength(1));
    expect(container.read(selectedInspectorProvider).id, 'Recurring');
    expect(
      container.read(appNotificationProvider)!.message,
      contains('recurring-task scope'),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('empty Projects still opens the normal new-task inspector', (
    tester,
  ) async {
    final tasks = <TaskItem>[];
    final container = await _pumpProjects(tester, tasks, withInspector: true);

    await tester.tap(find.text('Add a task'));
    await tester.pumpAndSettle();

    expect(container.read(selectedInspectorProvider).isCreating, isTrue);
    expect(
      container.read(selectedInspectorProvider).prerequisiteTaskId,
      isNull,
    );
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
    expect(tasks.single.title, 'Next step');
    expect(tasks.single.dependencies, isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('blocked tasks keep their follow-up action accessible', (
    tester,
  ) async {
    final tasks = [
      _task('A'),
      _task('B', dependencies: ['A']),
      _task('C'),
    ];
    final container = await _pumpProjects(tester, tasks);

    final semantics = tester.ensureSemantics();
    final followUp = find.descendant(
      of: find.byKey(const ValueKey('project-task-B-2')),
      matching: find.text('+ Follow-Up'),
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
      expect(find.text('Link Existing Follow-Up'), findsOneWidget);
      await tester.tap(find.byKey(const ValueKey('link-candidate-C')));
      await tester.pumpAndSettle();
      expect(tasks.last.dependencies, ['B']);
      expect(container.read(selectedInspectorProvider).isCreating, isFalse);
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
          matching: find.text('+ Follow-Up'),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Link Existing Follow-Up'), findsOneWidget);
      final cyclic = find.byKey(const ValueKey('link-candidate-A'));
      expect(tester.widget<ListTile>(cyclic).enabled, isFalse);
      expect(find.text('Would create a circular dependency'), findsNWidgets(2));
      expect(
        find.byKey(const ValueKey('link-candidate-Recurring')),
        findsNothing,
      );
      expect(
        find.descendant(
          of: find.byKey(const ValueKey('project-task-Recurring-null')),
          matching: find.text('+ Follow-Up'),
        ),
        findsNothing,
      );
      expect(find.byKey(const ValueKey('link-candidate-C')), findsNothing);
      await tester.tap(
        find.byKey(const ValueKey('link-candidate-Independent')),
      );
      await tester.pumpAndSettle();

      expect(tasks[3].dependencies, ['C']);
      expect(find.text('Blocked by: C'), findsOneWidget);
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
        matching: find.text('+ Follow-Up'),
      ),
    );
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
