import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/task_item.dart';

/// The kind of entity currently shown in the Right Inspector.
enum InspectorKind { none, task, event, list, defaults }

/// What the Right Inspector should render.
///
/// A null [id] with a non-[InspectorKind.none] kind means "create new".
class InspectorTarget {
  const InspectorTarget({required this.kind, this.id, this.task});

  const InspectorTarget.none()
    : kind = InspectorKind.none,
      id = null,
      task = null;

  final InspectorKind kind;
  final String? id;
  final TaskItem? task;

  bool get isCreating => kind != InspectorKind.none && id == null;

  @override
  bool operator ==(Object other) =>
      other is InspectorTarget &&
      other.kind == kind &&
      other.id == id &&
      identical(other.task, task);

  @override
  int get hashCode => Object.hash(kind, id, identityHashCode(task));
}

/// Drives which entity (task/event/list) the Right Inspector currently shows.
final selectedInspectorProvider = StateProvider<InspectorTarget>(
  (ref) => const InspectorTarget.none(),
);
