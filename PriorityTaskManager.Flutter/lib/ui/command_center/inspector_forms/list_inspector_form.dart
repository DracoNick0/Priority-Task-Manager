import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/task_list.dart';
import '../../../providers/selection_provider.dart';
import '../../../providers/task_providers.dart';
import '../../theme/app_theme.dart';
import '../resizable_text_field.dart';
import 'settings_fields.dart';

/// Inline CRUD form for renaming/deleting a task list and editing its
/// scheduling-settings overrides, shown in the Right Inspector.
class ListInspectorForm extends ConsumerStatefulWidget {
  const ListInspectorForm({super.key, required this.listId});

  final String listId;

  @override
  ConsumerState<ListInspectorForm> createState() => _ListInspectorFormState();
}

class _ListInspectorFormState extends ConsumerState<ListInspectorForm> {
  late final TextEditingController _nameController;
  late final TextEditingController _descriptionController;
  TaskList? _loadedFrom;

  int? _sortOption;
  int? _schedulingMode;
  int? _workStartMinutes;
  int? _workEndMinutes;
  List<int>? _workDays;
  double? _slackThresholdDire;
  double? _slackThresholdPressing;
  double? _slackThresholdFocus;
  double? _slackThresholdSafe;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController();
    _descriptionController = TextEditingController();
  }

  void _loadFrom(TaskList list) {
    if (identical(_loadedFrom, list)) return;
    _loadedFrom = list;
    _nameController.text = list.name;
    _descriptionController.text = list.description ?? '';
    _sortOption = list.sortOption;
    _schedulingMode = list.schedulingMode;
    _workStartMinutes = list.workStartMinutes;
    _workEndMinutes = list.workEndMinutes;
    _workDays = list.workDays;
    _slackThresholdDire = list.slackThresholdDire;
    _slackThresholdPressing = list.slackThresholdPressing;
    _slackThresholdFocus = list.slackThresholdFocus;
    _slackThresholdSafe = list.slackThresholdSafe;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
    final list = lists.where((l) => l.id == widget.listId).firstOrNull;
    if (list == null) {
      return const Center(child: Text('List not found.'));
    }
    _loadFrom(list);

    return ListView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: [
        Row(
          children: [
            Icon(Icons.list_alt, color: Theme.of(context).colorScheme.primary),
            const SizedBox(width: AppTheme.spacingSm),
            Text(
              'Edit List',
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
          ],
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _nameController,
          decoration: const InputDecoration(
            labelText: 'Name',
            prefixIcon: Icon(Icons.drive_file_rename_outline),
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        ResizableTextField(
          controller: _descriptionController,
          label: 'Description',
        ),
        const SizedBox(height: AppTheme.spacingLg),
        Row(
          children: [
            Icon(
              Icons.settings_suggest_outlined,
              size: 18,
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Text(
              'List Settings',
              style: Theme.of(
                context,
              ).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
            ),
          ],
        ),
        Text(
          'Unchecked settings inherit the global defaults.',
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            color: Theme.of(context).colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: AppTheme.spacingSm),
        OverrideToggle(
          label: 'Sort tasks by',
          isOverridden: _sortOption != null,
          onChanged: (v) => setState(() => _sortOption = v ? 0 : null),
          child: SortOptionField(
            value: _sortOption ?? 0,
            onChanged: (v) => setState(() => _sortOption = v),
          ),
        ),
        OverrideToggle(
          label: 'Scheduling mode',
          isOverridden: _schedulingMode != null,
          onChanged: (v) => setState(() => _schedulingMode = v ? 0 : null),
          child: SchedulingModeField(
            value: _schedulingMode ?? 0,
            onChanged: (v) => setState(() => _schedulingMode = v),
          ),
        ),
        OverrideToggle(
          label: 'Work hours',
          isOverridden: _workStartMinutes != null && _workEndMinutes != null,
          onChanged: (v) => setState(() {
            _workStartMinutes = v ? (_workStartMinutes ?? 9 * 60) : null;
            _workEndMinutes = v ? (_workEndMinutes ?? 17 * 60) : null;
          }),
          child: WorkHoursField(
            startMinutes: _workStartMinutes ?? 9 * 60,
            endMinutes: _workEndMinutes ?? 17 * 60,
            onStartChanged: (v) => setState(() => _workStartMinutes = v),
            onEndChanged: (v) => setState(() => _workEndMinutes = v),
          ),
        ),
        OverrideToggle(
          label: 'Work days',
          isOverridden: _workDays != null,
          onChanged: (v) =>
              setState(() => _workDays = v ? const [1, 2, 3, 4, 5] : null),
          child: WorkDaysField(
            selected: (_workDays ?? const [1, 2, 3, 4, 5]).toSet(),
            onChanged: (v) => setState(() => _workDays = v.toList()..sort()),
          ),
        ),
        OverrideToggle(
          label: 'Urgency thresholds',
          isOverridden: _slackThresholdDire != null,
          onChanged: (v) => setState(() {
            _slackThresholdDire = v ? 0.5 : null;
            _slackThresholdPressing = v ? 1.0 : null;
            _slackThresholdFocus = v ? 3.0 : null;
            _slackThresholdSafe = v ? 5.0 : null;
          }),
          child: SlackThresholdsField(
            dire: _slackThresholdDire ?? 0.5,
            pressing: _slackThresholdPressing ?? 1.0,
            focus: _slackThresholdFocus ?? 3.0,
            safe: _slackThresholdSafe ?? 5.0,
            onDireChanged: (v) => setState(() => _slackThresholdDire = v),
            onPressingChanged: (v) =>
                setState(() => _slackThresholdPressing = v),
            onFocusChanged: (v) => setState(() => _slackThresholdFocus = v),
            onSafeChanged: (v) => setState(() => _slackThresholdSafe = v),
          ),
        ),
        const SizedBox(height: AppTheme.spacingLg),
        Row(
          children: [
            Expanded(
              child: FilledButton.icon(
                onPressed: () => _save(list),
                icon: const Icon(Icons.save_outlined),
                label: const Text('Save'),
              ),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            IconButton.outlined(
              icon: const Icon(Icons.delete_outline),
              tooltip: 'Delete list',
              onPressed: () => _delete(list),
            ),
          ],
        ),
      ],
    );
  }

  Future<void> _save(TaskList list) async {
    final name = _nameController.text.trim();
    if (name.isEmpty) return;
    await ref
        .read(taskListsProvider.notifier)
        .updateList(
          list.copyWith(
            name: name,
            description: _descriptionController.text.trim(),
            sortOption: _sortOption,
            clearSortOption: _sortOption == null,
            schedulingMode: _schedulingMode,
            clearSchedulingMode: _schedulingMode == null,
            workStartMinutes: _workStartMinutes,
            clearWorkStartMinutes: _workStartMinutes == null,
            workEndMinutes: _workEndMinutes,
            clearWorkEndMinutes: _workEndMinutes == null,
            workDays: _workDays,
            clearWorkDays: _workDays == null,
            slackThresholdDire: _slackThresholdDire,
            slackThresholdPressing: _slackThresholdPressing,
            slackThresholdFocus: _slackThresholdFocus,
            slackThresholdSafe: _slackThresholdSafe,
            clearSlackThresholds: _slackThresholdDire == null,
          ),
        );
  }

  Future<void> _delete(TaskList list) async {
    await ref.read(taskListsProvider.notifier).deleteList(list.id);
    if (ref.read(activeListIdProvider) == list.id) {
      ref.read(activeListIdProvider.notifier).state = null;
    }
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget.none();
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
