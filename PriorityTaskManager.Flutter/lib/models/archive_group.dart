import 'fixed_event.dart';
import 'task_item.dart';

/// Category of the items sharing one archive action or source series.
enum ArchiveGroupKind { tasks, events }

/// A restorable group of archived task or event records.
class ArchiveGroup {
  const ArchiveGroup({
    required this.groupId,
    required this.kind,
    required this.tasks,
    required this.events,
  });

  /// Stable archive-group identity.
  final String groupId;

  /// Whether this group contains tasks or events.
  final ArchiveGroupKind kind;

  /// Tasks in the group; empty for event groups.
  final List<TaskItem> tasks;

  /// Events in the group; empty for task groups.
  final List<FixedEvent> events;

  /// Number of archived records in this group.
  int get itemCount =>
      kind == ArchiveGroupKind.tasks ? tasks.length : events.length;
}
