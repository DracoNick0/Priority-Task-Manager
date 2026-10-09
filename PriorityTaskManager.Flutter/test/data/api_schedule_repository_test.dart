import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:priority_task_manager/data/api_schedule_repository.dart';
import 'package:priority_task_manager/models/effective_settings.dart';
import 'package:priority_task_manager/models/fixed_event.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/models/recurring_schedule_task.dart';
import 'package:priority_task_manager/models/task_item.dart';

void main() {
  group('ApiScheduleRepository', () {
    test('preserves every scheduled part as a separate time block', () async {
      final repository = ApiScheduleRepository(
        authToken: 'test-token',
        httpClient: MockClient(
          (_) async => http.Response(
            jsonEncode({
              'scheduledTasks': [
                {
                  'id': 'task-1',
                  'title': 'Prepare presentation',
                  'seriesId': 'series-1',
                  'occurrenceDate': '2026-10-20T00:00:00',
                  'listId': 'list-1',
                  'description': 'Prepare notes',
                  'link': '',
                  'dueDate': null,
                  'estimatedDuration': '01:00:00',
                  'importance': 5,
                  'complexity': 1,
                  'effectiveImportance': 5,
                  'occurrenceStatus': 'Pending',
                  'completionCount': 1,
                  'requiredCompletions': 2,
                  'showMissedIndicator': true,
                  'hasMissedOccurrence': false,
                  'isMissed': false,
                  'trackStreak': true,
                  'currentStreak': 3,
                  'bestStreak': 5,
                  'progressionMode': 'RollForwardKeepBacklog',
                  'scheduledParts': [
                    {
                      'startTime': '2026-10-08T09:00:00',
                      'endTime': '2026-10-08T11:00:00',
                    },
                    {
                      'startTime': '2026-10-08T14:00:00',
                      'endTime': '2026-10-08T15:00:00',
                    },
                    {
                      'startTime': '2026-10-09T09:00:00',
                      'endTime': '2026-10-09T10:00:00',
                    },
                  ],
                },
              ],
              'unscheduledTaskIds': [],
              'leastSlackTaskTitle': 'None',
              'leastSlackRealisticMinutes': null,
              'leastSlackActualMinutes': null,
            }),
            200,
          ),
        ),
      );

      final schedule = await repository.computeSchedule(
        tasks: [TaskItem(id: 'task-1', listId: 'list-1', title: 'Task')],
        settings: EffectiveListSettings(
          sortOption: 0,
          schedulingMode: 0,
          workStartMinutes: 9 * 60,
          workEndMinutes: 17 * 60,
          workDays: const [1, 2, 3, 4, 5],
          desiredBreatherMinutes: 15,
          slackThresholdDire: 0.5,
          slackThresholdPressing: 1,
          slackThresholdFocus: 2,
          slackThresholdSafe: 4,
        ),
        now: DateTime(2026, 10, 8, 8),
      );

      expect(schedule.todayTasks, hasLength(2));
      expect(schedule.todayTasks[0].startTime, DateTime(2026, 10, 8, 9));
      expect(schedule.todayTasks[0].endTime, DateTime(2026, 10, 8, 11));
      expect(schedule.todayTasks[0].chunkHours, 2);
      expect(schedule.todayTasks[1].startTime, DateTime(2026, 10, 8, 14));
      expect(schedule.todayTasks[1].endTime, DateTime(2026, 10, 8, 15));
      expect(schedule.todayTasks[1].chunkHours, 1);
      expect(schedule.futureTasks, hasLength(1));
      expect(schedule.futureTasks.single.startTime, DateTime(2026, 10, 9, 9));
      expect(schedule.futureTasks.single.endTime, DateTime(2026, 10, 9, 10));
      expect(schedule.futureTasks.single.isFuture, isTrue);
      final occurrence = schedule.scheduledOccurrences.single;
      expect(occurrence.id, 'task-1');
      expect(occurrence.occurrenceDate, DateTime(2026, 10, 20));
      expect(occurrence.completionCount, 1);
      expect(occurrence.requiredCompletions, 2);
      expect(occurrence.currentStreak, 3);
      expect(occurrence.isRecurringOccurrence, isTrue);
    });

    test('signs out when the scheduling API rejects the session', () async {
      var signedOut = false;
      final repository = ApiScheduleRepository(
        authToken: 'test-token',
        httpClient: MockClient((_) async => http.Response('', 401)),
        onUnauthorized: () async {
          signedOut = true;
        },
      );

      await expectLater(
        repository.computeSchedule(
          tasks: [TaskItem(id: 'task-1', listId: 'list-1', title: 'Task')],
          settings: EffectiveListSettings(
            sortOption: 0,
            schedulingMode: 0,
            workStartMinutes: 9 * 60,
            workEndMinutes: 17 * 60,
            workDays: const [1, 2, 3, 4, 5],
            desiredBreatherMinutes: 15,
            slackThresholdDire: 0.5,
            slackThresholdPressing: 1,
            slackThresholdFocus: 2,
            slackThresholdSafe: 4,
          ),
        ),
        throwsStateError,
      );

      expect(signedOut, isTrue);
    });

    test(
      'sends recurring series and occurrence state for horizon expansion',
      () async {
        Map<String, dynamic>? requestBody;
        final repository = ApiScheduleRepository(
          authToken: 'test-token',
          httpClient: MockClient((request) async {
            requestBody = jsonDecode(request.body) as Map<String, dynamic>;
            return http.Response(
              jsonEncode({'scheduledTasks': [], 'unscheduledTaskIds': []}),
              200,
            );
          }),
        );

        await repository.computeSchedule(
          tasks: const [],
          recurringTasks: [
            RecurringScheduleTask(
              id: 'series-1',
              title: 'Review notes',
              description: '',
              listId: 'list-1',
              importance: 5,
              complexity: 1,
              points: 0,
              estimatedDuration: '01:30:00',
              isPinned: false,
              beforePadding: null,
              afterPadding: null,
              isDivisible: false,
              recurrenceRule: DailyIntervalRecurrenceRule(
                seriesStartDate: DateTime(2026, 10, 8),
                endCondition: NeverEndCondition(),
              ),
              seriesId: 'series-1',
              progressionMode: 'RollForwardKeepBacklog',
              requiredCompletions: 2,
              occurrenceStates: [
                RecurringScheduleOccurrenceState(
                  scheduledDate: DateTime(2026, 10, 8),
                  status: 'Missed',
                  completionCount: 1,
                  completedAt: null,
                ),
              ],
            ),
          ],
          settings: EffectiveListSettings(
            sortOption: 0,
            schedulingMode: 0,
            workStartMinutes: 9 * 60,
            workEndMinutes: 17 * 60,
            workDays: const [1, 2, 3, 4, 5],
            desiredBreatherMinutes: 15,
            slackThresholdDire: 0.5,
            slackThresholdPressing: 1,
            slackThresholdFocus: 2,
            slackThresholdSafe: 4,
          ),
          now: DateTime(2026, 10, 9, 8),
        );

        final taskJson =
            (requestBody!['tasks'] as List<dynamic>).single
                as Map<String, dynamic>;
        expect(taskJson['recurrenceRule']['type'], 'dailyInterval');
        expect(taskJson['progressionMode'], 'RollForwardKeepBacklog');
        expect(taskJson['requiredCompletions'], 2);
        expect(taskJson['occurrenceStates'][0]['status'], 'Missed');
        expect(taskJson['occurrenceStates'][0]['completionCount'], 1);
      },
    );

    test('sends recurring event rules for horizon expansion', () async {
      Map<String, dynamic>? requestBody;
      final repository = ApiScheduleRepository(
        authToken: 'test-token',
        httpClient: MockClient((request) async {
          requestBody = jsonDecode(request.body) as Map<String, dynamic>;
          return http.Response(
            jsonEncode({'scheduledTasks': [], 'unscheduledTaskIds': []}),
            200,
          );
        }),
      );
      final firstEventRule = DailyIntervalRecurrenceRule(
        seriesStartDate: DateTime(2026, 10, 8),
        endCondition: NeverEndCondition(),
      );
      final secondEventRule = DailyIntervalRecurrenceRule(
        seriesStartDate: DateTime(2026, 10, 8),
        endCondition: NeverEndCondition(),
      );

      await repository.computeSchedule(
        tasks: [TaskItem(id: 'task-1', listId: 'list-1', title: 'Task')],
        events: [
          FixedEvent(
            id: 'event-1',
            listId: 'list-1',
            title: 'Morning meeting',
            startTime: DateTime(2026, 10, 8, 9),
            endTime: DateTime(2026, 10, 8, 10, 30),
            recurrenceRule: firstEventRule.toJson(),
          ),
          FixedEvent(
            id: 'event-2',
            listId: 'list-1',
            title: 'Afternoon meeting',
            startTime: DateTime(2026, 10, 8, 12, 30),
            endTime: DateTime(2026, 10, 8, 14),
            recurrenceRule: secondEventRule.toJson(),
          ),
          FixedEvent(
            id: 'event-3',
            listId: 'list-1',
            title: 'One-off event',
            startTime: DateTime(2026, 10, 8, 15),
            endTime: DateTime(2026, 10, 8, 16),
          ),
        ],
        settings: EffectiveListSettings(
          sortOption: 0,
          schedulingMode: 0,
          workStartMinutes: 9 * 60,
          workEndMinutes: 17 * 60,
          workDays: const [1, 2, 3, 4, 5],
          desiredBreatherMinutes: 15,
          slackThresholdDire: 0.5,
          slackThresholdPressing: 1,
          slackThresholdFocus: 2,
          slackThresholdSafe: 4,
        ),
        now: DateTime(2026, 10, 8, 8),
      );

      final eventJson = (requestBody!['events'] as List<dynamic>)
          .cast<Map<String, dynamic>>();
      expect(eventJson[0]['recurrenceRule'], firstEventRule.toJson());
      expect(eventJson[1]['recurrenceRule'], secondEventRule.toJson());
      expect(eventJson[2].containsKey('recurrenceRule'), isFalse);
    });
  });
}
