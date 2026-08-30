import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

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
  ConsumerState<EventInspectorForm> createState() => _EventInspectorFormState();
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
    final events =
        ref.watch(eventsProvider(widget.listId)).asData?.value ?? const [];

    FixedEvent? existing;
    if (_isEditing) {
      existing = events
          .where((event) => event.id == widget.eventId)
          .firstOrNull;
      if (existing == null) {
        return const Center(child: Text('Event not found.'));
      }
      _loadFrom(existing);
    }

    final colorScheme = Theme.of(context).colorScheme;
    return ListView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: [
        Row(
          children: [
            Icon(
              _isEditing ? Icons.event : Icons.event_available,
              color: colorScheme.primary,
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Text(
              _isEditing ? 'Edit Event' : 'New Event',
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
          ],
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _titleController,
          decoration: const InputDecoration(
            labelText: 'Title',
            prefixIcon: Icon(Icons.title),
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        Card(
          margin: EdgeInsets.zero,
          color: colorScheme.surfaceContainerLow,
          child: Padding(
            padding: const EdgeInsets.all(AppTheme.spacingSm),
            child: Column(
              children: [
                _DateTimeRow(
                  icon: Icons.play_circle_outline,
                  label: 'Start',
                  value: _start,
                  onPick: () =>
                      _pickDateTime((d) => setState(() => _start = d)),
                ),
                const Divider(height: AppTheme.spacingMd),
                _DateTimeRow(
                  icon: Icons.stop_circle_outlined,
                  label: 'End',
                  value: _end,
                  onPick: () => _pickDateTime((d) => setState(() => _end = d)),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: AppTheme.spacingLg),
        Row(
          children: [
            Expanded(
              child: FilledButton.icon(
                onPressed: () => _save(existing),
                icon: Icon(_isEditing ? Icons.save_outlined : Icons.add),
                label: Text(_isEditing ? 'Save' : 'Create'),
              ),
            ),
            if (_isEditing) ...[
              const SizedBox(width: AppTheme.spacingSm),
              IconButton.outlined(
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
    onPicked(DateTime(date.year, date.month, date.day, time.hour, time.minute));
  }

  Future<void> _save(FixedEvent? existing) async {
    final title = _titleController.text.trim();
    if (title.isEmpty) return;
    final notifier = ref.read(eventsProvider(widget.listId).notifier);

    if (existing == null) {
      final created = await notifier.addEvent(
        title: title,
        startTime: _start,
        endTime: _end,
      );
      if (!mounted) return;
      ref.read(selectedInspectorProvider.notifier).state = InspectorTarget(
        kind: InspectorKind.event,
        id: created.id,
      );
    } else {
      await notifier.updateEvent(
        existing.copyWith(title: title, startTime: _start, endTime: _end),
      );
    }
  }

  Future<void> _delete(FixedEvent event) async {
    await ref
        .read(eventsProvider(widget.listId).notifier)
        .deleteEvent(event.id);
    if (!mounted) return;
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget.none();
  }
}

class _DateTimeRow extends StatelessWidget {
  const _DateTimeRow({
    required this.icon,
    required this.label,
    required this.value,
    required this.onPick,
  });

  final IconData icon;
  final String label;
  final DateTime value;
  final VoidCallback onPick;

  static final _format = DateFormat('EEE, MMM d • h:mm a');

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return InkWell(
      onTap: onPick,
      borderRadius: BorderRadius.circular(AppTheme.radiusSm),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppTheme.spacingXs),
        child: Row(
          children: [
            Icon(icon, size: 18, color: colorScheme.onSurfaceVariant),
            const SizedBox(width: AppTheme.spacingSm),
            SizedBox(
              width: 40,
              child: Text(label, style: Theme.of(context).textTheme.bodySmall),
            ),
            Expanded(
              child: Text(
                _format.format(value.toLocal()),
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ),
            Icon(
              Icons.edit_calendar_outlined,
              size: 18,
              color: colorScheme.primary,
            ),
          ],
        ),
      ),
    );
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
