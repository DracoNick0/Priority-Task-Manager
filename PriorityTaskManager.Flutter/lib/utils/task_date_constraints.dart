/// Whether [notBefore] leaves enough time to finish before [dueDate].
///
/// A missing date disables the constraint, and equality with the latest
/// possible start time is allowed.
bool isNotBeforeWithinDueWindow({
  required DateTime? notBefore,
  required DateTime? dueDate,
  required int estimatedDurationMinutes,
}) {
  if (notBefore == null || dueDate == null) return true;

  final latestStart = dueDate.subtract(
    Duration(minutes: estimatedDurationMinutes),
  );
  return !notBefore.isAfter(latestStart);
}
