import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/fixed_event.dart';
import '../models/recurrence_rule.dart';
import 'task_providers.dart';

export '../models/fixed_event.dart' show FixedEvent;
export '../models/recurrence_rule.dart';

/// Fixed events for a given list id, backed by the active [TaskRepository]
/// (Hive-persisted; see docs/ARCHITECTURE_INTEGRATIONS.md).
final eventsProvider =
    AsyncNotifierProvider.family<EventsNotifier, List<FixedEvent>, String>(
      EventsNotifier.new,
    );

class EventsNotifier extends FamilyAsyncNotifier<List<FixedEvent>, String> {
  @override
  Future<List<FixedEvent>> build(String arg) async {
    final repository = await ref.watch(taskRepositoryProvider.future);
    return repository.getEvents(arg);
  }

  Future<FixedEvent> addEvent({
    required String title,
    required DateTime startTime,
    required DateTime endTime,
    RecurrenceRule? recurrenceRule,
  }) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    final created = await repository.addEvent(
      listId: arg,
      title: title,
      startTime: startTime,
      endTime: endTime,
      recurrenceRule: recurrenceRule,
    );
    ref.invalidateSelf();
    await future;
    return created;
  }

  Future<void> updateEvent(FixedEvent event) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.updateEvent(event);
    ref.invalidateSelf();
    await future;
  }

  Future<void> deleteEvent(String eventId) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.deleteEvent(eventId);
    ref.invalidateSelf();
    await future;
  }
}
