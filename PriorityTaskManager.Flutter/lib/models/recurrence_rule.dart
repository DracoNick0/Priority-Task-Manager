/// Mirrors `PriorityTaskManager.Models.RecurrenceRule`/`RecurrenceEndCondition`
/// (see docs/ARCHITECTURE_DATA.md "RecurrenceRule Polymorphic Shape").
///
/// These types exist purely for (de)serializing the JSON the API already
/// accepts/returns for `Event.RecurrenceRule` (issue #70); [FixedEvent] keeps
/// storing the raw map for Hive compatibility, and uses [RecurrenceRule.fromJson]
/// / [RecurrenceRule.toJson] only at the UI boundary (issue #71).
library;

/// Base class for a recurrence pattern. The `type` field is the same
/// discriminator string used by the C# `JsonDerivedType` attributes.
abstract class RecurrenceRule {
  RecurrenceRule({required this.seriesStartDate, required this.endCondition});

  final DateTime seriesStartDate;
  final RecurrenceEndCondition endCondition;

  Map<String, dynamic> toJson();

  static RecurrenceRule? fromJson(Map<dynamic, dynamic>? json) {
    if (json == null) return null;
    final map = Map<String, dynamic>.from(json);
    final seriesStartDate = DateTime.parse(map['seriesStartDate'] as String);
    final endCondition = RecurrenceEndCondition.fromJson(
      map['endCondition'] as Map<dynamic, dynamic>?,
    );
    switch (map['type'] as String?) {
      case 'dailyInterval':
        return DailyIntervalRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          intervalDays: map['intervalDays'] as int? ?? 1,
        );
      case 'weekly':
        return WeeklyRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          daysOfWeek: (map['daysOfWeek'] as List<dynamic>? ?? const [])
              .map((d) => d as int)
              .toList(),
          intervalWeeks: map['intervalWeeks'] as int? ?? 1,
        );
      case 'monthlyOnDays':
        return MonthlyOnDaysRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          daysOfMonth: (map['daysOfMonth'] as List<dynamic>? ?? const [])
              .map((d) => d as int)
              .toList(),
          shortMonthBehavior: map['shortMonthBehavior'] as String? ?? 'Skip',
        );
      case 'monthlyRelative':
        return MonthlyRelativeRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          ordinal: map['ordinal'] as String? ?? 'First',
          dayKind: map['dayKind'] as String? ?? 'SpecificDayOfWeek',
          dayOfWeek: map['dayOfWeek'] as String?,
        );
      case 'yearly':
        return YearlyRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          month: map['month'] as int? ?? 1,
          day: map['day'] as int? ?? 1,
        );
      case 'explicitDates':
        return ExplicitDatesRecurrenceRule(
          seriesStartDate: seriesStartDate,
          endCondition: endCondition,
          dates: (map['dates'] as List<dynamic>? ?? const [])
              .map((d) => DateTime.parse(d as String))
              .toList(),
        );
      default:
        return null;
    }
  }

  Map<String, dynamic> _baseJson(String type) => {
    'type': type,
    'seriesStartDate': seriesStartDate.toIso8601String(),
    'endCondition': endCondition.toJson(),
  };
}

/// Repeats every [intervalDays] days from [RecurrenceRule.seriesStartDate].
class DailyIntervalRecurrenceRule extends RecurrenceRule {
  DailyIntervalRecurrenceRule({
    required super.seriesStartDate,
    required super.endCondition,
    this.intervalDays = 1,
  });

  final int intervalDays;

  @override
  Map<String, dynamic> toJson() =>
      _baseJson('dailyInterval')..addAll({'intervalDays': intervalDays});
}

/// Repeats on [daysOfWeek] (C# `DayOfWeek` ints, 0 = Sunday .. 6 = Saturday),
/// every [intervalWeeks] weeks.
class WeeklyRecurrenceRule extends RecurrenceRule {
  WeeklyRecurrenceRule({
    required super.seriesStartDate,
    required super.endCondition,
    required this.daysOfWeek,
    this.intervalWeeks = 1,
  });

  final List<int> daysOfWeek;
  final int intervalWeeks;

  @override
  Map<String, dynamic> toJson() =>
      _baseJson('weekly')
        ..addAll({'daysOfWeek': daysOfWeek, 'intervalWeeks': intervalWeeks});
}

/// Repeats on [daysOfMonth] (1-31); [shortMonthBehavior] is `"Skip"` or
/// `"ClampToLastDay"` for months without a given day.
class MonthlyOnDaysRecurrenceRule extends RecurrenceRule {
  MonthlyOnDaysRecurrenceRule({
    required super.seriesStartDate,
    required super.endCondition,
    required this.daysOfMonth,
    this.shortMonthBehavior = 'Skip',
  });

