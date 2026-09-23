import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/fixed_event.dart';
import '../models/task_item.dart';
import '../models/task_list.dart';
import '../models/user_profile.dart';
import 'api_schedule_repository.dart';
import 'task_repository.dart';

/// .NET `DayOfWeek` enum order (Sunday = 0 ... Saturday = 6), used to convert
/// to/from Dart's `DateTime.weekday` order (Monday = 1 ... Sunday = 7).
const List<String> _dotNetDayNames = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
];

String _dartWeekdayToDotNetDayName(int dartWeekday) =>
    _dotNetDayNames[dartWeekday % 7];

int _dotNetDayNameToDartWeekday(String name) {
  final index = _dotNetDayNames.indexOf(name);
  return index == 0 ? 7 : index;
}

const List<String> _sortOptionNames = [
  'Default',
  'Alphabetical',
  'DueDate',
  'Id',
];

const List<String> _schedulingModeNames = [
  'GoldPanning',
  'ConstraintOptimization',
];

/// Formats minutes-since-midnight as .NET's default `TimeOnly` JSON shape (`HH:mm:ss`).
String _formatMinutesAsTimeOnly(int minutesSinceMidnight) {
  final hours = minutesSinceMidnight ~/ 60;
  final minutes = minutesSinceMidnight % 60;
  return '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}:00';
}

int _parseTimeOnlyAsMinutes(String timeOnly) {
  final parts = timeOnly.split(':');
  return int.parse(parts[0]) * 60 + int.parse(parts[1]);
}

/// Formats a minutes duration as .NET's default `TimeSpan` JSON shape (`hh:mm:ss`).
String _formatMinutesAsDuration(int minutes) {
  final hours = minutes ~/ 60;
  final remainderMinutes = minutes % 60;
  return '${hours.toString().padLeft(2, '0')}:${remainderMinutes.toString().padLeft(2, '0')}:00';
}

int _parseDurationAsMinutes(String duration) {
  final parts = duration.split(':');
  return int.parse(parts[0]) * 60 + int.parse(parts[1]);
}

/// [TaskRepository] implementation that calls an already-running
/// `PriorityTaskManager.API` instance's authenticated `/api/tasks`,
/// `/api/lists`, `/api/events`, and `/api/profile` REST endpoints. Only used
/// for Authenticated sessions (see `lib/providers/task_providers.dart`);
/// Guests use [LocalTaskRepository] (`lib/data/local_task_repository.dart`).
///
/// Known limitation (see issue #44): the server's `Event` model
/// (`PriorityTaskManager/Models/Event.cs`) is not list-scoped, unlike the
/// local `FixedEvent`/`TaskList` relationship. [getEvents] therefore returns
/// every event on the account regardless of [String] `listId`, and
/// [addEvent]/[updateEvent] do not persist a list association server-side.
class ApiTaskRepository implements TaskRepository {
  ApiTaskRepository({
    Uri? baseUri,
    http.Client? httpClient,
    required this.authToken,
  }) : baseUri = baseUri ?? ApiScheduleRepository.defaultBaseUri,
       _httpClient = httpClient ?? http.Client();

