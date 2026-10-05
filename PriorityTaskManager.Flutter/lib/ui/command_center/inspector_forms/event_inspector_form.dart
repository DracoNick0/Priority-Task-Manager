import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/effective_settings.dart';
import '../../../providers/app_notifications_provider.dart';
import '../../../providers/event_providers.dart';
import '../../../providers/engine_status_provider.dart';
import '../../../providers/selection_provider.dart';
import '../../../providers/session_provider.dart';
import '../../../providers/task_providers.dart';
import '../../../providers/user_profile_provider.dart';
import '../../../utils/iterable_extensions.dart';
import '../../../utils/working_day_defaults.dart';
import '../../theme/app_theme.dart';
import 'date_time_field.dart';
import 'recurrence_edit_target_dialog.dart';
import 'recurrence_picker.dart';

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
  late DateTime _start;
  late DateTime _end;
  FixedEvent? _loadedFrom;
  RecurrenceRule? _recurrenceRule;
  bool _appliedDefaultStart = false;
  String? _titleError;

  static const String _titleRequiredWarning = 'Enter an event name.';

  bool get _isEditing => widget.eventId != null;

  @override
  void initState() {
    super.initState();
    _titleController = TextEditingController();
    _start = _roundUpToHour(DateTime.now());
    _end = _start.add(const Duration(hours: 1));
  }

  static DateTime _roundUpToHour(DateTime time) {
    final flooredToHour = DateTime(time.year, time.month, time.day, time.hour);
    return flooredToHour == time
        ? time
        : flooredToHour.add(const Duration(hours: 1));
  }

  static DateTime _nextNineAm(DateTime time) {
    final localTime = time.toLocal();
    final todayAtNine = DateTime(
      localTime.year,
      localTime.month,
      localTime.day,
      9,
    );
    return localTime.isAfter(todayAtNine)
        ? DateTime(localTime.year, localTime.month, localTime.day + 1, 9)
        : todayAtNine;
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

  void _clearTitleError(String value) {
    if (_titleError != null && value.trim().isNotEmpty) {
      setState(() => _titleError = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (!_isEditing && !_appliedDefaultStart) {
      final lists = ref.watch(taskListsProvider).asData?.value;
      final profile = ref.watch(userProfileProvider).asData?.value;
      final session = ref.watch(sessionControllerProvider).asData?.value;
      if (lists != null && profile != null && session != null) {
        final list = lists.where((l) => l.id == widget.listId).firstOrNull;
        final settings = list == null
            ? EffectiveListSettings.fromProfile(profile)
            : EffectiveListSettings.resolve(list, profile);
        final simulatedTime = session.status == SessionStatus.authenticated
            ? list?.simulatedTime
            : null;
        final currentTime = simulatedTime ?? DateTime.now();
        final start =
            nextWorkingHour(
              currentTime: currentTime,
              workDays: settings.workDays,
              workStartMinutes: settings.workStartMinutes,
              workEndMinutes: settings.workEndMinutes,
            ) ??
            _nextNineAm(currentTime);
        _start = start;
        _end = start.add(const Duration(hours: 1));
        _appliedDefaultStart = true;
      }
    }
    final events =
        ref.watch(eventsProvider(widget.listId)).asData?.value ?? const [];
    final now = ref.watch(engineClockProvider).asData?.value ?? DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final occurrences =
        ref
            .watch(eventOccurrencesProvider((widget.listId, today)))
            .asData
            ?.value ??
        const <FixedEvent>[];

    FixedEvent? existing;
    if (_isEditing) {
      existing = [
        ...occurrences,
        ...events,
      ].where((event) => event.id == widget.eventId).firstOrNull;
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
          decoration: InputDecoration(
            labelText: 'Title',
            prefixIcon: Icon(Icons.title),
            errorText: _titleError,
          ),
          onChanged: _clearTitleError,
        ),
        const SizedBox(height: AppTheme.spacingMd),
        Card(
          margin: EdgeInsets.zero,
          color: colorScheme.surfaceContainerLow,
          child: Padding(
            padding: const EdgeInsets.all(AppTheme.spacingSm),
            child: Column(
              children: [
                DateTimeCompactRow(
                  icon: Icons.play_circle_outline,
                  label: 'Start',
                  value: _start,
                  onPick: () => _pickDateTime(_start, (d) {
                    final shift = d.difference(_start);
                    setState(() {
                      _appliedDefaultStart = true;
                      _start = d;
                      _end = _end.add(shift);
                    });
                  }),
                ),
                const Divider(height: AppTheme.spacingMd),
                DateTimeCompactRow(
                  icon: Icons.stop_circle_outlined,
                  label: 'End',
                  value: _end,
                  onPick: () => _pickDateTime(
                    _end,
                    (d) => setState(() {
                      _appliedDefaultStart = true;
                      _end = d;
                    }),
                  ),
                ),
              ],
            ),
          ),
        ),
        if (_isAuthenticated &&
            (!_isEditing || existing?.seriesId != null)) ...[
          const SizedBox(height: AppTheme.spacingMd),
          RecurrencePicker(
            seriesStartDate: () => _start,
            onChanged: (rule) => setState(() => _recurrenceRule = rule),
          ),
        ],
        const SizedBox(height: AppTheme.spacingLg),
        Row(
          children: [
            Expanded(
              child: FilledButton.icon(
                onPressed: !_isEditing && !_appliedDefaultStart
                    ? null
                    : () => _save(existing),
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

  bool get _isAuthenticated =>
      ref.watch(sessionControllerProvider).asData?.value.status ==
      SessionStatus.authenticated;

  Future<void> _pickDateTime(
    DateTime initialDate,
    ValueChanged<DateTime> onPicked,
  ) async {
    final picked = await pickDateAndTime(context, initialDate: initialDate);
    if (picked != null) onPicked(picked);
  }

  Future<void> _save(FixedEvent? existing) async {
    final title = _titleController.text.trim();
    if (title.isEmpty) {
      setState(() => _titleError = _titleRequiredWarning);
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        _titleRequiredWarning,
        icon: Icons.warning_amber_rounded,
      );
      return;
    }
    final notifier = ref.read(eventsProvider(widget.listId).notifier);

    if (existing == null) {
      final created = await notifier.addEvent(
        title: title,
        startTime: _start,
        endTime: _end,
        recurrenceRule: _recurrenceRule,
      );
      if (!mounted) return;
      ref.read(selectedInspectorProvider.notifier).state = InspectorTarget(
        kind: InspectorKind.event,
        id: created.id,
      );
    } else if (existing.seriesId != null) {
      final target = await showRecurrenceEditTargetDialog(
        context,
        isDelete: false,
      );
      if (target == null) return;
      await notifier.editOccurrence(
        seriesId: existing.seriesId!,
        occurrenceDate: existing.originalOccurrenceDate ?? existing.startTime,
        name: title,
        startTime: _start,
        endTime: _end,
        target: target,
        recurrenceRule: target == RecurrenceEditTarget.thisOccurrence
            ? null
            : _recurrenceRule,
      );
      if (target == RecurrenceEditTarget.thisAndFollowing && mounted) {
        ref.read(selectedInspectorProvider.notifier).state =
            const InspectorTarget.none();
      }
    } else {
      await notifier.updateEvent(
        existing.copyWith(title: title, startTime: _start, endTime: _end),
      );
    }
  }

  Future<void> _delete(FixedEvent event) async {
    final notifier = ref.read(eventsProvider(widget.listId).notifier);
    if (event.seriesId != null) {
      final target = await showRecurrenceEditTargetDialog(
        context,
        isDelete: true,
      );
      if (target == null) return;
      await notifier.deleteOccurrence(
        seriesId: event.seriesId!,
        occurrenceDate: event.originalOccurrenceDate ?? event.startTime,
        target: target,
      );
    } else {
      await notifier.deleteEvent(event.id);
    }
    if (!mounted) return;
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget.none();
  }
}
