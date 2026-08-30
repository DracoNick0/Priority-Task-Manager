import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/task_item.dart';
import '../../../providers/selection_provider.dart';
import '../../../providers/task_providers.dart';
import '../../theme/app_theme.dart';
import '../resizable_text_field.dart';

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
  int _complexity = 1;
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
      if (existing != null) _loadFrom(existing);
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
        ResizableTextField(
          controller: _descriptionController,
          label: 'Description',
        ),
        const SizedBox(height: AppTheme.spacingMd),
        _DatePickerRow(
          icon: Icons.event_outlined,
          label: 'Due date',
          value: _dueDate,
          onPick: () => _pickDate((d) => setState(() => _dueDate = d)),
          onClear: () => setState(() => _dueDate = null),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _durationController,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(
            labelText: 'Estimated minutes',
            prefixIcon: Icon(Icons.timer_outlined),
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        _SliderField(
          icon: Icons.priority_high,
          label: 'Importance',
          valueLabel: '$_importance',
          value: _importance.toDouble(),
          min: 1,
          max: 10,
          divisions: 9,
          onChanged: (value) => setState(() => _importance = value.round()),
        ),
        const SizedBox(height: AppTheme.spacingSm),
        _SliderField(
          icon: Icons.bar_chart,
          label: 'Complexity',
          valueLabel: '$_complexity',
          value: _complexity.toDouble(),
          min: 1,
          max: 10,
          divisions: 9,
          onChanged: (value) => setState(() => _complexity = value.round()),
        ),
        const SizedBox(height: AppTheme.spacingSm),
        Theme(
          data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
          child: ExpansionTile(
            tilePadding: EdgeInsets.zero,
            childrenPadding: EdgeInsets.zero,
            title: Text(
              'Advanced Settings',
              style: Theme.of(
                context,
              ).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
            ),
            children: [
              _DatePickerRow(
                icon: Icons.hourglass_empty,
                label: 'Not before',
                value: _notBefore,
                onPick: () => _pickDate((d) => setState(() => _notBefore = d)),
                onClear: () => setState(() => _notBefore = null),
              ),
              const SizedBox(height: AppTheme.spacingSm),
              ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.push_pin_outlined, size: 20),
                title: const Text('Pinned'),
                subtitle: const Text('Skip scheduling'),
                trailing: Transform.scale(
                  scale: 0.8,
                  child: Switch(
                    value: _isPinned,
                    onChanged: (value) => setState(() => _isPinned = value),
                  ),
                ),
              ),
              ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.call_split, size: 20),
                title: const Text('Divisible'),
                subtitle: const Text('Can be split across sessions'),
                trailing: Transform.scale(
                  scale: 0.8,
                  child: Switch(
                    value: _isDivisible,
                    onChanged: (value) => setState(() => _isDivisible = value),
                  ),
                ),
              ),
              if (candidateDependencies.isNotEmpty) ...[
                const SizedBox(height: AppTheme.spacingMd),
                Row(
                  children: [
                    Icon(
                      Icons.account_tree_outlined,
                      size: 18,
                      color: Theme.of(context).colorScheme.primary,
                    ),
                    const SizedBox(width: AppTheme.spacingXs),
                    Text(
                      'Dependencies',
                      style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        color: Theme.of(context).colorScheme.primary,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppTheme.spacingSm),
                Wrap(
                  spacing: AppTheme.spacingSm,
                  runSpacing: AppTheme.spacingSm,
                  children: [
                    for (final candidate in candidateDependencies)
                      FilterChip(
                        label: Text(candidate.title),
                        selected: _selectedDependencyIds.contains(candidate.id),
                        onSelected: (checked) {
                          setState(() {
                            if (checked) {
                              _selectedDependencyIds.add(candidate.id);
                            } else {
                              _selectedDependencyIds.remove(candidate.id);
                            }
                          });
                        },
                      ),
                  ],
                ),
              ],
            ],
          ),
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
                tooltip: 'Delete task',
                onPressed: () => _delete(existing!),
              ),
            ],
          ],
        ),
      ],
    );
  }

  // Mirrors the default UserProfile.WorkEndTime until profile settings are
  // editable from the Flutter UI.
  static const TimeOfDay _defaultEndOfWorkday = TimeOfDay(hour: 17, minute: 0);

  Future<void> _pickDate(ValueChanged<DateTime> onPicked) async {
    final pickedDate = await showDatePicker(
      context: context,
      initialDate: DateTime.now().add(const Duration(days: 1)),
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365 * 5)),
    );
    if (pickedDate == null) return;
    if (!mounted) return;

    final pickedTime = await showTimePicker(
      context: context,
      initialTime: _defaultEndOfWorkday,
      helpText: 'Due time (defaults to end of workday)',
    );
    final time = pickedTime ?? _defaultEndOfWorkday;

    onPicked(
      DateTime(
        pickedDate.year,
        pickedDate.month,
        pickedDate.day,
        time.hour,
        time.minute,
      ),
    );
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
    required this.icon,
    required this.label,
    required this.value,
    required this.onPick,
    required this.onClear,
  });

  final IconData icon;
  final String label;
  final DateTime? value;
  final VoidCallback onPick;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return InkWell(
      onTap: onPick,
      borderRadius: BorderRadius.circular(AppTheme.radiusMd),
      child: Container(
        padding: const EdgeInsets.symmetric(
          horizontal: AppTheme.spacingMd,
          vertical: AppTheme.spacingSm,
        ),
        decoration: BoxDecoration(
          border: Border.all(color: colorScheme.outline),
          borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        ),
        child: Row(
          children: [
            Icon(icon, size: 20, color: colorScheme.onSurfaceVariant),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: Theme.of(context).textTheme.labelSmall),
                  Text(
                    value == null
                        ? 'Not set'
                        : '${value!.toLocal().toString().split(' ').first} '
                              '${TimeOfDay.fromDateTime(value!.toLocal()).format(context)}',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
            if (value != null)
              IconButton(
                icon: const Icon(Icons.clear, size: 18),
                tooltip: 'Clear $label',
                onPressed: onClear,
              ),
          ],
        ),
      ),
    );
  }
}

/// Labeled slider with a leading icon and a value badge, used for the
/// Importance and Complexity fields.
class _SliderField extends StatelessWidget {
  const _SliderField({
    required this.icon,
    required this.label,
    required this.valueLabel,
    required this.value,
    required this.min,
    required this.max,
    required this.divisions,
    required this.onChanged,
  });

  final IconData icon;
  final String label;
  final String valueLabel;
  final double value;
  final double min;
  final double max;
  final int divisions;
  final ValueChanged<double> onChanged;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Icon(icon, size: 18, color: colorScheme.onSurfaceVariant),
            const SizedBox(width: AppTheme.spacingXs),
            Text(label, style: Theme.of(context).textTheme.bodyMedium),
            const Spacer(),
            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: AppTheme.spacingSm,
                vertical: 2,
              ),
              decoration: BoxDecoration(
                color: colorScheme.primaryContainer,
                borderRadius: BorderRadius.circular(AppTheme.radiusSm),
              ),
              child: Text(
                valueLabel,
                style: Theme.of(context).textTheme.labelMedium?.copyWith(
                  color: colorScheme.onPrimaryContainer,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
        Slider(
          value: value,
          min: min,
          max: max,
          divisions: divisions,
          label: valueLabel,
          onChanged: onChanged,
        ),
      ],
    );
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
