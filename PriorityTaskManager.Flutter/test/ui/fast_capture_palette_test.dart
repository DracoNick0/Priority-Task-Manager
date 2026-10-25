import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/models/recurring_schedule_task.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/view_provider.dart';
import 'package:priority_task_manager/ui/command_center/fast_capture_palette.dart';

class _FakeMemoryTaskRepository implements TaskRepository {
  final Map<String, List<TaskItem>> _tasks = {};
  final List<TaskList> _lists = [TaskList(id: 'list-1', name: 'Work')];

  @override
  Future<List<TaskList>> getLists() async => _lists;

  @override
  Future<TaskList> createList({required String name, String? description}) async {
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
    for (final list in _tasks.values) {
      list.removeWhere((t) => t.id == taskId);
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
    await tester.enterText(
      find.byType(TextField),
      'Math homework problem set',
    );
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
        child: const MaterialApp(
          home: Scaffold(
            body: InlineFastCaptureBar(listId: 'list-1'),
          ),
        ),
      ),
    );
    await tester.pump();

    // Enter text in inline bar
    await tester.enterText(
      find.byType(TextField),
      'Read biology chapter 3',
    );
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
}
