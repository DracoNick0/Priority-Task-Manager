import 'package:hive_ce/hive.dart';

part 'fixed_event.g.dart';

/// A fixed, immovable calendar event shown alongside scheduled tasks, stored
/// locally via Hive. Mirrors `PriorityTaskManager.Models.Event`.
///
/// [recurrenceRule]/[seriesId] carry the server's `RecurrenceRule`/`SeriesId`
/// (issue #70) through opaquely, as a raw JSON map, since there is no Flutter
/// recurrence-pattern picker UI yet (tracked separately); this only prevents
/// [ApiTaskRepository] edits from silently dropping a server-created series'
/// recurrence data, it does not let the app create or interpret one.
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

  FixedEvent copyWith({String? title, DateTime? startTime, DateTime? endTime}) {
    return FixedEvent(
      id: id,
      listId: listId,
      title: title ?? this.title,
      startTime: startTime ?? this.startTime,
      endTime: endTime ?? this.endTime,
      recurrenceRule: recurrenceRule,
      seriesId: seriesId,
    );
  }
}
