import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/effective_settings.dart';
import '../../../models/task_item.dart';
import '../../../providers/app_notifications_provider.dart';
import '../../../providers/selection_provider.dart';
import '../../../providers/task_providers.dart';
import '../../../providers/user_profile_provider.dart';
import '../../../utils/iterable_extensions.dart';
import '../../../utils/task_date_constraints.dart';
import '../../theme/app_theme.dart';
import '../resizable_text_field.dart';
import 'combined_date_time_picker.dart';
import 'date_time_field.dart';

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
  int _complexity = 5;
  bool _isPinned = false;
  bool _isDivisible = true;
  TaskItem? _loadedFrom;
  bool _appliedDefaultDueDate = false;

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

    if (!_isEditing && !_appliedDefaultDueDate) {
      final lists = ref.watch(taskListsProvider).asData?.value;
      final profile = ref.watch(userProfileProvider).asData?.value;
      if (profile != null) {
        final list = lists?.where((l) => l.id == widget.listId).firstOrNull;
        final settings = list == null
            ? EffectiveListSettings.fromProfile(profile)
            : EffectiveListSettings.resolve(list, profile);
        final tomorrow = DateTime.now().add(const Duration(days: 1));
        _dueDate = DateTime(
          tomorrow.year,
          tomorrow.month,
          tomorrow.day,
        ).add(Duration(minutes: settings.workEndMinutes));
        _appliedDefaultDueDate = true;
      }
    }

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
        DateTimeFieldBox(
          icon: Icons.event_outlined,
          label: 'Due date',
          value: _dueDate,
          onPick: _pickDueDate,
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
          onEditingComplete: () {
            FocusScope.of(context).unfocus();
            _clearInvalidNotBefore(durationMinutes: _estimatedDurationMinutes);
          },
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
              DateTimeFieldBox(
                icon: Icons.hourglass_empty,
                label: 'Not before',
                value: _notBefore,
                onPick: () => _pickDate(_setNotBefore),
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

  static const String _notBeforeWarning =
      'Not before cleared: it must leave enough time for the task before its due date.';

  int get _estimatedDurationMinutes =>
      int.tryParse(_durationController.text.trim()) ?? 60;

  bool _clearInvalidNotBefore({
    required int durationMinutes,
    bool showWarning = true,
  }) {
    final notBefore = _notBefore;
    if (isNotBeforeWithinDueWindow(
      notBefore: notBefore,
      dueDate: _dueDate,
      estimatedDurationMinutes: durationMinutes,
    )) {
      return false;
    }

    setState(() => _notBefore = null);
    if (showWarning) _showNotBeforeWarning();
    return true;
  }

  void _setNotBefore(DateTime value) {
    if (!isNotBeforeWithinDueWindow(
      notBefore: value,
      dueDate: _dueDate,
      estimatedDurationMinutes: _estimatedDurationMinutes,
    )) {
      setState(() => _notBefore = null);
      _showNotBeforeWarning();
      return;
    }
    setState(() => _notBefore = value);
  }

  void _showNotBeforeWarning() {
    ref.read(appNotificationProvider.notifier).state = AppNotification(
      _notBeforeWarning,
      icon: Icons.warning_amber_rounded,
    );
  }

  Future<void> _pickDate(ValueChanged<DateTime> onPicked) async {
    final picked = await pickDateAndTime(
      context,
      initialDate: DateTime.now().add(const Duration(days: 1)),
      initialTime: _defaultEndOfWorkday,
      timeHelpText: 'Due time (defaults to end of workday)',
    );
    if (picked != null) onPicked(picked);
  }

  // A bottom-left toggle in the picker lets the due date be cleared, mirroring
  // the simulated-time picker in the Left Rail.
  Future<void> _pickDueDate() async {
    final tomorrow = DateTime.now().add(const Duration(days: 1));
    final initial =
        _dueDate ??
        DateTime(
          tomorrow.year,
          tomorrow.month,
          tomorrow.day,
          _defaultEndOfWorkday.hour,
          _defaultEndOfWorkday.minute,
        );
    final result =
        await showCombinedDateTimePicker<CombinedDateTimePickerResult>(
          context,
          initialDateTime: initial,
          subtitle: 'Due time (defaults to end of workday)',
          showDisableButton: true,
          disableButtonLabel: 'No due date',
        );
    if (result == null) return;
    setState(() => _dueDate = result.enabled ? result.dateTime : null);
    _clearInvalidNotBefore(durationMinutes: _estimatedDurationMinutes);
  }

  Future<void> _save(TaskItem? existing) async {
    final title = _titleController.text.trim();
    if (title.isEmpty) return;
    final duration = _estimatedDurationMinutes;
    final notBeforeWasCleared = _clearInvalidNotBefore(
      durationMinutes: duration,
      showWarning: false,
    );
    final notifier = ref.read(tasksProvider(widget.listId).notifier);

    if (existing == null) {
      await notifier.addTask(
        title: title,
        description: _descriptionController.text.trim(),
        dueDate: _dueDate,
        estimatedDurationMinutes: duration,
        notBefore: _notBefore,
        importance: _importance,
        complexity: _complexity,
        isPinned: _isPinned,
        isDivisible: _isDivisible,
        dependencies: _selectedDependencyIds.toList(),
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

    if (!mounted) return;
    ref.read(appNotificationProvider.notifier).state = AppNotification(
      notBeforeWasCleared
          ? _notBeforeWarning
          : existing == null
          ? 'Task created'
          : 'Task saved',
      icon: notBeforeWasCleared
          ? Icons.warning_amber_rounded
          : existing == null
          ? Icons.add_task
          : Icons.check_circle,
    );
    _closeInspector();
  }

  Future<void> _delete(TaskItem task) async {
    await ref.read(tasksProvider(widget.listId).notifier).deleteTask(task.id);
    if (!mounted) return;
    ref.read(appNotificationProvider.notifier).state = const AppNotification(
      'Task deleted',
      icon: Icons.delete_outline,
    );
    _closeInspector();
  }

  void _closeInspector() {
    final scaffold = Scaffold.maybeOf(context);
    if (scaffold != null && scaffold.isEndDrawerOpen) {
      scaffold.closeEndDrawer();
    }
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget.none();
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
