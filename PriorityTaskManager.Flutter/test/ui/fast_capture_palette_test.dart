import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/fixed_event.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/models/recurring_schedule_task.dart';
import 'package:priority_task_manager/models/schedule_models.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/selection_provider.dart';
import 'package:priority_task_manager/providers/session_provider.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/view_provider.dart';
import 'package:priority_task_manager/ui/command_center/command_center_screen.dart';
import 'package:priority_task_manager/ui/command_center/daily_rituals_dialog.dart';
import 'package:priority_task_manager/ui/command_center/fast_capture_palette.dart';

class _FakeMemoryTaskRepository implements TaskRepository {
  final Map<String, List<TaskItem>> _tasks = {};
  final Map<String, List<FixedEvent>> _events = {};
  final List<TaskList> _lists = [TaskList(id: 'list-1', name: 'Work')];
  final List<String> deletedTaskIds = [];
  final List<String> archivedTaskIds = [];
  final List<String> addedTaskTitles = [];
  final List<FixedEvent> addedEvents = [];

  @override
  Future<List<TaskList>> getLists() async => _lists;

  @override
  Future<TaskList> createList({
    required String name,
    String? description,
  }) async {
    final list = TaskList(id: 'list-${_lists.length + 1}', name: name);
    _lists.add(list);
    return list;
  }

  @override
  Future<void> updateList(TaskList list) async {
    final index = _lists.indexWhere((l) => l.id == list.id);
    if (index >= 0) _lists[index] = list;
  }

  @override
  Future<void> deleteList(String listId) async {
    _lists.removeWhere((l) => l.id == listId);
  }

  @override
  Future<List<TaskItem>> getTasks(String listId) async => _tasks[listId] ?? [];

  void seedTask(TaskItem task) {
    (_tasks[task.listId] ??= []).add(task);
  }

  @override
  Future<TaskItem> getTaskOccurrence({
    required String listId,
    required String seriesId,
    required DateTime occurrenceDate,
  }) => throw UnimplementedError();

