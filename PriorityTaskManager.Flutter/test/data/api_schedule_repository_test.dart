import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:priority_task_manager/data/api_schedule_repository.dart';
import 'package:priority_task_manager/models/effective_settings.dart';
import 'package:priority_task_manager/models/task_item.dart';

void main() {
  group('ApiScheduleRepository', () {
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
