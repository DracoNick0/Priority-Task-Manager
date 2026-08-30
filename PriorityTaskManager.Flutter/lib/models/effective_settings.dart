import 'task_list.dart';
import 'user_profile.dart';

/// Resolves a [TaskList]'s per-list overrides against the global
/// [UserProfile] defaults (mirrors `TaskList.ApplyDefaultsFrom` in
/// `PriorityTaskManager.Models.TaskList`).
class EffectiveListSettings {
  EffectiveListSettings({
    required this.sortOption,
    required this.schedulingMode,
    required this.workStartMinutes,
    required this.workEndMinutes,
    required this.workDays,
    required this.desiredBreatherMinutes,
    required this.slackThresholdDire,
    required this.slackThresholdPressing,
    required this.slackThresholdFocus,
    required this.slackThresholdSafe,
  });

  factory EffectiveListSettings.resolve(TaskList list, UserProfile profile) {
    return EffectiveListSettings(
      sortOption: list.sortOption ?? profile.defaultListSortOption,
      schedulingMode: list.schedulingMode ?? profile.schedulingMode,
      workStartMinutes: list.workStartMinutes ?? profile.workStartMinutes,
      workEndMinutes: list.workEndMinutes ?? profile.workEndMinutes,
      workDays: list.workDays ?? profile.workDays,
      desiredBreatherMinutes: profile.desiredBreatherMinutes,
      slackThresholdDire: list.slackThresholdDire ?? profile.slackThresholdDire,
      slackThresholdPressing:
          list.slackThresholdPressing ?? profile.slackThresholdPressing,
      slackThresholdFocus:
          list.slackThresholdFocus ?? profile.slackThresholdFocus,
      slackThresholdSafe: list.slackThresholdSafe ?? profile.slackThresholdSafe,
    );
  }

  /// Resolves settings using only the global defaults (no list overrides).
  factory EffectiveListSettings.fromProfile(UserProfile profile) {
    return EffectiveListSettings(
      sortOption: profile.defaultListSortOption,
      schedulingMode: profile.schedulingMode,
      workStartMinutes: profile.workStartMinutes,
      workEndMinutes: profile.workEndMinutes,
      workDays: profile.workDays,
      desiredBreatherMinutes: profile.desiredBreatherMinutes,
      slackThresholdDire: profile.slackThresholdDire,
      slackThresholdPressing: profile.slackThresholdPressing,
      slackThresholdFocus: profile.slackThresholdFocus,
      slackThresholdSafe: profile.slackThresholdSafe,
    );
  }

  final int sortOption;
  final int schedulingMode;
  final int workStartMinutes;
  final int workEndMinutes;
  final List<int> workDays;
  final int desiredBreatherMinutes;
  final double slackThresholdDire;
  final double slackThresholdPressing;
  final double slackThresholdFocus;
  final double slackThresholdSafe;
}

/// `SortOption` enum names, matching `PriorityTaskManager.Models.SortOption`.
const List<String> sortOptionNames = [
  'Default',
  'Alphabetical',
  'DueDate',
  'Id',
];

/// `SchedulingMode` enum names, matching `PriorityTaskManager.Models.SchedulingMode`.
const List<String> schedulingModeNames = [
  'GoldPanning',
  'ConstraintOptimization',
];

/// Dart weekday int (Monday = 1 ... Sunday = 7) to the .NET `DayOfWeek` name
/// used by the API/local sidecar JSON contract.
const Map<int, String> dartWeekdayToDotNetName = {
  1: 'Monday',
  2: 'Tuesday',
  3: 'Wednesday',
  4: 'Thursday',
  5: 'Friday',
  6: 'Saturday',
  7: 'Sunday',
};

String formatMinutesAsTimeOfDay(int minutesSinceMidnight) {
  final hours = (minutesSinceMidnight ~/ 60) % 24;
  final minutes = minutesSinceMidnight % 60;
  final h = hours.toString().padLeft(2, '0');
  final m = minutes.toString().padLeft(2, '0');
  return '$h:$m:00';
}