  final List<int> daysOfMonth;
  final String shortMonthBehavior;

  @override
  Map<String, dynamic> toJson() => _baseJson('monthlyOnDays')
    ..addAll({
      'daysOfMonth': daysOfMonth,
      'shortMonthBehavior': shortMonthBehavior,
    });
}

/// Repeats on a relative occurrence within the month (e.g. "the last weekday").
/// [ordinal] is one of `First`/`Second`/`Third`/`Fourth`/`Last`; [dayKind] is
/// one of `SpecificDayOfWeek`/`AnyDay`/`Weekday`/`WeekendDay`; [dayOfWeek] is
/// a C# `DayOfWeek` name (e.g. `"Monday"`), only used for `SpecificDayOfWeek`.
class MonthlyRelativeRecurrenceRule extends RecurrenceRule {
  MonthlyRelativeRecurrenceRule({
    required super.seriesStartDate,
    required super.endCondition,
    required this.ordinal,
    required this.dayKind,
    this.dayOfWeek,
  });

  final String ordinal;
  final String dayKind;
  final String? dayOfWeek;

  @override
  Map<String, dynamic> toJson() => _baseJson('monthlyRelative')
    ..addAll({
      'ordinal': ordinal,
      'dayKind': dayKind,
      if (dayOfWeek != null) 'dayOfWeek': dayOfWeek,
    });
}

/// Repeats once a year on a fixed [month] (1-12) / [day].
class YearlyRecurrenceRule extends RecurrenceRule {
  YearlyRecurrenceRule({
    required super.seriesStartDate,
    required super.endCondition,
    required this.month,
    required this.day,
  });

  final int month;
  final int day;

  @override
  Map<String, dynamic> toJson() =>
      _baseJson('yearly')..addAll({'month': month, 'day': day});
}

/// Repeats on an explicit, caller-supplied list of [dates].
class ExplicitDatesRecurrenceRule extends RecurrenceRule {
  ExplicitDatesRecurrenceRule({
    required super.seriesStartDate,
    required super.endCondition,
    required this.dates,
  });

  final List<DateTime> dates;

  @override
  Map<String, dynamic> toJson() =>
      _baseJson('explicitDates')
        ..addAll({'dates': dates.map((d) => d.toIso8601String()).toList()});
}

/// Mirrors `PriorityTaskManager.Models.RecurrenceEndCondition`.
abstract class RecurrenceEndCondition {
  Map<String, dynamic> toJson();

  static RecurrenceEndCondition fromJson(Map<dynamic, dynamic>? json) {
    if (json == null) return NeverEndCondition();
    final map = Map<String, dynamic>.from(json);
    switch (map['type'] as String?) {
      case 'afterOccurrences':
        return AfterOccurrencesEndCondition(
          map['occurrenceCount'] as int? ?? 1,
        );
      case 'untilDate':
        return UntilDateEndCondition(
          DateTime.parse(map['untilDate'] as String),
        );
      case 'never':
      default:
        return NeverEndCondition();
    }
  }
}

/// The series never ends on its own.
class NeverEndCondition extends RecurrenceEndCondition {
  @override
  Map<String, dynamic> toJson() => {'type': 'never'};
}

/// The series ends after [occurrenceCount] occurrences.
class AfterOccurrencesEndCondition extends RecurrenceEndCondition {
  AfterOccurrencesEndCondition(this.occurrenceCount);

  final int occurrenceCount;

  @override
  Map<String, dynamic> toJson() => {
    'type': 'afterOccurrences',
    'occurrenceCount': occurrenceCount,
  };
}

/// Mirrors `PriorityTaskManager.Models.RecurrenceEditTarget` — which
/// occurrences of a recurring series an edit/delete applies to (issue #71).
enum RecurrenceEditTarget { thisOccurrence, thisAndFollowing, allOccurrences }

extension RecurrenceEditTargetApi on RecurrenceEditTarget {
  /// The string sent as the API's `target` query parameter; parsed
  /// case-insensitively server-side but matches the C# enum names.
  String get apiValue => switch (this) {
    RecurrenceEditTarget.thisOccurrence => 'ThisOccurrence',
    RecurrenceEditTarget.thisAndFollowing => 'ThisAndFollowing',
    RecurrenceEditTarget.allOccurrences => 'AllOccurrences',
  };
}

/// The series ends after [untilDate] (inclusive).
class UntilDateEndCondition extends RecurrenceEndCondition {
  UntilDateEndCondition(this.untilDate);

  final DateTime untilDate;

  @override
  Map<String, dynamic> toJson() => {
    'type': 'untilDate',
    'untilDate': untilDate.toIso8601String(),
  };
}
