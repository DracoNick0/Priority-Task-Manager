import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

/// A fixed, immovable calendar event shown alongside scheduled tasks.
///
/// This is a UI-only placeholder model: no backend Event API is wired for the
/// Flutter client yet (see docs/ARCHITECTURE_INTEGRATIONS.md). Events here are
/// kept in memory per list and are lost on restart until a real repository
/// backs this provider.
class FixedEvent {
  FixedEvent({
    required this.id,
    required this.listId,
    required this.title,
    required this.startTime,
    required this.endTime,
  });

  final String id;
  final String listId;
  String title;
  DateTime startTime;
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

/// In-memory fixed events for a given list id. Placeholder until a real
/// backend-backed event repository is introduced.
final eventsProvider =
    StateNotifierProvider.family<EventsNotifier, List<FixedEvent>, String>(
      (ref, listId) => EventsNotifier(listId),
    );

class EventsNotifier extends StateNotifier<List<FixedEvent>> {
  EventsNotifier(this.listId) : super(const []);

  final String listId;
  final _uuid = const Uuid();

  FixedEvent addEvent({
    required String title,
    required DateTime startTime,
    required DateTime endTime,
  }) {
    final event = FixedEvent(
      id: _uuid.v4(),
      listId: listId,
      title: title,
      startTime: startTime,
      endTime: endTime,
    );
    state = [...state, event];
    return event;
  }

  void updateEvent(FixedEvent event) {
    state = [
      for (final existing in state)
        if (existing.id == event.id) event else existing,
    ];
  }

  void deleteEvent(String eventId) {
    state = state.where((event) => event.id != eventId).toList();
  }
}
