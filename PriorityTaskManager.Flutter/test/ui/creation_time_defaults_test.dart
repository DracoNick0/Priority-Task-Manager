import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/models/user_profile.dart';
import 'package:priority_task_manager/providers/app_notifications_provider.dart';
import 'package:priority_task_manager/providers/engine_status_provider.dart';
import 'package:priority_task_manager/providers/event_providers.dart';
import 'package:priority_task_manager/providers/session_provider.dart';
import 'package:priority_task_manager/providers/task_providers.dart';
import 'package:priority_task_manager/providers/user_profile_provider.dart';
import 'package:priority_task_manager/ui/command_center/inspector_forms/event_inspector_form.dart';
import 'package:priority_task_manager/ui/command_center/inspector_forms/task_inspector_form.dart';

class _Lists extends TaskListsNotifier {
  _Lists(this.lists);
  final List<TaskList> lists;

  @override
  Future<List<TaskList>> build() async => lists;
}

class _Profile extends UserProfileNotifier {
  @override
  Future<UserProfile> build() async => UserProfile();
}

class _Session extends SessionController {
  _Session(this.status);
  final SessionStatus status;

  @override
  Future<SessionState> build() async => SessionState(status: status);
}

class _Tasks extends TasksNotifier {
  String? addedTitle;
  String? addedLink;

  @override
  Future<List<TaskItem>> build(String arg) async => [];

  @override
  Future<TaskItem> addTask({
    required String title,
    String description = '',
    DateTime? dueDate,
    int estimatedDurationMinutes = 60,
    List<String>? dependencies,
    int importance = 5,
    int complexity = 1,
    DateTime? notBefore,
    bool isPinned = false,
    bool isDivisible = true,
    String link = '',
  }) async {
    addedTitle = title;
    addedLink = link;
    return TaskItem(id: 'created-task', listId: arg, title: title, link: link);
  }
}

class _Events extends EventsNotifier {
  String? addedTitle;

  @override
  Future<List<FixedEvent>> build(String arg) async => [];

  @override
  Future<FixedEvent> addEvent({
    required String title,
    required DateTime startTime,
    required DateTime endTime,
    String description = '',
    String link = '',
    RecurrenceRule? recurrenceRule,
  }) async {
    addedTitle = title;
    return FixedEvent(
      id: 'created-event',
      listId: arg,
      title: title,
      startTime: startTime,
      endTime: endTime,
    );
  }
}

