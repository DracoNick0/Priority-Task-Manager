import 'package:hive_ce/hive.dart';

import 'recurrence_rule.dart';

part 'fixed_event.g.dart';

/// A fixed, immovable calendar event shown alongside scheduled tasks, stored
/// locally via Hive. Mirrors `PriorityTaskManager.Models.Event`.
///
/// [recurrenceRule]/[seriesId] carry the server's `RecurrenceRule`/`SeriesId`
/// (issue #70) as a raw JSON map for Hive storage compatibility; use
/// [recurrenceRuleTyped] to read it as a [RecurrenceRule] at the UI boundary
/// (issue #71). Only `Authenticated` sessions (`ApiTaskRepository`) can
/// create or interpret one; Guests round-trip it opaquely.
@HiveType(typeId: 3)
class FixedEvent extends HiveObject {
  FixedEvent({
    required this.id,
    required this.listId,
    required this.title,
    required this.startTime,
    required this.endTime,
    this.recurrenceRule,
    this.seriesId,
    this.originalOccurrenceDate,
  });

  @HiveField(0)
  final String id;

  @HiveField(1)
  String listId;

  @HiveField(2)
  String title;

  @HiveField(3)
  DateTime startTime;

  @HiveField(4)
  DateTime endTime;

  @HiveField(5)
  Map<dynamic, dynamic>? recurrenceRule;

  @HiveField(6)
  String? seriesId;

  /// Date of the generated occurrence before any per-occurrence move.
  final DateTime? originalOccurrenceDate;

  /// Parses [recurrenceRule] into a typed [RecurrenceRule], or null if this
  /// event isn't part of a series or the map doesn't match a known pattern.
  RecurrenceRule? get recurrenceRuleTyped =>
      RecurrenceRule.fromJson(recurrenceRule);

  FixedEvent copyWith({String? title, DateTime? startTime, DateTime? endTime}) {
    return FixedEvent(
      id: id,
      listId: listId,
      title: title ?? this.title,
      startTime: startTime ?? this.startTime,
      endTime: endTime ?? this.endTime,
      recurrenceRule: recurrenceRule,
      seriesId: seriesId,
      originalOccurrenceDate: originalOccurrenceDate,
    );
  }
}
