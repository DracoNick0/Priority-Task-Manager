import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/utils/working_day_defaults.dart';

void main() {
  group('nextWorkingDayAtTime', () {
    test('uses the next configured day and the end of work', () {
      expect(
        nextWorkingDayAtTime(
          currentTime: DateTime(2026, 10, 2, 16, 45),
          workDays: [1, 2, 3, 4, 5],
          minutesSinceMidnight: 17 * 60 + 30,
        ),
        DateTime(2026, 10, 5, 17, 30),
      );
    });

    test('excludes today even before work ends', () {
      expect(
        nextWorkingDayAtTime(
          currentTime: DateTime(2026, 10, 5, 8),
          workDays: [1, 2, 3, 4, 5],
          minutesSinceMidnight: 17 * 60,
        ),
        DateTime(2026, 10, 6, 17),
      );
    });

    test('returns null without configured workdays', () {
      expect(
        nextWorkingDayAtTime(
          currentTime: DateTime(2026, 10, 2),
          workDays: [],
          minutesSinceMidnight: 17 * 60,
        ),
        isNull,
      );
    });
  });

  group('nextWorkingHour', () {
    DateTime? calculate(
      DateTime now, {
      List<int>? days,
      int start = 9 * 60,
      int end = 17 * 60,
    }) => nextWorkingHour(
      currentTime: now,
      workDays: days ?? [1, 2, 3, 4, 5],
      workStartMinutes: start,
      workEndMinutes: end,
    );

    test('rounds to the next full hour during the workday', () {
      expect(
        calculate(DateTime(2026, 10, 2, 10, 15)),
        DateTime(2026, 10, 2, 11),
      );
      expect(calculate(DateTime(2026, 10, 2, 10)), DateTime(2026, 10, 2, 10));
    });

    test('uses start of work before hours and skips evenings and weekends', () {
      expect(calculate(DateTime(2026, 10, 2, 8)), DateTime(2026, 10, 2, 9));
      expect(calculate(DateTime(2026, 10, 2, 16, 1)), DateTime(2026, 10, 5, 9));
      expect(calculate(DateTime(2026, 10, 3, 12)), DateTime(2026, 10, 5, 9));
    });

    test('respects minute-level settings and an hour-long event', () {
      expect(
        calculate(DateTime(2026, 10, 2, 8), start: 9 * 60 + 30, end: 11 * 60),
        DateTime(2026, 10, 2, 9, 30),
      );
      expect(
        calculate(
          DateTime(2026, 10, 2, 9, 31),
          start: 9 * 60 + 30,
          end: 11 * 60,
        ),
        DateTime(2026, 10, 2, 10),
      );
      expect(
        calculate(
          DateTime(2026, 10, 2, 10, 1),
          start: 9 * 60 + 30,
          end: 11 * 60,
        ),
        DateTime(2026, 10, 5, 9, 30),
      );
    });

    test('returns null if no working hour exists', () {
      expect(calculate(DateTime(2026, 10, 2), days: []), isNull);
      expect(
        calculate(DateTime(2026, 10, 2), start: 9 * 60, end: 9 * 60 + 30),
        isNull,
      );
    });
  });
}
