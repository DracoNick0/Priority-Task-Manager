import 'package:hive_ce/hive.dart';

part 'task_list.g.dart';

/// A named container for a collection of [TaskItem]s, stored locally via Hive.
@HiveType(typeId: 1)
class TaskList extends HiveObject {
  TaskList({
    required this.id,
    required this.name,
    this.description,
    this.sortOption,
    this.schedulingMode,
    this.workStartMinutes,
    this.workEndMinutes,
    this.workDays,
    this.slackThresholdDire,
    this.slackThresholdPressing,
    this.slackThresholdFocus,
    this.slackThresholdSafe,
  });

  @HiveField(0)
  final String id;

  @HiveField(1)
  String name;

  @HiveField(2)
  String? description;

  /// List-level override of `UserProfile.defaultListSortOption`'s enum
  /// index; null means "inherit the global default".
  @HiveField(3)
  int? sortOption;

  /// List-level override of `UserProfile.schedulingMode`'s enum index;
  /// null means "inherit the global default".
  @HiveField(4)
  int? schedulingMode;

  @HiveField(5)
  int? workStartMinutes;

  @HiveField(6)
  int? workEndMinutes;

  /// Workdays as Dart weekday ints (Monday = 1 ... Sunday = 7); null means
  /// "inherit the global default".
  @HiveField(7)
  List<int>? workDays;

  @HiveField(8)
  double? slackThresholdDire;

  @HiveField(9)
  double? slackThresholdPressing;

  @HiveField(10)
  double? slackThresholdFocus;

  @HiveField(11)
  double? slackThresholdSafe;

  TaskList copyWith({
    String? name,
    String? description,
    int? sortOption,
    bool clearSortOption = false,
    int? schedulingMode,
    bool clearSchedulingMode = false,
    int? workStartMinutes,
    bool clearWorkStartMinutes = false,
    int? workEndMinutes,
    bool clearWorkEndMinutes = false,
    List<int>? workDays,
    bool clearWorkDays = false,
    double? slackThresholdDire,
    double? slackThresholdPressing,
    double? slackThresholdFocus,
    double? slackThresholdSafe,
    bool clearSlackThresholds = false,
  }) {
    return TaskList(
      id: id,
      name: name ?? this.name,
      description: description ?? this.description,
      sortOption: clearSortOption ? null : (sortOption ?? this.sortOption),
      schedulingMode: clearSchedulingMode
          ? null
          : (schedulingMode ?? this.schedulingMode),
      workStartMinutes: clearWorkStartMinutes
          ? null
          : (workStartMinutes ?? this.workStartMinutes),
      workEndMinutes: clearWorkEndMinutes
          ? null
          : (workEndMinutes ?? this.workEndMinutes),
      workDays: clearWorkDays ? null : (workDays ?? this.workDays),
      slackThresholdDire: clearSlackThresholds
          ? null
          : (slackThresholdDire ?? this.slackThresholdDire),
      slackThresholdPressing: clearSlackThresholds
          ? null
          : (slackThresholdPressing ?? this.slackThresholdPressing),
      slackThresholdFocus: clearSlackThresholds
          ? null
          : (slackThresholdFocus ?? this.slackThresholdFocus),
      slackThresholdSafe: clearSlackThresholds
          ? null
          : (slackThresholdSafe ?? this.slackThresholdSafe),
    );
  }
}
