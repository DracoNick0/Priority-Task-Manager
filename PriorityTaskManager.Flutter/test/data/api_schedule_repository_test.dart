import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:priority_task_manager/data/api_schedule_repository.dart';
import 'package:priority_task_manager/models/effective_settings.dart';
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
                  'dueDate': null,
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
  });
}
