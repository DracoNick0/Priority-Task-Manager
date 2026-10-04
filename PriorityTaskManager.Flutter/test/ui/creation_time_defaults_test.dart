import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/models/task_item.dart';
import 'package:priority_task_manager/models/task_list.dart';
import 'package:priority_task_manager/models/user_profile.dart';
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
  @override
  Future<List<TaskItem>> build(String arg) async => [];
}

class _Events extends EventsNotifier {
  @override
  Future<List<FixedEvent>> build(String arg) async => [];
}

void main() {
  final simulated = DateTime(2026, 10, 2, 16, 15);

  Future<void> pumpForm(
    WidgetTester tester, {
    required Widget form,
    required TaskList list,
    SessionStatus session = SessionStatus.authenticated,
  }) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          taskListsProvider.overrideWith(() => _Lists([list])),
          userProfileProvider.overrideWith(_Profile.new),
          sessionControllerProvider.overrideWith(() => _Session(session)),
          tasksProvider.overrideWith(_Tasks.new),
          eventsProvider.overrideWith(_Events.new),
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