  @override
  Future<List<RecurringScheduleTask>> getRecurringTasksForScheduling(
    String listId,
  ) async => [];

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
    final item = TaskItem(
      id: 'task-${DateTime.now().microsecondsSinceEpoch}',
      listId: listId,
      title: title,
      description: description,
      dueDate: dueDate,
      estimatedDurationMinutes: estimatedDurationMinutes,
      importance: importance,
      complexity: complexity,
      notBefore: notBefore,
      isPinned: isPinned,
      isDivisible: isDivisible,
      link: link,
    );
    addedTaskTitles.add(title);
    (_tasks[listId] ??= []).add(item);
    return item;
  }

  @override
  Future<void> updateTask(TaskItem task) async {
    final list = _tasks[task.listId];
    if (list != null) {
      final index = list.indexWhere((t) => t.id == task.id);
      if (index >= 0) list[index] = task;
    }
  }

  @override
  Future<void> deleteTask(String taskId) async {
    deletedTaskIds.add(taskId);
    for (final list in _tasks.values) {
      list.removeWhere((t) => t.id == taskId);
    }
  }

  @override
  Future<void> archiveTask(String taskId) async {
    archivedTaskIds.add(taskId);
    for (final list in _tasks.values) {
      list.removeWhere((task) => task.id == taskId);
    }
  }

  @override
  Future<List<FixedEvent>> getEvents(String listId) async =>
      _events[listId] ?? [];

  @override
  Future<List<FixedEvent>> getEventOccurrences(
    String listId,
    DateTime from,
    DateTime to,
  ) async => _events[listId] ?? [];

  @override
  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
    String description = '',
    String link = '',
    RecurrenceRule? recurrenceRule,
  }) async {
    final event = FixedEvent(
      id: 'event-${addedEvents.length + 1}',
      listId: listId,
      title: title,
      startTime: startTime,
      endTime: endTime,
      description: description,
      link: link,
    );
    addedEvents.add(event);
    (_events[listId] ??= []).add(event);
    return event;
  }

  @override
  Future<void> deleteEvent(String eventId) async {
    for (final events in _events.values) {
      events.removeWhere((event) => event.id == eventId);
    }
  }

  @override
  Future<void> setCompleted(String taskId, bool isCompleted) async {
    for (final list in _tasks.values) {
      final index = list.indexWhere((t) => t.id == taskId);
      if (index >= 0) {
        final t = list[index];
        list[index] = TaskItem(
          id: t.id,
          listId: t.listId,
          title: t.title,
          isCompleted: isCompleted,
          estimatedDurationMinutes: t.estimatedDurationMinutes,
          importance: t.importance,
          dueDate: t.dueDate,
        );
      }
    }
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _Session extends SessionController {
  _Session(this.status);

  final SessionStatus status;

  @override
  Future<SessionState> build() async => SessionState(status: status);
}

Future<ProviderContainer> _pumpCommandCenter(
  WidgetTester tester, {
  required _FakeMemoryTaskRepository repository,
  SessionStatus session = SessionStatus.guest,
  double width = 1280,
}) async {
  tester.view.physicalSize = Size(width, 800);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final container = ProviderContainer(
    overrides: [
      taskRepositoryProvider.overrideWith((ref) => repository),
      activeListIdProvider.overrideWith((ref) => 'list-1'),
      engineClockProvider.overrideWith(
        (ref) => Stream.value(DateTime(2026, 10, 24, 10, 0)),
      ),
      sessionControllerProvider.overrideWith(() => _Session(session)),
      viewModeProvider.overrideWith((ref) => ViewMode.projects),
      dailyRitualsStateProvider.overrideWith(
        (ref) => const DailyRitualsState(
          lastMorningDate: '2026-10-24',
          lastEveningDate: '2026-10-24',
        ),
      ),
      scheduleProvider.overrideWith((ref) async => DailySchedule.empty()),
    ],
  );
  addTearDown(container.dispose);

  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: container,
      child: const MaterialApp(home: CommandCenterScreen()),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
  return container;
}

Future<void> _sendShortcut(
  WidgetTester tester,
  LogicalKeyboardKey modifier,
  LogicalKeyboardKey key,
) async {
  await tester.sendKeyDownEvent(modifier);
  await tester.sendKeyDownEvent(key);
  await tester.sendKeyUpEvent(key);
  await tester.sendKeyUpEvent(modifier);
}

void main() {
  testWidgets('Fast capture dialog renders smart defaults and submits task', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository();
    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(DateTime(2026, 10, 24, 10, 0)),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => showFastCapturePalette(context),
                child: const Text('Open Palette'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    // Open palette
    await tester.tap(find.text('Open Palette'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    // Verify smart default micro-pills
    expect(find.text('60m'), findsOneWidget);
    expect(find.text('Normal (5)'), findsOneWidget);

    // Enter title
    await tester.enterText(find.byType(TextField), 'Math homework problem set');
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    // SnackBar appears with undo
    expect(find.text('Added task "Math homework problem set"'), findsOneWidget);
    expect(find.text('Undo'), findsOneWidget);
  });

  testWidgets('Inline fast-capture bar creates task on enter', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository();
    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(DateTime(2026, 10, 24, 10, 0)),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp(
          home: Scaffold(
            body: InlineFastCaptureBar(
              listId: 'list-1',
              onOpenInspector: () {},
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    // Enter text in inline bar
    await tester.enterText(find.byType(TextField), 'Read biology chapter 3');
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('Added task "Read biology chapter 3"'), findsOneWidget);
  });

  testWidgets('Command Palette quick commands switch views', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository();
    final container = ProviderContainer(
      overrides: [
        taskRepositoryProvider.overrideWith((ref) => repository),
        activeListIdProvider.overrideWith((ref) => 'list-1'),
        engineClockProvider.overrideWith(
          (ref) => Stream.value(DateTime(2026, 10, 24, 10, 0)),
        ),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => showFastCapturePalette(context),
                child: const Text('Open Palette'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    await tester.tap(find.text('Open Palette'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    // Tap quick command to switch to Horizon
    await tester.tap(find.text('Switch view to: Week Horizon Grid'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(container.read(viewModeProvider), ViewMode.horizon);
  });

  testWidgets(
    'Global Ctrl+K opens palette from a view control and Alt+Enter opens the empty inspector',
    (WidgetTester tester) async {
      final repository = _FakeMemoryTaskRepository();
      final container = await _pumpCommandCenter(
        tester,
        repository: repository,
      );

      expect(find.byTooltip('Fast Capture (Ctrl + K)'), findsNothing);
      expect(find.byTooltip('Command Palette (Ctrl + K)'), findsOneWidget);
      expect(find.byTooltip('Cleanup completed tasks'), findsOneWidget);
      expect(find.byTooltip('Add Task'), findsNothing);
      expect(find.byTooltip('Add Event'), findsNothing);

      await tester.tap(find.text('Today'));
      await tester.pump();
      await _sendShortcut(
        tester,
        LogicalKeyboardKey.controlLeft,
        LogicalKeyboardKey.keyK,
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 300));

      expect(find.text('Fast Capture & Command Palette'), findsOneWidget);

      await _sendShortcut(
        tester,
        LogicalKeyboardKey.altLeft,
        LogicalKeyboardKey.enter,
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 300));

      expect(
        container.read(selectedInspectorProvider).kind,
        InspectorKind.empty,
      );
      expect(find.text('New Task'), findsOneWidget);
      expect(find.text('New Event'), findsOneWidget);
      expect(repository.addedTaskTitles, isEmpty);
    },
  );

  testWidgets('Global Cmd+K opens the palette from a view control', (
    WidgetTester tester,
  ) async {
    await _pumpCommandCenter(tester, repository: _FakeMemoryTaskRepository());

    await tester.tap(find.text('Today'));
    await tester.pump();
    await _sendShortcut(
      tester,
      LogicalKeyboardKey.metaLeft,
      LogicalKeyboardKey.keyK,
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.text('Fast Capture & Command Palette'), findsOneWidget);
  });

  testWidgets('Alt+Enter keeps the empty inspector open when docked', (
    WidgetTester tester,
  ) async {
    final container = await _pumpCommandCenter(
      tester,
      repository: _FakeMemoryTaskRepository(),
    );

    await tester.tap(find.byTooltip('Show inspector'));
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Dock panel'));
    await tester.pumpAndSettle();

    expect(find.byTooltip('Show inspector'), findsNothing);

    await tester.tap(find.text('Today'));
    await tester.pump();
    await _sendShortcut(
      tester,
      LogicalKeyboardKey.controlLeft,
      LogicalKeyboardKey.keyK,
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
    await _sendShortcut(
      tester,
      LogicalKeyboardKey.altLeft,
      LogicalKeyboardKey.enter,
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(container.read(selectedInspectorProvider).kind, InspectorKind.empty);
    expect(find.text('New Task'), findsOneWidget);
    expect(find.text('New Event'), findsOneWidget);
    expect(find.byTooltip('Show inspector'), findsNothing);
  });

  testWidgets('Shift+Enter still adds an event from the command palette', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository();
    await _pumpCommandCenter(tester, repository: repository);

    await tester.tap(find.text('Today'));
    await tester.pump();
    await _sendShortcut(
      tester,
      LogicalKeyboardKey.controlLeft,
      LogicalKeyboardKey.keyK,
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    await tester.enterText(find.byType(TextField).last, 'Planning meeting');
    await _sendShortcut(
      tester,
      LogicalKeyboardKey.shiftLeft,
      LogicalKeyboardKey.enter,
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(repository.addedEvents.map((event) => event.title), [
      'Planning meeting',
    ]);
    expect(repository.addedTaskTitles, isEmpty);
  });

  testWidgets('Cleanup deletes completed tasks for Guests', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository()
      ..seedTask(
        TaskItem(
          id: 'completed-task',
          listId: 'list-1',
          title: 'Completed',
          isCompleted: true,
        ),
      )
      ..seedTask(
        TaskItem(id: 'pending-task', listId: 'list-1', title: 'Pending'),
      );
    await _pumpCommandCenter(tester, repository: repository);
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Cleanup completed tasks'));
    await tester.pumpAndSettle();

    expect(repository.deletedTaskIds, ['completed-task']);
    expect(repository.archivedTaskIds, isEmpty);
    expect(repository._tasks['list-1']!.map((task) => task.id), [
      'pending-task',
    ]);
  });

  testWidgets('Cleanup archives completed tasks for authenticated users', (
    WidgetTester tester,
  ) async {
    final repository = _FakeMemoryTaskRepository()
      ..seedTask(
        TaskItem(
          id: 'completed-task',
          listId: 'list-1',
          title: 'Completed',
          isCompleted: true,
        ),
      )
      ..seedTask(
        TaskItem(id: 'pending-task', listId: 'list-1', title: 'Pending'),
      );
    await _pumpCommandCenter(
      tester,
      repository: repository,
      session: SessionStatus.authenticated,
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Cleanup completed tasks'));
    await tester.pumpAndSettle();

    expect(repository.archivedTaskIds, ['completed-task']);
    expect(repository.deletedTaskIds, isEmpty);
    expect(repository._tasks['list-1']!.map((task) => task.id), [
      'pending-task',
    ]);
  });

  testWidgets('Compact app bar retains the active list name', (
    WidgetTester tester,
  ) async {
    await _pumpCommandCenter(
      tester,
      repository: _FakeMemoryTaskRepository(),
      width: 600,
    );

    expect(find.text('Work'), findsOneWidget);
  });
}
