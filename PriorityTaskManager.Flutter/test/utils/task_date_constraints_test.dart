import 'package:flutter_test/flutter_test.dart';
import 'package:priority_task_manager/utils/task_date_constraints.dart';

void main() {
  const durationMinutes = 60;
  final dueDate = DateTime(2026, 10, 1, 12);
  final latestStart = dueDate.subtract(
    const Duration(minutes: durationMinutes),
  );

  test('allows not-before at the latest start time', () {
    expect(
      isNotBeforeWithinDueWindow(
        notBefore: latestStart,
        dueDate: dueDate,
        estimatedDurationMinutes: durationMinutes,
      ),
      isTrue,
    );
  });

  test('rejects not-before after the latest start time', () {
    expect(
      isNotBeforeWithinDueWindow(
        notBefore: latestStart.add(const Duration(microseconds: 1)),
        dueDate: dueDate,
        estimatedDurationMinutes: durationMinutes,
      ),
      isFalse,
    );
  });

  test('allows not-before when no due date is set', () {
    expect(
      isNotBeforeWithinDueWindow(
        notBefore: dueDate,
        dueDate: null,
        estimatedDurationMinutes: durationMinutes,
      ),
      isTrue,
    );
  });
}
