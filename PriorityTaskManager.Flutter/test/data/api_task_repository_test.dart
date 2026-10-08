// Unit tests for ApiTaskRepository's request/response JSON mapping, using
// http.MockClient (from package:http/testing.dart, no extra dependency)
// instead of a real PriorityTaskManager.API instance.

import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:priority_task_manager/data/api_task_repository.dart';
import 'package:priority_task_manager/models/archive_group.dart';
import 'package:priority_task_manager/data/task_repository.dart';
import 'package:priority_task_manager/models/fixed_event.dart';
import 'package:priority_task_manager/models/recurrence_rule.dart';
import 'package:priority_task_manager/models/user_profile.dart';

void main() {
  group('ApiTaskRepository', () {
    test(
      'getLists parses enum names and TimeOnly strings from the response',
      () async {
        final client = MockClient((request) async {
          expect(request.url.path, '/api/lists/');
          expect(request.headers['Authorization'], 'Bearer test-token');
          return http.Response(
            jsonEncode([
              {
                'id': 'list-1',
                'name': 'Work',
                'description': null,
                'sortOption': 'DueDate',
                'schedulingMode': 'ConstraintOptimization',
                'workStartTime': '08:30:00',
                'workEndTime': '16:00:00',
                'workDays': ['Monday', 'Wednesday'],
                'slackThresholdDire': 0.25,
                'slackThresholdPressing': 0.75,
                'slackThresholdFocus': 2.0,
                'slackThresholdSafe': 4.0,
                'simulatedTime': null,
              },
            ]),
            200,
          );
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        final lists = await repository.getLists();

        expect(lists, hasLength(1));
        final list = lists.single;
        expect(list.id, 'list-1');
        expect(list.sortOption, 2); // DueDate index
        expect(list.schedulingMode, 1); // ConstraintOptimization index
        expect(list.workStartMinutes, 8 * 60 + 30);
        expect(list.workEndMinutes, 16 * 60);
        expect(list.workDays, [1, 3]); // Monday, Wednesday
      },
    );

    test(
      'addTask sends duration as hh:mm:ss and parses the created task back',
      () async {
        String? sentBody;
        final client = MockClient((request) async {
          sentBody = request.body;
          return http.Response(
            jsonEncode({
              'id': 'task-1',
              'listId': 'list-1',
              'title': 'Write report',
              'description': '',
              'link': 'https://example.com/report',
              'isCompleted': false,
              'dueDate': null,
              'estimatedDuration': '01:30:00',
              'dependencies': <String>[],
              'importance': 5,
              'complexity': 1,
              'notBefore': null,
              'isPinned': false,
              'isDivisible': true,
            }),
            201,
          );
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        final task = await repository.addTask(
          listId: 'list-1',
          title: 'Write report',
          link: 'https://example.com/report',
          estimatedDurationMinutes: 90,
        );

        final requestJson = jsonDecode(sentBody!) as Map<String, dynamic>;
        expect(requestJson['estimatedDuration'], '01:30:00');
        expect(requestJson['link'], 'https://example.com/report');
        expect(task.estimatedDurationMinutes, 90);
        expect(task.title, 'Write report');
        expect(task.link, 'https://example.com/report');
      },
    );

    test(
      'addDependency fetches the task, mutates dependencies, and PUTs the full task',
      () async {
        final requests = <http.Request>[];
        final client = MockClient((request) async {
          requests.add(request);
          if (request.method == 'GET') {
            return http.Response(
              jsonEncode({
                'id': 'task-1',
                'listId': 'list-1',
                'title': 'A',
                'description': '',
                'link': 'https://example.com/task',
                'isCompleted': false,
                'dueDate': null,
                'estimatedDuration': '01:00:00',
                'dependencies': <String>[],
                'importance': 5,
                'complexity': 1,
                'notBefore': null,
                'isPinned': false,
                'isDivisible': true,
              }),
              200,
            );
          }
          return http.Response(request.body, 200);
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        await repository.addDependency('task-1', 'task-2');

        final putRequest = requests.firstWhere((r) => r.method == 'PUT');
        final body = jsonDecode(putRequest.body) as Map<String, dynamic>;
        expect(body['dependencies'], ['task-2']);
        expect(body['link'], 'https://example.com/task');
      },
    );

    test(
      'getProfile/updateProfile round-trip enum names and TimeOnly strings',
      () async {
        Map<String, dynamic>? putBody;
        final client = MockClient((request) async {
          if (request.method == 'PUT') {
            putBody = jsonDecode(request.body) as Map<String, dynamic>;
            return http.Response(request.body, 200);
          }
          return http.Response(
            jsonEncode({
              'defaultListSortOption': 'Alphabetical',
              'desiredBreatherDuration': '00:20:00',
              'workStartTime': '09:00:00',
              'workEndTime': '17:00:00',
              'workDays': ['Tuesday', 'Thursday'],
              'schedulingMode': 'GoldPanning',
              'slackThresholdDire': 0.5,
              'slackThresholdPressing': 1.0,
              'slackThresholdFocus': 3.0,
              'slackThresholdSafe': 5.0,
            }),
            200,
          );
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        final profile = await repository.getProfile();
        expect(profile.defaultListSortOption, 1); // Alphabetical index
        expect(profile.desiredBreatherMinutes, 20);
        expect(profile.workDays, [2, 4]); // Tuesday, Thursday

        await repository.updateProfile(
          UserProfile(defaultListSortOption: 2, workDays: [1, 2]),
        );
        expect(putBody!['defaultListSortOption'], 'DueDate');
        expect(putBody!['workDays'], ['Monday', 'Tuesday']);
      },
    );

    test(
      'getEvents ignores server list scoping and returns every account event for any listId',
      () async {
        final client = MockClient((request) async {
          expect(request.url.path, '/api/events/');
          return http.Response(
            jsonEncode([
              {
                'id': 'event-1',
                'name': 'Standup',
                'startTime': '2026-09-14T09:00:00.000',
                'endTime': '2026-09-14T09:15:00.000',
              },
            ]),
            200,
          );
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        final events = await repository.getEvents('any-list-id');

        expect(events, hasLength(1));
        expect(events.single.listId, 'any-list-id');
      },
    );

    test(
      'getEvents parses a recurring event\'s recurrenceRule/seriesId',
      () async {
        final client = MockClient((request) async {
          return http.Response(
            jsonEncode([
              {
                'id': 'event-1',
                'name': 'Standup',
                'startTime': '2026-09-14T09:00:00.000',
                'endTime': '2026-09-14T09:15:00.000',
                'recurrenceRule': {
                  'type': 'weekly',
                  'seriesStartDate': '2026-09-14T00:00:00.000',
                  'daysOfWeek': ['Monday'],
                  'intervalWeeks': 1,
                },
                'seriesId': 'event-1',
              },
            ]),
            200,
          );
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        final events = await repository.getEvents('any-list-id');

        expect(events.single.recurrenceRule, isNotNull);
        expect(events.single.recurrenceRule!['type'], 'weekly');
        expect(events.single.seriesId, 'event-1');
      },
    );

    test(
      'updateEvent round-trips recurrenceRule instead of dropping it',
      () async {
        Map<String, dynamic>? putBody;
        final client = MockClient((request) async {
          if (request.method == 'PUT') {
            putBody = jsonDecode(request.body) as Map<String, dynamic>;
            return http.Response('', 204);
          }
          return http.Response('', 404);
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );
        final recurringEvent = FixedEvent(
          id: 'event-1',
          listId: 'list-1',
          title: 'Standup',
          startTime: DateTime(2026, 9, 14, 9),
          endTime: DateTime(2026, 9, 14, 9, 15),
          recurrenceRule: {'type': 'weekly'},
          seriesId: 'event-1',
        );

        await repository.updateEvent(recurringEvent);

        expect(putBody!['recurrenceRule'], {'type': 'weekly'});
      },
    );

    test('event create and update round-trip description and link', () async {
      final requests = <http.Request>[];
      final client = MockClient((request) async {
        requests.add(request);
        if (request.method == 'POST') {
          return http.Response(
            jsonEncode({
              'id': 'event-1',
              'name': 'Appointment',
              'description': 'Annual checkup',
              'link': 'https://example.com',
              'startTime': '2026-09-14T09:00:00',
              'endTime': '2026-09-14T10:00:00',
              'recurrenceRule': null,
              'seriesId': null,
            }),
            201,
          );
        }
        return http.Response('', 204);
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      final event = await repository.addEvent(
        listId: 'list-1',
        title: 'Appointment',
        description: 'Annual checkup',
        link: 'https://example.com',
        startTime: DateTime(2026, 9, 14, 9),
        endTime: DateTime(2026, 9, 14, 10),
      );
      await repository.updateEvent(event);

      expect(event.description, 'Annual checkup');
      expect(event.link, 'https://example.com');
      for (final request in requests) {
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['description'], 'Annual checkup');
        expect(body['link'], 'https://example.com');
      }
    });

    test(
      'editOccurrence PUTs to the occurrence endpoint with the target query param',
      () async {
        Uri? requestUri;
        Map<String, dynamic>? putBody;
        final client = MockClient((request) async {
          requestUri = request.url;
          putBody = jsonDecode(request.body) as Map<String, dynamic>;
          return http.Response('', 204);
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        await repository.editOccurrence(
          seriesId: 'series-1',
          occurrenceDate: DateTime(2026, 9, 14),
          name: 'Standup (moved)',
          startTime: DateTime(2026, 9, 14, 10),
          endTime: DateTime(2026, 9, 14, 10, 15),
          description: 'Agenda',
          link: 'https://example.com',
          target: RecurrenceEditTarget.thisOccurrence,
        );

        expect(requestUri!.path, '/api/events/series-1/occurrences/2026-09-14');
        expect(requestUri!.queryParameters['target'], 'ThisOccurrence');
        expect(putBody!['name'], 'Standup (moved)');
        expect(putBody!['description'], 'Agenda');
        expect(putBody!['link'], 'https://example.com');
      },
    );

    test('getEventOccurrences preserves original date after a move', () async {
      Uri? requestUri;
      final client = MockClient((request) async {
        requestUri = request.url;
        return http.Response(
          jsonEncode([
            {
              'id': 'series-1',
              'seriesId': 'series-1',
              'name': 'Moved',
              'startTime': '2026-09-15T10:00:00',
              'endTime': '2026-09-15T10:15:00',
              'originalOccurrenceDate': '2026-09-14T00:00:00',
            },
          ]),
          200,
        );
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      final events = await repository.getEventOccurrences(
        'list-1',
        DateTime(2026, 9, 15),
        DateTime(2026, 9, 29),
      );

      expect(requestUri!.path, '/api/events/occurrences');
      expect(requestUri!.queryParameters['from'], '2026-09-15');
      expect(requestUri!.queryParameters['to'], '2026-09-29');
      expect(events.single.id, 'series-1_2026-09-14');
      expect(events.single.originalOccurrenceDate, DateTime(2026, 9, 14));
      expect(events.single.startTime, DateTime(2026, 9, 15, 10));
    });

    test('editOccurrence sends a changed future recurrence rule', () async {
      Map<String, dynamic>? body;
      final client = MockClient((request) async {
        body = jsonDecode(request.body) as Map<String, dynamic>;
        return http.Response('', 204);
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      await repository.editOccurrence(
        seriesId: 'series-1',
        occurrenceDate: DateTime(2026, 9, 14),
        name: 'Changed',
        startTime: DateTime(2026, 9, 14, 10),
        endTime: DateTime(2026, 9, 14, 11),
        target: RecurrenceEditTarget.thisAndFollowing,
        recurrenceRule: WeeklyRecurrenceRule(
          seriesStartDate: DateTime(2026, 9, 14),
          endCondition: NeverEndCondition(),
          daysOfWeek: [1, 3],
        ),
      );

      expect((body!['recurrenceRule'] as Map<String, dynamic>)['daysOfWeek'], [
        1,
        3,
      ]);
    });

    test(
      'deleteOccurrence DELETEs to the occurrence endpoint with the target query param',
      () async {
        Uri? requestUri;
        final client = MockClient((request) async {
          requestUri = request.url;
          return http.Response('', 204);
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        await repository.deleteOccurrence(
          seriesId: 'series-1',
          occurrenceDate: DateTime(2026, 9, 14),
          target: RecurrenceEditTarget.thisAndFollowing,
        );

        expect(requestUri!.path, '/api/events/series-1/occurrences/2026-09-14');
        expect(requestUri!.queryParameters['target'], 'ThisAndFollowing');
      },
    );

    test(
      'a non-2xx response throws a StateError carrying the server error message',
      () async {
        final client = MockClient((request) async {
          return http.Response(
            jsonEncode({'error': 'Task title cannot be empty.'}),
            400,
          );
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        await expectLater(
          () => repository.addTask(listId: 'list-1', title: ''),
          throwsA(
            isA<StateError>().having(
              (e) => e.message,
              'message',
              'Task title cannot be empty.',
            ),
          ),
        );
      },
    );

    test('getArchivedTasks parses the archived task list', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/archive/');
        return http.Response(
          jsonEncode([
            {
              'id': 'task-1',
              'listId': 'list-1',
              'title': 'Old task',
              'description': '',
              'isCompleted': true,
              'dueDate': null,
              'estimatedDuration': '01:00:00',
              'dependencies': <String>[],
              'importance': 5,
              'complexity': 1,
              'notBefore': null,
              'isPinned': false,
              'isDivisible': true,
            },
          ]),
          200,
        );
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      final archivedTasks = await repository.getArchivedTasks();

      expect(archivedTasks, hasLength(1));
      expect(archivedTasks.single.title, 'Old task');
    });

    test(
      'restoreArchivedTask throws RestoreTargetListRequiredException on a 409',
      () async {
        final client = MockClient((request) async {
          expect(request.url.path, '/api/archive/task-1/restore');
          return http.Response(jsonEncode({'error': 'list required'}), 409);
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        await expectLater(
          () => repository.restoreArchivedTask('task-1'),
          throwsA(isA<RestoreTargetListRequiredException>()),
        );
      },
    );

    test('restoreArchivedTask returns the restored task on success', () async {
      final client = MockClient((request) async {
        expect(jsonDecode(request.body), {'targetListId': 'list-2'});
        return http.Response(
          jsonEncode({
            'id': 'task-1',
            'listId': 'list-2',
            'title': 'Old task',
            'description': '',
            'isCompleted': false,
            'dueDate': null,
            'estimatedDuration': '01:00:00',
            'dependencies': <String>[],
            'importance': 5,
            'complexity': 1,
            'notBefore': null,
            'isPinned': false,
            'isDivisible': true,
          }),
          200,
        );
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      final restored = await repository.restoreArchivedTask(
        'task-1',
        targetListId: 'list-2',
      );

      expect(restored.listId, 'list-2');
    });

    test('getArchivedEvents parses archived event entries', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/archive/events');
        return http.Response(
          jsonEncode([
            {
              'id': 'event-1',
              'name': 'Archived meeting',
              'startTime': '2026-10-01T09:00:00.000',
              'endTime': '2026-10-01T10:00:00.000',
              'recurrenceRule': null,
              'seriesId': null,
              'description': '',
              'link': null,
            },
          ]),
          200,
        );
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      final events = await repository.getArchivedEvents();

      expect(events, hasLength(1));
      expect(events.single.title, 'Archived meeting');
      expect(events.single.listId, '');
    });

    test(
      'archive event restore and permanent-delete use archive endpoints',
      () async {
        final requests = <http.Request>[];
        final client = MockClient((request) async {
          requests.add(request);
          return http.Response('', 204);
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );

        await repository.restoreArchivedEvent('event-1');
        await repository.deleteArchivedEvent('event-1');
        await repository.clearArchive();

        expect(requests.map((request) => request.method), [
          'POST',
          'DELETE',
          'DELETE',
        ]);
        expect(requests.map((request) => request.url.path), [
          '/api/archive/events/event-1/restore',
          '/api/archive/events/event-1',
          '/api/archive/',
        ]);
      },
    );

    test('archive groups parse their kind and item collections', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/archive/groups');
        return http.Response(
          jsonEncode([
            {
              'groupId': 'series-1',
              'kind': 'events',
              'tasks': <Object>[],
              'events': [
                {
                  'id': 'event-1',
                  'name': 'Recurring meeting',
                  'startTime': '2026-10-01T09:00:00.000',
                  'endTime': '2026-10-01T10:00:00.000',
                  'recurrenceRule': null,
                  'seriesId': null,
                  'description': '',
                  'link': null,
                },
                {
                  'id': 'event-2',
                  'name': 'Recurring meeting',
                  'startTime': '2026-10-08T09:00:00.000',
                  'endTime': '2026-10-08T10:00:00.000',
                  'recurrenceRule': null,
                  'seriesId': null,
                  'description': '',
                  'link': null,
                },
              ],
            },
          ]),
          200,
        );
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      final groups = await repository.getArchiveGroups();

      expect(groups, hasLength(1));
      expect(groups.single.kind, ArchiveGroupKind.events);
      expect(groups.single.groupId, 'series-1');
      expect(groups.single.events, hasLength(2));
    });

    test('archive task groups are submitted as one batch', () async {
      final client = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, '/api/tasks/archive/batch');
        expect(jsonDecode(request.body), {
          'taskIds': ['task-1', 'task-2'],
        });
        return http.Response('', 204);
      });
      final repository = ApiTaskRepository(
        authToken: 'test-token',
        httpClient: client,
      );

      await repository.archiveTaskGroup(['task-1', 'task-2']);
    });

    test(
      'restoring a task group reports when a target list is needed',
      () async {
        final client = MockClient((request) async {
          expect(request.url.path, '/api/archive/task-groups/group-1/restore');
          return http.Response(jsonEncode({'error': 'list required'}), 409);
        });
        final repository = ApiTaskRepository(
          authToken: 'test-token',
          httpClient: client,
        );
        const group = ArchiveGroup(
          groupId: 'group-1',
          kind: ArchiveGroupKind.tasks,
          tasks: [],
          events: [],
        );

        await expectLater(
          repository.restoreArchiveGroup(group),
          throwsA(isA<RestoreTargetListRequiredException>()),
        );
      },
    );
  });
}
