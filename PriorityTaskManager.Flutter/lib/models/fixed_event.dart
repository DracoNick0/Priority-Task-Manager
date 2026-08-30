import 'package:hive_ce/hive.dart';

part 'fixed_event.g.dart';

/// A fixed, immovable calendar event shown alongside scheduled tasks, stored
/// locally via Hive. Mirrors `PriorityTaskManager.Models.Event`.
@HiveType(typeId: 3)
class FixedEvent extends HiveObject {
  FixedEvent({
    required this.id,
    required this.listId,
    required this.title,
    required this.startTime,
    required this.endTime,
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

  FixedEvent copyWith({String? title, DateTime? startTime, DateTime? endTime}) {
    return FixedEvent(
      id: id,
      listId: listId,
      title: title ?? this.title,
      startTime: startTime ?? this.startTime,
      endTime: endTime ?? this.endTime,
    );
  }
}
