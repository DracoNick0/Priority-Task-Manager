/// Returns the next configured workday at [minutesSinceMidnight].
///
/// The current date is excluded. Returns null when no valid workdays are
/// configured.
DateTime? nextWorkingDayAtTime({
  required DateTime currentTime,
  required Iterable<int> workDays,
  required int minutesSinceMidnight,
}) {
  final localTime = currentTime.toLocal();
  final configuredWorkDays = workDays
      .where((day) => day >= DateTime.monday && day <= DateTime.sunday)
      .toSet();
  if (configuredWorkDays.isEmpty) return null;

  for (var daysAhead = 1; daysAhead <= DateTime.sunday; daysAhead++) {
    final candidate = DateTime(
      localTime.year,
      localTime.month,
      localTime.day + daysAhead,
    );
    if (configuredWorkDays.contains(candidate.weekday)) {
      return DateTime(
        candidate.year,
        candidate.month,
        candidate.day,
        minutesSinceMidnight ~/ 60,
        minutesSinceMidnight % 60,
      );
    }
  }

  return null;
}

/// Returns the next start time for a full working hour, or null if none fits.
DateTime? nextWorkingHour({
  required DateTime currentTime,
  required Iterable<int> workDays,
  required int workStartMinutes,
  required int workEndMinutes,
}) {
  final localTime = currentTime.toLocal();
  final configuredWorkDays = workDays
      .where((day) => day >= DateTime.monday && day <= DateTime.sunday)
      .toSet();
  if (configuredWorkDays.isEmpty ||
      workStartMinutes < 0 ||
      workEndMinutes > 24 * 60 ||
      workEndMinutes - workStartMinutes < 60) {
    return null;
  }

  for (var daysAhead = 0; daysAhead <= DateTime.sunday; daysAhead++) {
    final day = DateTime(
      localTime.year,
      localTime.month,
      localTime.day + daysAhead,
    );
    if (!configuredWorkDays.contains(day.weekday)) continue;

    final startOfWork = DateTime(
      day.year,
      day.month,
      day.day,
      workStartMinutes ~/ 60,
      workStartMinutes % 60,
    );
    var candidate = startOfWork;
    if (daysAhead == 0 && localTime.isAfter(candidate)) {
      candidate = DateTime(
        localTime.year,
        localTime.month,
        localTime.day,
        localTime.hour +
            (localTime.minute == 0 &&
                    localTime.second == 0 &&
                    localTime.millisecond == 0 &&
                    localTime.microsecond == 0
                ? 0
                : 1),
      );
      if (candidate.isBefore(startOfWork)) candidate = startOfWork;
    }
    if (!candidate
        .add(const Duration(hours: 1))
        .isAfter(
          DateTime(
            day.year,
            day.month,
            day.day,
            workEndMinutes ~/ 60,
            workEndMinutes % 60,
          ),
        )) {
      return candidate;
    }
  }

  return null;
}
