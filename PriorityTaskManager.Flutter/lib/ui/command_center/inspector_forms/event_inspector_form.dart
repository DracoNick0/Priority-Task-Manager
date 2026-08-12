import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../providers/event_providers.dart';
import '../../../providers/selection_provider.dart';
import '../../theme/app_theme.dart';

/// Inline CRUD form for a fixed event, shown in the Right Inspector.
///
/// A null [eventId] means "create a new event" for [listId].
class EventInspectorForm extends ConsumerStatefulWidget {
  const EventInspectorForm({super.key, required this.listId, this.eventId});

  final String listId;
  final String? eventId;

  @override
  ConsumerState<EventInspectorForm> createState() =>
      _EventInspectorFormState();
}

class _EventInspectorFormState extends ConsumerState<EventInspectorForm> {
  late final TextEditingController _titleController;
  DateTime _start = DateTime.now();
  DateTime _end = DateTime.now().add(const Duration(hours: 1));
  FixedEvent? _loadedFrom;

  bool get _isEditing => widget.eventId != null;

  @override
  void initState() {
    super.initState();
    _titleController = TextEditingController();
  }

  void _loadFrom(FixedEvent event) {
    if (identical(_loadedFrom, event)) return;
    _loadedFrom = event;
    _titleController.text = event.title;
    _start = event.startTime;
    _end = event.endTime;
  }

  @override
  void dispose() {
    _titleController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final events = ref.watch(eventsProvider(widget.listId));

    FixedEvent? existing;
    if (_isEditing) {
      existing = events.where((event) => event.id == widget.eventId).firstOrNull;
      if (existing == null) {
        return const Center(child: Text('Event not found.'));
      }
      _loadFrom(existing);
    }

    return ListView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: [
        Text(
          _isEditing ? 'Edit Event' : 'New Event',
          style: Theme.of(
            context,
          ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _titleController,
          decoration: const InputDecoration(labelText: 'Title'),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        _DateTimeRow(
          label: 'Start',
          value: _start,
          onPick: () => _pickDateTime((d) => setState(() => _start = d)),
        ),
        const SizedBox(height: AppTheme.spacingSm),
        _DateTimeRow(
          label: 'End',
          value: _end,
          onPick: () => _pickDateTime((d) => setState(() => _end = d)),
        ),
        const SizedBox(height: AppTheme.spacingLg),
        Row(
          children: [
            Expanded(
              child: FilledButton(
                onPressed: () => _save(existing),
                child: Text(_isEditing ? 'Save' : 'Create'),
              ),
            ),
            if (_isEditing) ...[
              const SizedBox(width: AppTheme.spacingSm),
              IconButton(
                icon: const Icon(Icons.delete_outline),
                tooltip: 'Delete event',
                onPressed: () => _delete(existing!),
              ),
            ],
          ],
        ),
      ],
    );
  }

  Future<void> _pickDateTime(ValueChanged<DateTime> onPicked) async {
    final date = await showDatePicker(
      context: context,
      initialDate: DateTime.now(),
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365 * 5)),
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.now(),
    );
    if (time == null) return;
    onPicked(
      DateTime(date.year, date.month, date.day, time.hour, time.minute),
    );
  }

  void _save(FixedEvent? existing) {
    final title = _titleController.text.trim();
    if (title.isEmpty) return;
    final notifier = ref.read(eventsProvider(widget.listId).notifier);

    if (existing == null) {
      final created = notifier.addEvent(
        title: title,
        startTime: _start,
        endTime: _end,
      );
      ref.read(selectedInspectorProvider.notifier).state = InspectorTarget(
        kind: InspectorKind.event,
        id: created.id,
      );
    } else {
      notifier.updateEvent(
        existing.copyWith(title: title, startTime: _start, endTime: _end),
      );
    }
  }

  void _delete(FixedEvent event) {
    ref.read(eventsProvider(widget.listId).notifier).deleteEvent(event.id);
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget.none();
  }
}

class _DateTimeRow extends StatelessWidget {
  const _DateTimeRow({
    required this.label,
    required this.value,
    required this.onPick,
  });

  final String label;
  final DateTime value;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(child: Text('$label: ${value.toLocal()}')),
        TextButton(onPressed: onPick, child: const Text('Pick')),
      ],
    );
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