  final Uri baseUri;
  final http.Client _httpClient;
  final String authToken;

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $authToken',
  };

  Future<http.Response> _send(
    String method,
    String path, {
    Object? body,
  }) async {
    final uri = baseUri.resolve(path);
    http.Response response;
    try {
      switch (method) {
        case 'GET':
          response = await _httpClient.get(uri, headers: _headers);
          break;
        case 'POST':
          response = await _httpClient.post(
            uri,
            headers: _headers,
            body: body == null ? null : jsonEncode(body),
          );
          break;
        case 'PUT':
          response = await _httpClient.put(
            uri,
            headers: _headers,
            body: body == null ? null : jsonEncode(body),
          );
          break;
        case 'DELETE':
          response = await _httpClient.delete(uri, headers: _headers);
          break;
        default:
          throw ArgumentError('Unsupported method: $method');
      }
    } catch (error) {
      throw StateError(
        'Could not reach the API at $baseUri. Make sure PriorityTaskManager.API '
        'is running (see docs/WORKFLOW.md). Underlying error: $error',
      );
    }

    if (response.statusCode >= 400) {
      String message = 'Request to $path failed (${response.statusCode}).';
      try {
        final json = jsonDecode(response.body) as Map<String, dynamic>;
        if (json['error'] is String) {
          message = json['error'] as String;
        }
      } catch (_) {
        // Non-JSON error body; fall back to the generic message above.
      }
      throw StateError(message);
    }

    return response;
  }

  // ---- Lists ----

  @override
  Future<List<TaskList>> getLists() async {
    final response = await _send('GET', '/api/lists/');
    final json = jsonDecode(response.body) as List<dynamic>;
    return json.map((e) => _listFromJson(e as Map<String, dynamic>)).toList();
  }

  @override
  Future<TaskList> createList({
    required String name,
    String? description,
  }) async {
    final response = await _send(
      'POST',
      '/api/lists/',
      body: _listRequestJson(name: name, description: description),
    );
    return _listFromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  @override
  Future<void> updateList(TaskList list) async {
    await _send(
      'PUT',
      '/api/lists/${list.id}',
      body: _listRequestJson(
        name: list.name,
        description: list.description,
        sortOption: list.sortOption,
        schedulingMode: list.schedulingMode,
        workStartMinutes: list.workStartMinutes,
        workEndMinutes: list.workEndMinutes,
        workDays: list.workDays,
        slackThresholdDire: list.slackThresholdDire,
        slackThresholdPressing: list.slackThresholdPressing,
        slackThresholdFocus: list.slackThresholdFocus,
        slackThresholdSafe: list.slackThresholdSafe,
        simulatedTime: list.simulatedTime,
      ),
    );
  }

  @override
  Future<void> deleteList(String listId) async {
    await _send('DELETE', '/api/lists/$listId');
  }

  Map<String, dynamic> _listRequestJson({
    required String name,
    String? description,
    int? sortOption,
    int? schedulingMode,
    int? workStartMinutes,
    int? workEndMinutes,
    List<int>? workDays,
    double? slackThresholdDire,
    double? slackThresholdPressing,
    double? slackThresholdFocus,
    double? slackThresholdSafe,
    DateTime? simulatedTime,
  }) => {
    'name': name,
    'description': description,
    'sortOption': sortOption == null ? null : _sortOptionNames[sortOption],
    'schedulingMode': schedulingMode == null
        ? null
        : _schedulingModeNames[schedulingMode],
    'workStartTime': workStartMinutes == null
        ? null
        : _formatMinutesAsTimeOnly(workStartMinutes),
    'workEndTime': workEndMinutes == null
        ? null
        : _formatMinutesAsTimeOnly(workEndMinutes),
    'workDays': workDays?.map(_dartWeekdayToDotNetDayName).toList(),
    'slackThresholdDire': slackThresholdDire,
    'slackThresholdPressing': slackThresholdPressing,
    'slackThresholdFocus': slackThresholdFocus,
    'slackThresholdSafe': slackThresholdSafe,
    'simulatedTime': simulatedTime?.toIso8601String(),
  };

  TaskList _listFromJson(Map<String, dynamic> json) => TaskList(
    id: json['id'] as String,
    name: json['name'] as String,
    description: json['description'] as String?,
    sortOption: json['sortOption'] == null
        ? null
        : _sortOptionNames.indexOf(json['sortOption'] as String),
    schedulingMode: json['schedulingMode'] == null
        ? null
        : _schedulingModeNames.indexOf(json['schedulingMode'] as String),
    workStartMinutes: json['workStartTime'] == null
        ? null
        : _parseTimeOnlyAsMinutes(json['workStartTime'] as String),
    workEndMinutes: json['workEndTime'] == null
        ? null
        : _parseTimeOnlyAsMinutes(json['workEndTime'] as String),
    workDays: (json['workDays'] as List<dynamic>?)
        ?.map((d) => _dotNetDayNameToDartWeekday(d as String))
        .toList(),
    slackThresholdDire: (json['slackThresholdDire'] as num?)?.toDouble(),
    slackThresholdPressing: (json['slackThresholdPressing'] as num?)
        ?.toDouble(),
    slackThresholdFocus: (json['slackThresholdFocus'] as num?)?.toDouble(),
    slackThresholdSafe: (json['slackThresholdSafe'] as num?)?.toDouble(),
    simulatedTime: json['simulatedTime'] == null
        ? null
        : DateTime.parse(json['simulatedTime'] as String),
  );

  // ---- Tasks ----

  @override
  Future<List<TaskItem>> getTasks(String listId) async {
    final response = await _send('GET', '/api/tasks/');
    final json = jsonDecode(response.body) as List<dynamic>;
    return json
        .map((e) => _taskFromJson(e as Map<String, dynamic>))
        .where((task) => task.listId == listId)
        .toList();
  }

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
    bool isDivisible = true,
  }) async {
    final response = await _send(
      'POST',
      '/api/tasks/',
      body: _taskRequestJson(
        listId: listId,
        title: title,
        description: description,
        dueDate: dueDate,
        estimatedDurationMinutes: estimatedDurationMinutes,
        dependencies: dependencies,
        importance: importance,
        complexity: complexity,
        notBefore: notBefore,
        isPinned: isPinned,
        isDivisible: isDivisible,
      ),
    );
    return _taskFromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  @override
  Future<void> updateTask(TaskItem task) async {
    await _send(
      'PUT',
      '/api/tasks/${task.id}',
      body: _taskRequestJson(
        listId: task.listId,
        title: task.title,
        description: task.description,
        dueDate: task.dueDate,
        estimatedDurationMinutes: task.estimatedDurationMinutes,
        dependencies: task.dependencies,
        importance: task.importance,
        complexity: task.complexity,
        notBefore: task.notBefore,
        isPinned: task.isPinned,
        isDivisible: task.isDivisible,
      ),
    );
  }

  @override
  Future<void> deleteTask(String taskId) async {
    await _send('DELETE', '/api/tasks/$taskId');
  }

  @override
  Future<void> setCompleted(String taskId, bool isCompleted) async {
    await _send(
      'POST',
      '/api/tasks/$taskId/${isCompleted ? 'complete' : 'uncomplete'}',
    );
  }

  @override
  Future<void> addDependency(String taskId, String dependsOnTaskId) async {
    if (taskId == dependsOnTaskId) return;
    final task = await _getTask(taskId);
    if (task.dependencies.contains(dependsOnTaskId)) return;
    task.dependencies = List<String>.from(task.dependencies)
      ..add(dependsOnTaskId);
    await updateTask(task);
  }

  @override
  Future<void> removeDependency(String taskId, String dependsOnTaskId) async {
    final task = await _getTask(taskId);
    task.dependencies = List<String>.from(task.dependencies)
      ..remove(dependsOnTaskId);
    await updateTask(task);
  }

  Future<TaskItem> _getTask(String taskId) async {
    final response = await _send('GET', '/api/tasks/$taskId');
    return _taskFromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Map<String, dynamic> _taskRequestJson({
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
    bool isDivisible = true,
  }) => {
    'title': title,
    'description': description,
    'listId': listId,
    'importance': importance,
    'dueDate': dueDate?.toIso8601String(),
    'notBefore': notBefore?.toIso8601String(),
    'estimatedDuration': _formatMinutesAsDuration(estimatedDurationMinutes),
    'dependencies': dependencies ?? <String>[],
    'isPinned': isPinned,
    'complexity': complexity,
    'points': 0,
    'beforePadding': null,
    'afterPadding': null,
    'isDivisible': isDivisible,
  };

  TaskItem _taskFromJson(Map<String, dynamic> json) => TaskItem(
    id: json['id'] as String,
    listId: json['listId'] as String,
    title: json['title'] as String? ?? '',
    description: json['description'] as String? ?? '',
    isCompleted: json['isCompleted'] as bool,
    dueDate: json['dueDate'] == null
        ? null
        : DateTime.parse(json['dueDate'] as String),
    estimatedDurationMinutes: _parseDurationAsMinutes(
      json['estimatedDuration'] as String,
    ),
    dependencies: (json['dependencies'] as List<dynamic>)
        .map((d) => d as String)
        .toList(),
    importance: json['importance'] as int,
    complexity: json['complexity'] as int,
    notBefore: json['notBefore'] == null
        ? null
        : DateTime.parse(json['notBefore'] as String),
    isPinned: json['isPinned'] as bool,
    isDivisible: json['isDivisible'] as bool,
  );

  // ---- Profile ----

  @override
  Future<UserProfile> getProfile() async {
    final response = await _send('GET', '/api/profile/');
    return _profileFromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  @override
  Future<void> updateProfile(UserProfile profile) async {
    await _send(
      'PUT',
      '/api/profile/',
      body: {
        'defaultListSortOption':
            _sortOptionNames[profile.defaultListSortOption],
        'desiredBreatherDuration': _formatMinutesAsDuration(
          profile.desiredBreatherMinutes,
        ),
        'workStartTime': _formatMinutesAsTimeOnly(profile.workStartMinutes),
        'workEndTime': _formatMinutesAsTimeOnly(profile.workEndMinutes),
        'workDays': profile.workDays.map(_dartWeekdayToDotNetDayName).toList(),
        'schedulingMode': _schedulingModeNames[profile.schedulingMode],
        'slackThresholdDire': profile.slackThresholdDire,
        'slackThresholdPressing': profile.slackThresholdPressing,
        'slackThresholdFocus': profile.slackThresholdFocus,
        'slackThresholdSafe': profile.slackThresholdSafe,
      },
    );
  }

  UserProfile _profileFromJson(Map<String, dynamic> json) => UserProfile(
    defaultListSortOption: _sortOptionNames.indexOf(
      json['defaultListSortOption'] as String,
    ),
    workStartMinutes: _parseTimeOnlyAsMinutes(json['workStartTime'] as String),
    workEndMinutes: _parseTimeOnlyAsMinutes(json['workEndTime'] as String),
    workDays: (json['workDays'] as List<dynamic>)
        .map((d) => _dotNetDayNameToDartWeekday(d as String))
        .toList(),
    schedulingMode: _schedulingModeNames.indexOf(
      json['schedulingMode'] as String,
    ),
    desiredBreatherMinutes: _parseDurationAsMinutes(
      json['desiredBreatherDuration'] as String,
    ),
    slackThresholdDire: (json['slackThresholdDire'] as num).toDouble(),
    slackThresholdPressing: (json['slackThresholdPressing'] as num).toDouble(),
    slackThresholdFocus: (json['slackThresholdFocus'] as num).toDouble(),
    slackThresholdSafe: (json['slackThresholdSafe'] as num).toDouble(),
  );

  // ---- Events ----
  //
  // Known limitation (see class doc comment above): server events are not
  // list-scoped, so [getEvents] returns every account event for any listId,
  // and the [listId] passed to [addEvent] is not sent to the server.

  @override
  Future<List<FixedEvent>> getEvents(String listId) async {
    final response = await _send('GET', '/api/events/');
    final json = jsonDecode(response.body) as List<dynamic>;
    return json
        .map((e) => _eventFromJson(e as Map<String, dynamic>, listId))
        .toList();
  }

  @override
  Future<FixedEvent> addEvent({
    required String listId,
    required String title,
    required DateTime startTime,
    required DateTime endTime,
  }) async {
    final response = await _send(
      'POST',
      '/api/events/',
      body: {
        'name': title,
        'startTime': startTime.toIso8601String(),
        'endTime': endTime.toIso8601String(),
      },
    );
    return _eventFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
      listId,
    );
  }

  @override
  Future<void> updateEvent(FixedEvent event) async {
    await _send(
      'PUT',
      '/api/events/${event.id}',
      body: {
        'name': event.title,
        'startTime': event.startTime.toIso8601String(),
        'endTime': event.endTime.toIso8601String(),
      },
    );
  }

  @override
  Future<void> deleteEvent(String eventId) async {
    await _send('DELETE', '/api/events/$eventId');
  }

  FixedEvent _eventFromJson(Map<String, dynamic> json, String listId) =>
      FixedEvent(
        id: json['id'] as String,
        listId: listId,
        title: json['name'] as String? ?? '',
        startTime: DateTime.parse(json['startTime'] as String),
        endTime: DateTime.parse(json['endTime'] as String),
      );
}
