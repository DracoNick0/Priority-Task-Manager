import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/task_item.dart';
import '../../../providers/selection_provider.dart';
import '../../../providers/task_providers.dart';
import '../../theme/app_theme.dart';

/// Inline CRUD form for a single task, shown in the Right Inspector.
///
/// A null [taskId] means "create a new task" for [listId].
class TaskInspectorForm extends ConsumerStatefulWidget {
  const TaskInspectorForm({super.key, required this.listId, this.taskId});

  final String listId;
  final String? taskId;

  @override
  ConsumerState<TaskInspectorForm> createState() => _TaskInspectorFormState();
}

class _TaskInspectorFormState extends ConsumerState<TaskInspectorForm> {
  late final TextEditingController _titleController;
  late final TextEditingController _descriptionController;
  late final TextEditingController _durationController;
  Set<String> _selectedDependencyIds = {};
  DateTime? _dueDate;
  DateTime? _notBefore;
  int _importance = 5;
  double _complexity = 1.0;
  bool _isPinned = false;
  bool _isDivisible = true;
  TaskItem? _loadedFrom;

  bool get _isEditing => widget.taskId != null;

  @override
  void initState() {
    super.initState();
    _titleController = TextEditingController();
    _descriptionController = TextEditingController();
    _durationController = TextEditingController(text: '60');
  }

  void _loadFrom(TaskItem task) {
    if (identical(_loadedFrom, task)) return;
    _loadedFrom = task;
    _titleController.text = task.title;
    _descriptionController.text = task.description;
    _durationController.text = task.estimatedDurationMinutes.toString();
    _selectedDependencyIds = {...task.dependencies};
    _dueDate = task.dueDate;
    _notBefore = task.notBefore;
    _importance = task.importance;
    _complexity = task.complexity;
    _isPinned = task.isPinned;
    _isDivisible = task.isDivisible;
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    _durationController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tasksAsync = ref.watch(tasksProvider(widget.listId));
    final tasks = tasksAsync.asData?.value ?? const <TaskItem>[];

    TaskItem? existing;
    if (_isEditing) {
      existing = tasks.where((task) => task.id == widget.taskId).firstOrNull;
      if (existing == null) {
        return const Center(child: Text('Task not found.'));
      }
      _loadFrom(existing);
    }

    final candidateDependencies = tasks
        .where((task) => task.id != widget.taskId)
        .toList();

    return ListView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: [
        Text(
          _isEditing ? 'Edit Task' : 'New Task',
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
        TextField(
          controller: _descriptionController,
          decoration: const InputDecoration(labelText: 'Description'),
          maxLines: 3,
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _durationController,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(labelText: 'Estimated minutes'),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        _DatePickerRow(
          label: 'Due date',
          value: _dueDate,
          onPick: () => _pickDate((d) => setState(() => _dueDate = d)),
          onClear: () => setState(() => _dueDate = null),
        ),
        const SizedBox(height: AppTheme.spacingSm),
        _DatePickerRow(
          label: 'Not before',
          value: _notBefore,
          onPick: () => _pickDate((d) => setState(() => _notBefore = d)),
          onClear: () => setState(() => _notBefore = null),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        Text('Importance: $_importance'),
        Slider(
          value: _importance.toDouble(),
          min: 1,
          max: 10,
          divisions: 9,
          label: '$_importance',
          onChanged: (value) => setState(() => _importance = value.round()),
        ),
        Text('Complexity: ${_complexity.toStringAsFixed(1)}'),
        Slider(
          value: _complexity,
          min: 0.5,
          max: 5,
          divisions: 9,
          label: _complexity.toStringAsFixed(1),
          onChanged: (value) => setState(() => _complexity = value),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Pinned (skip scheduling)'),
          value: _isPinned,
          onChanged: (value) => setState(() => _isPinned = value),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Divisible (can be split)'),
          value: _isDivisible,
          onChanged: (value) => setState(() => _isDivisible = value),
        ),
        if (candidateDependencies.isNotEmpty) ...[
          const SizedBox(height: AppTheme.spacingMd),
          Text(
            'Dependencies',
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
              color: Theme.of(context).colorScheme.primary,
              fontWeight: FontWeight.bold,
            ),
          ),
          for (final candidate in candidateDependencies)
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              dense: true,
              title: Text(candidate.title),
              value: _selectedDependencyIds.contains(candidate.id),
              onChanged: (checked) {
                setState(() {
                  if (checked ?? false) {
                    _selectedDependencyIds.add(candidate.id);
                  } else {
                    _selectedDependencyIds.remove(candidate.id);
                  }
                });
              },
            ),
        ],
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
                tooltip: 'Delete task',
                onPressed: () => _delete(existing!),
              ),
            ],
          ],
        ),
      ],
    );
  }

  Future<void> _pickDate(ValueChanged<DateTime> onPicked) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now(),
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365 * 5)),
    );
    if (picked != null) onPicked(picked);
  }

  Future<void> _save(TaskItem? existing) async {
    final title = _titleController.text.trim();
    if (title.isEmpty) return;
    final duration = int.tryParse(_durationController.text.trim()) ?? 60;
    final notifier = ref.read(tasksProvider(widget.listId).notifier);

    if (existing == null) {
      final created = await notifier.addTask(
        title: title,
        description: _descriptionController.text.trim(),
        dueDate: _dueDate,
        estimatedDurationMinutes: duration,
      );
      await notifier.updateTask(
        created.copyWith(
          notBefore: _notBefore,
          clearNotBefore: _notBefore == null,
          importance: _importance,
          complexity: _complexity,
          isPinned: _isPinned,
          isDivisible: _isDivisible,
          dependencies: _selectedDependencyIds.toList(),
        ),
      );
      ref.read(selectedInspectorProvider.notifier).state = InspectorTarget(
        kind: InspectorKind.task,
        id: created.id,
      );
    } else {
      await notifier.updateTask(
        existing.copyWith(
          title: title,
          description: _descriptionController.text.trim(),
          dueDate: _dueDate,
          clearDueDate: _dueDate == null,
          estimatedDurationMinutes: duration,
          notBefore: _notBefore,
          clearNotBefore: _notBefore == null,
          importance: _importance,
          complexity: _complexity,
          isPinned: _isPinned,
          isDivisible: _isDivisible,
          dependencies: _selectedDependencyIds.toList(),
        ),
      );
    }
  }

  Future<void> _delete(TaskItem task) async {
    await ref.read(tasksProvider(widget.listId).notifier).deleteTask(task.id);
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget.none();
  }
}

class _DatePickerRow extends StatelessWidget {
  const _DatePickerRow({
    required this.label,
    required this.value,
    required this.onPick,
    required this.onClear,
  });

  final String label;
  final DateTime? value;
  final VoidCallback onPick;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            value == null
                ? '$label: not set'
                : '$label: ${value!.toLocal().toString().split(' ').first}',
          ),
        ),
        TextButton(onPressed: onPick, child: const Text('Pick')),
        if (value != null)
          IconButton(icon: const Icon(Icons.clear), onPressed: onClear),
      ],
    );
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
