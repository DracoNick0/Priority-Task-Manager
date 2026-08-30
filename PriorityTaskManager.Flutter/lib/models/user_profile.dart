import 'package:hive_ce/hive.dart';

part 'user_profile.g.dart';

/// Global default scheduling/urgency preferences, mirroring the shape of
/// `PriorityTaskManager.Models.UserProfile`. Stored locally via Hive as a
/// single record; individual [TaskList]s may override any of these fields.
@HiveType(typeId: 2)
class UserProfile extends HiveObject {
  UserProfile({
    this.defaultListSortOption = 0,
    this.workStartMinutes = 9 * 60,
    this.workEndMinutes = 17 * 60,
    List<int>? workDays,
    this.schedulingMode = 0,
    this.desiredBreatherMinutes = 15,
    this.slackThresholdDire = 0.5,
    this.slackThresholdPressing = 1.0,
    this.slackThresholdFocus = 3.0,
    this.slackThresholdSafe = 5.0,
  }) : workDays = workDays ?? [1, 2, 3, 4, 5];

  /// `SortOption` enum index: Default, Alphabetical, DueDate, Id.
  @HiveField(0)
  int defaultListSortOption;

  /// Minutes since midnight the workday starts.
  @HiveField(1)
  int workStartMinutes;

  /// Minutes since midnight the workday ends.
  @HiveField(2)
  int workEndMinutes;

  /// Workdays as Dart weekday ints (Monday = 1 ... Sunday = 7).
  @HiveField(3)
  List<int> workDays;

  /// `SchedulingMode` enum index: GoldPanning, ConstraintOptimization.
  @HiveField(4)
  int schedulingMode;

  @HiveField(5)
  int desiredBreatherMinutes;

  @HiveField(6)
  double slackThresholdDire;

  @HiveField(7)
  double slackThresholdPressing;

  @HiveField(8)
  double slackThresholdFocus;

  @HiveField(9)
  double slackThresholdSafe;

  UserProfile copyWith({
    int? defaultListSortOption,
    int? workStartMinutes,
    int? workEndMinutes,
    List<int>? workDays,
    int? schedulingMode,
    int? desiredBreatherMinutes,
    double? slackThresholdDire,
    double? slackThresholdPressing,
    double? slackThresholdFocus,
    double? slackThresholdSafe,
  }) {
    return UserProfile(
      defaultListSortOption:
          defaultListSortOption ?? this.defaultListSortOption,
      workStartMinutes: workStartMinutes ?? this.workStartMinutes,
      workEndMinutes: workEndMinutes ?? this.workEndMinutes,
      workDays: workDays ?? List<int>.from(this.workDays),
      schedulingMode: schedulingMode ?? this.schedulingMode,
      desiredBreatherMinutes:
          desiredBreatherMinutes ?? this.desiredBreatherMinutes,
      slackThresholdDire: slackThresholdDire ?? this.slackThresholdDire,
      slackThresholdPressing:
          slackThresholdPressing ?? this.slackThresholdPressing,
      slackThresholdFocus: slackThresholdFocus ?? this.slackThresholdFocus,
      slackThresholdSafe: slackThresholdSafe ?? this.slackThresholdSafe,
    );
  }
}
