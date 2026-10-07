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

/// Expanded cards for the visible window; watches series data for mutation refreshes.
final eventOccurrencesProvider =
    FutureProvider.family<List<FixedEvent>, (String, DateTime)>((
      ref,
      key,
    ) async {
      await ref.watch(eventsProvider(key.$1).future);
      final repository = await ref.watch(taskRepositoryProvider.future);
      return repository.getEventOccurrences(
        key.$1,
        key.$2,
        DateTime(key.$2.year, key.$2.month, key.$2.day + 13),
      );
    });

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
    String description = '',
    String link = '',
    RecurrenceRule? recurrenceRule,
  }) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    final created = await repository.addEvent(
      listId: arg,
      title: title,
      startTime: startTime,
      endTime: endTime,
      description: description,
      link: link,
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

  /// Edits one or more occurrences of a recurring series (issue #71). The
  /// PUT response only reflects the prior/original series row for a
  /// `thisAndFollowing` edit, so this always refetches the full event list
  /// afterward rather than relying on any returned event.
  Future<void> editOccurrence({
    required String seriesId,
    required DateTime occurrenceDate,
    required String name,
    required DateTime startTime,
    required DateTime endTime,
    String description = '',
    String link = '',
    required RecurrenceEditTarget target,
    RecurrenceRule? recurrenceRule,
  }) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.editOccurrence(
      seriesId: seriesId,
      occurrenceDate: occurrenceDate,
      name: name,
      startTime: startTime,
      endTime: endTime,
      description: description,
      link: link,
      target: target,
      recurrenceRule: recurrenceRule,
    );
    ref.invalidateSelf();
    await future;
  }

  Future<void> deleteOccurrence({
    required String seriesId,
    required DateTime occurrenceDate,
    required RecurrenceEditTarget target,
  }) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.deleteOccurrence(
      seriesId: seriesId,
      occurrenceDate: occurrenceDate,
      target: target,
    );
    ref.invalidateSelf();
    await future;
  }
}