void main() {
  final simulated = DateTime(2026, 10, 2, 16, 15);

  Future<void> pumpForm(
    WidgetTester tester, {
    required Widget form,
    required TaskList list,
    SessionStatus session = SessionStatus.authenticated,
    _Tasks? tasksNotifier,
    _Events? eventsNotifier,
  }) async {
    tester.view.physicalSize = const Size(800, 1200);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          taskListsProvider.overrideWith(() => _Lists([list])),
          userProfileProvider.overrideWith(_Profile.new),
          sessionControllerProvider.overrideWith(() => _Session(session)),
          tasksProvider.overrideWith(() => tasksNotifier ?? _Tasks()),
          eventsProvider.overrideWith(() => eventsNotifier ?? _Events()),
          engineClockProvider.overrideWith((ref) => Stream.value(simulated)),
          eventOccurrencesProvider((
            'list',
            DateTime(2026, 10, 2),
          )).overrideWith((ref) async => []),
        ],
        child: MaterialApp(
          home: Scaffold(body: SizedBox(width: 600, height: 900, child: form)),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  Future<void> submitName(WidgetTester tester, String title) async {
    await tester.enterText(find.byType(TextField).first, title);
    final createButton = find.widgetWithText(FilledButton, 'Create');
    await tester.scrollUntilVisible(
      createButton,
      250,
      scrollable: find
          .descendant(
            of: find.byType(ListView).first,
            matching: find.byType(Scrollable),
          )
          .first,
    );
    await tester.tap(createButton);
    await tester.pumpAndSettle();
  }

  void expectNameWarning(
    WidgetTester tester, {
    required Type formType,
    required String message,
  }) {
    final container = ProviderScope.containerOf(
      tester.element(find.byType(formType)),
    );
    final notification = container.read(appNotificationProvider);
    expect(notification?.message, message);
    expect(notification?.icon, Icons.warning_amber_rounded);
    expect(
      tester
          .widget<TextField>(find.byType(TextField).first)
          .decoration
          ?.errorText,
      message,
    );
  }

  testWidgets('task uses simulated time and list workday/end override', (
    tester,
  ) async {
    await pumpForm(
      tester,
      form: const TaskInspectorForm(listId: 'list'),
      list: TaskList(
        id: 'list',
        name: 'Work',
        simulatedTime: simulated,
        workDays: [DateTime.tuesday],
        workEndMinutes: 18 * 60 + 30,
      ),
    );

    expect(find.text('Tue, Oct 6 • 6:30 PM'), findsOneWidget);
  });

  for (final invalidTitle in ['', ' \t ']) {
    final nameKind = invalidTitle.isEmpty ? 'empty' : 'whitespace-only';

    testWidgets('task form warns on $nameKind names without creating a task', (
      tester,
    ) async {
      final tasks = _Tasks();
      await pumpForm(
        tester,
        form: const TaskInspectorForm(listId: 'list'),
        list: TaskList(id: 'list', name: 'Work'),
        tasksNotifier: tasks,
      );

      await submitName(tester, invalidTitle);

      expectNameWarning(
        tester,
        formType: TaskInspectorForm,
        message: 'Enter a task name.',
      );
      expect(tasks.addedTitle, isNull);
    });

    testWidgets(
      'event form warns on $nameKind names without creating an event',
      (tester) async {
        final events = _Events();
        await pumpForm(
          tester,
          form: const EventInspectorForm(listId: 'list'),
          list: TaskList(id: 'list', name: 'Work'),
          eventsNotifier: events,
        );

        await submitName(tester, invalidTitle);

        expectNameWarning(
          tester,
          formType: EventInspectorForm,
          message: 'Enter an event name.',
        );
        expect(events.addedTitle, isNull);
      },
    );
  }

  testWidgets('task form saves a link when creating a task', (tester) async {
    final tasks = _Tasks();
    await pumpForm(
      tester,
      form: const TaskInspectorForm(listId: 'list'),
      list: TaskList(id: 'list', name: 'Work'),
      tasksNotifier: tasks,
    );

    final linkField = find.byWidgetPredicate(
      (widget) => widget is TextField && widget.decoration?.labelText == 'Link',
    );
    await tester.enterText(linkField, 'https://example.com/reference');
    await submitName(tester, 'Review proposal');

    expect(tasks.addedLink, 'https://example.com/reference');
  });

  testWidgets('event uses next full working hour from simulated time', (
    tester,
  ) async {
    await pumpForm(
      tester,
      form: const EventInspectorForm(listId: 'list'),
      list: TaskList(
        id: 'list',
        name: 'Work',
        simulatedTime: simulated,
        workStartMinutes: 10 * 60,
        workEndMinutes: 17 * 60,
      ),
    );

    expect(find.text('Mon, Oct 5 • 10:00 AM'), findsOneWidget);
    expect(find.text('Mon, Oct 5 • 11:00 AM'), findsOneWidget);
  });

  testWidgets('event defaults to next 9 AM without configured workdays', (
    tester,
  ) async {
    await pumpForm(
      tester,
      form: const EventInspectorForm(listId: 'list'),
      list: TaskList(
        id: 'list',
        name: 'Work',
        simulatedTime: simulated,
        workDays: [],
      ),
    );

    expect(find.text('Sat, Oct 3 • 9:00 AM'), findsOneWidget);
    expect(find.text('Sat, Oct 3 • 10:00 AM'), findsOneWidget);
    expect(
      tester.widget<FilledButton>(find.byType(FilledButton)).onPressed,
      isNotNull,
    );
  });

  testWidgets('event defaults to today at 9 AM before fallback time', (
    tester,
  ) async {
    await pumpForm(
      tester,
      form: const EventInspectorForm(listId: 'list'),
      list: TaskList(
        id: 'list',
        name: 'Work',
        simulatedTime: DateTime(2026, 10, 2, 8, 15),
        workStartMinutes: 9 * 60,
        workEndMinutes: 9 * 60 + 30,
      ),
    );

    expect(find.text('Fri, Oct 2 • 9:00 AM'), findsOneWidget);
    expect(find.text('Fri, Oct 2 • 10:00 AM'), findsOneWidget);
  });
}
