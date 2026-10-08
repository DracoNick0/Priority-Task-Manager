import 'package:flutter/material.dart';

import '../../../models/recurrence_rule.dart';

/// Prompts the user to choose which occurrences of a recurring series an
/// edit or delete applies to (issue #71). Returns null if dismissed without
/// a choice.
Future<RecurrenceEditTarget?> showRecurrenceEditTargetDialog(
  BuildContext context, {
  required bool isDelete,
  bool isArchive = false,
}) {
  final verb = isArchive
      ? 'Archive'
      : isDelete
      ? 'Delete'
      : 'Save changes to';
  return showDialog<RecurrenceEditTarget>(
    context: context,
    builder: (context) => SimpleDialog(
      title: Text('$verb which occurrences?'),
      children: [
        SimpleDialogOption(
          onPressed: () =>
              Navigator.pop(context, RecurrenceEditTarget.thisOccurrence),
          child: const Text('This occurrence'),
        ),
        SimpleDialogOption(
          onPressed: () =>
              Navigator.pop(context, RecurrenceEditTarget.thisAndFollowing),
          child: const Text('This and following occurrences'),
        ),
        SimpleDialogOption(
          onPressed: () =>
              Navigator.pop(context, RecurrenceEditTarget.allOccurrences),
          child: const Text('All occurrences'),
        ),
      ],
    ),
  );
}
