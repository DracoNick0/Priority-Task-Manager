import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/effective_settings.dart';
import '../../../models/recurrence_rule.dart';
import '../../../models/task_item.dart';
import '../../../providers/app_notifications_provider.dart';
import '../../../providers/selection_provider.dart';
import '../../../providers/session_provider.dart';
import '../../../providers/task_providers.dart';
import '../../../providers/user_profile_provider.dart';
import '../../../utils/iterable_extensions.dart';
import '../../../utils/task_date_constraints.dart';
import '../../../utils/working_day_defaults.dart';
import '../../theme/app_theme.dart';
import '../resizable_text_field.dart';
import 'combined_date_time_picker.dart';
import 'date_time_field.dart';
import 'recurrence_edit_target_dialog.dart';
import 'recurrence_picker.dart';

/// Inline CRUD form for a single task, shown in the Right Inspector.
///
/// A null [taskId] means "create a new task" for [listId].
class TaskInspectorForm extends ConsumerStatefulWidget {
  const TaskInspectorForm({
    super.key,
    required this.listId,
    this.taskId,
    this.initialTask,
  });

  final String listId;
  final String? taskId;
  final TaskItem? initialTask;

  @override
  ConsumerState<TaskInspectorForm> createState() => _TaskInspectorFormState();
}

class _TaskInspectorFormState extends ConsumerState<TaskInspectorForm> {
  late final TextEditingController _titleController;
  late final TextEditingController _descriptionController;
  late final TextEditingController _linkController;
  late final TextEditingController _durationController;
  Set<String> _selectedDependencyIds = {};
  DateTime? _dueDate;
  DateTime? _notBefore;
  int _importance = 5;
  int _complexity = 5;
  bool _isPinned = false;
  bool _isDivisible = false;
  RecurrenceRule? _recurrenceRule;
  String _progressionMode = 'RollForwardKeepBacklog';
  bool _showMissedIndicator = true;
  bool _trackStreak = false;
  bool _recurrenceEnabled = false;
  TaskItem? _loadedFrom;
  Future<TaskItem>? _occurrenceLoadFuture;
  bool _appliedDefaultDueDate = false;
  DateTime? _defaultDueDate;
  bool _hasNoWorkingDayDefault = false;
  String? _titleError;

  bool get _isEditing => widget.taskId != null;

  @override
  void initState() {
    super.initState();
    _titleController = TextEditingController();
    _descriptionController = TextEditingController();
    _linkController = TextEditingController();
    _durationController = TextEditingController(text: '60');
    final initialTask = widget.initialTask;
    if (initialTask?.isRecurringOccurrence == true) {
      _occurrenceLoadFuture = _loadAuthoritativeOccurrence(initialTask!);
    }
  }

  Future<TaskItem> _loadAuthoritativeOccurrence(TaskItem task) => ref
      .read(tasksProvider(widget.listId).notifier)
      .getTaskOccurrence(
        seriesId: task.seriesId!,
        occurrenceDate: task.occurrenceDate!,
      );

  void _loadFrom(TaskItem task) {
    if (identical(_loadedFrom, task)) return;
    _loadedFrom = task;
    _titleController.text = task.title;
    _descriptionController.text = task.description;
    _linkController.text = task.link;
    _durationController.text =
        (task.fullEstimatedDurationMinutes ?? task.estimatedDurationMinutes)
            .toString();
    _selectedDependencyIds = {...task.dependencies};
    _dueDate = task.dueDate;
    _notBefore = task.notBefore;
    _importance = task.importance;
    _complexity = task.complexity;
    _isPinned = task.isPinned;
    _isDivisible = task.isDivisible;
    _recurrenceRule = task.recurrenceRule;
    _recurrenceEnabled = task.recurrenceRule != null;
    _progressionMode = task.progressionMode;
    _showMissedIndicator = task.showMissedIndicator;
    _trackStreak = task.trackStreak;
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    _linkController.dispose();
    _durationController.dispose();
    super.dispose();
  }

  void _clearTitleError(String value) {
    if (_titleError != null && value.trim().isNotEmpty) {
      setState(() => _titleError = null);
    }
  }

  Widget _buildRecurringTaskSettings() => Column(
    children: [
      DropdownButtonFormField<String>(
        initialValue: _progressionMode,
        isExpanded: true,
        decoration: const InputDecoration(labelText: 'Missed occurrences'),
        items: const [
          DropdownMenuItem(
            value: 'SequentialCatchUp',
            child: Text(
              'Wait for each occurrence',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          DropdownMenuItem(
            value: 'RollForwardKeepBacklog',
            child: Text(
              'Roll forward and keep backlog',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          DropdownMenuItem(
            value: 'RollForwardDisregard',
            child: Text(
              'Roll forward and disregard missed',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
        onChanged: (value) {
          if (value != null) setState(() => _progressionMode = value);
        },
      ),
      SwitchListTile(
        contentPadding: EdgeInsets.zero,
        title: const Text('Show missed indicator'),
        value: _showMissedIndicator,
        onChanged: (value) => setState(() => _showMissedIndicator = value),
      ),
      SwitchListTile(
        contentPadding: EdgeInsets.zero,
        title: const Text('Track completion streaks'),
        subtitle: const Text('Show current and best streak'),
        value: _trackStreak,
        onChanged: (value) => setState(() => _trackStreak = value),
      ),
      if (_isEditing && _loadedFrom?.isRecurringOccurrence == true)
        Align(
          alignment: Alignment.centerLeft,
          child: Text(
            'These series settings apply to “This and following” or “Whole series” edits.',
            style: Theme.of(context).textTheme.bodySmall,
          ),
        ),
    ],
  );

  @override
  Widget build(BuildContext context) {
    final occurrenceFuture = _occurrenceLoadFuture;
    if (occurrenceFuture != null) {
      return FutureBuilder<TaskItem>(
        future: occurrenceFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Unable to load this occurrence: ${snapshot.error}'),
                  const SizedBox(height: AppTheme.spacingSm),
                  FilledButton(
                    onPressed: () => setState(() {
                      _occurrenceLoadFuture = _loadAuthoritativeOccurrence(
                        widget.initialTask!,
                      );
                    }),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            );
          }
          final occurrence = snapshot.data;
          if (occurrence == null) {
            return const Center(
              child: Text('The selected occurrence could not be loaded.'),
            );
          }
          return _buildForm(context, occurrence);
        },
      );
    }
    return _buildForm(context, null);
  }

  Widget _buildForm(BuildContext context, TaskItem? authoritativeOccurrence) {
    final tasksAsync = ref.watch(tasksProvider(widget.listId));
    final tasks = tasksAsync.asData?.value ?? const <TaskItem>[];
    final isAuthenticated =
        ref.watch(sessionControllerProvider).asData?.value.status ==
        SessionStatus.authenticated;

    TaskItem? existing;
    if (_isEditing) {
      existing =
          authoritativeOccurrence ??
          tasks.where((task) => task.id == widget.taskId).firstOrNull ??
          (widget.initialTask?.id == widget.taskId ? widget.initialTask : null);
      if (existing != null) _loadFrom(existing);
    }
    if (_isEditing && existing == null) {
      return const Center(child: Text('Task not found.'));
    }

    final candidateDependencies = tasks
        .where((task) => task.id != widget.taskId)
        .toList();
    final showRecurrenceControls =
        isAuthenticated &&
        (!_isEditing || existing?.recurrenceRule != null || _recurrenceEnabled);

    if (!_isEditing && !_appliedDefaultDueDate) {
      final lists = ref.watch(taskListsProvider).asData?.value;
      final profile = ref.watch(userProfileProvider).asData?.value;
      final session = ref.watch(sessionControllerProvider).asData?.value;
      if (profile != null && lists != null && session != null) {
        final list = lists.where((l) => l.id == widget.listId).firstOrNull;
        final settings = list == null
            ? EffectiveListSettings.fromProfile(profile)
            : EffectiveListSettings.resolve(list, profile);
        final simulatedTime = session.status == SessionStatus.authenticated
            ? list?.simulatedTime
            : null;
        _defaultDueDate = nextWorkingDayAtTime(
          currentTime: simulatedTime ?? DateTime.now(),
          workDays: settings.workDays,
          minutesSinceMidnight: settings.workEndMinutes,
        );
        _dueDate = _defaultDueDate;
        _hasNoWorkingDayDefault = _defaultDueDate == null;
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
          decoration: InputDecoration(
            labelText: 'Title',
            errorText: _titleError,
          ),
          onChanged: _clearTitleError,
        ),
        const SizedBox(height: AppTheme.spacingMd),
        ResizableTextField(
          controller: _descriptionController,
          label: 'Description',
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _linkController,
          decoration: const InputDecoration(
            labelText: 'Link',
            hintText: 'https://example.com',
            prefixIcon: Icon(Icons.link),
          ),
          keyboardType: TextInputType.url,
        ),
        const SizedBox(height: AppTheme.spacingMd),
        DateTimeFieldBox(
          icon: Icons.event_outlined,
          label: 'Due date',
          value: _dueDate,
          onPick: _pickDueDate,
          onClear: () => setState(() {
            _appliedDefaultDueDate = true;
            _dueDate = null;
          }),
        ),
        if (_hasNoWorkingDayDefault && _dueDate == null)
          Padding(
            padding: const EdgeInsets.only(top: AppTheme.spacingXs),
            child: Text(
              'No valid workdays are configured. Choose a due date manually.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: Theme.of(context).colorScheme.error,
              ),
            ),
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
              if (showRecurrenceControls) ...[
                RecurrencePicker(
                  key: ValueKey('task-recurrence-${widget.taskId ?? 'new'}'),
                  seriesStartDate: () =>
                      existing?.recurrenceRule?.seriesStartDate ??
                      _dueDate ??
                      DateTime.now(),
                  initialRule: existing?.recurrenceRule,
                  canDisable: existing?.recurrenceRule == null,
                  label: existing?.recurrenceRule == null
                      ? 'Recurring task'
                      : 'Series recurrence',
                  onChanged: (rule) => _recurrenceRule = rule,
                  onEnabledChanged: (enabled) =>
                      setState(() => _recurrenceEnabled = enabled),
                ),
                if (_recurrenceEnabled || existing?.recurrenceRule != null) ...[
                  const SizedBox(height: AppTheme.spacingMd),
                  _buildRecurringTaskSettings(),
                ],
                const SizedBox(height: AppTheme.spacingMd),
              ],
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
              if (candidateDependencies.isNotEmpty &&
                  !_recurrenceEnabled &&
                  existing?.recurrenceRule == null) ...[
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
                onPressed: !_isEditing && !_appliedDefaultDueDate
                    ? null
                    : () => _save(existing),
                child: Text(_isEditing ? 'Save' : 'Create'),
              ),
            ),
            if (_isEditing) ...[
              const SizedBox(width: AppTheme.spacingSm),
              IconButton(
                icon: const Icon(Icons.delete_outline),
                tooltip: isAuthenticated
                    ? 'Archive task'
                    : 'Delete permanently',
                onPressed: () => _delete(existing!),
              ),
            ],
          ],
        ),
      ],
    );
  }

  // Used as the initial time for the Not before picker.
  static const TimeOfDay _defaultEndOfWorkday = TimeOfDay(hour: 17, minute: 0);

  static const String _notBeforeWarning =
      'Not before cleared: it must leave enough time for the task before its due date.';
  static const String _titleRequiredWarning = 'Enter a task name.';

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
    final initial =
        _dueDate ??
        _defaultDueDate ??
        DateTime.now().add(const Duration(days: 1));
    final result =
        await showCombinedDateTimePicker<CombinedDateTimePickerResult>(
          context,
          initialDateTime: initial,
          subtitle: 'Due time (defaults to end of workday)',
          showDisableButton: true,
          disableButtonLabel: 'No due date',
        );
    if (result == null) return;
    setState(() {
      _appliedDefaultDueDate = true;
      _dueDate = result.enabled ? result.dateTime : null;
    });
    _clearInvalidNotBefore(durationMinutes: _estimatedDurationMinutes);
  }

  Future<void> _save(TaskItem? existing) async {
    final title = _titleController.text.trim();
    if (title.isEmpty) {
      setState(() => _titleError = _titleRequiredWarning);
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        _titleRequiredWarning,
        icon: Icons.warning_amber_rounded,
      );
      return;
    }
    final duration = _estimatedDurationMinutes;
    if (_recurrenceEnabled && _recurrenceRule == null) {
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        'Complete the recurrence pattern before saving.',
        icon: Icons.warning_amber_rounded,
      );
      return;
    }
    if (existing?.recurrenceRule != null && _recurrenceRule == null) {
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        'A recurring task must keep a valid recurrence pattern.',
        icon: Icons.warning_amber_rounded,
      );
      return;
    }
    final notBeforeWasCleared = _clearInvalidNotBefore(
      durationMinutes: duration,
      showWarning: false,
    );
    final notifier = ref.read(tasksProvider(widget.listId).notifier);

    if (existing == null) {
      await notifier.addTask(
        title: title,
        description: _descriptionController.text.trim(),
        link: _linkController.text.trim(),
        dueDate: _dueDate,
        estimatedDurationMinutes: duration,
        notBefore: _notBefore,
        importance: _importance,
        complexity: _complexity,
        isPinned: _isPinned,
        isDivisible: _isDivisible,
        dependencies: _recurrenceRule == null
            ? _selectedDependencyIds.toList()
            : const [],
        recurrenceRule: _recurrenceRule,
        progressionMode: _progressionMode,
        showMissedIndicator: _showMissedIndicator,
        trackStreak: _trackStreak,
      );
    } else if (existing.isRecurringOccurrence ||
        existing.recurrenceRule != null) {
      final target = await showRecurrenceEditTargetDialog(
        context,
        isDelete: false,
        allOccurrencesLabel: 'Whole series',
      );
      if (target == null) return;
      await notifier.editTaskOccurrence(
        seriesId: existing.seriesId ?? existing.id,
        occurrenceDate:
            existing.occurrenceDate ?? existing.recurrenceRule!.seriesStartDate,
        title: title,
        description: _descriptionController.text.trim(),
        link: _linkController.text.trim(),
        dueDate: _dueDate,
        notBefore: _notBefore,
        estimatedDurationMinutes: duration,
        importance: _importance,
        complexity: _complexity,
        isPinned: _isPinned,
        isDivisible: _isDivisible,
        target: target,
        recurrenceRule: target == RecurrenceEditTarget.thisOccurrence
            ? existing.recurrenceRule
            : _recurrenceRule,
        progressionMode: target == RecurrenceEditTarget.thisOccurrence
            ? existing.progressionMode
            : _progressionMode,
        requiredCompletions: existing.requiredCompletions,
        showMissedIndicator: target == RecurrenceEditTarget.thisOccurrence
            ? existing.showMissedIndicator
            : _showMissedIndicator,
        trackStreak: target == RecurrenceEditTarget.thisOccurrence
            ? existing.trackStreak
            : _trackStreak,
      );
    } else {
      await notifier.updateTask(
        existing.copyWith(
          title: title,
          description: _descriptionController.text.trim(),
          link: _linkController.text.trim(),
          dueDate: _dueDate,
          clearDueDate: _dueDate == null,
          estimatedDurationMinutes: duration,
          notBefore: _notBefore,
          clearNotBefore: _notBefore == null,
          importance: _importance,
          complexity: _complexity,
          isPinned: _isPinned,
          isDivisible: _isDivisible,
          dependencies: _recurrenceRule == null
              ? _selectedDependencyIds.toList()
              : const [],
          recurrenceRule: _recurrenceRule,
          progressionMode: _progressionMode,
          showMissedIndicator: _showMissedIndicator,
          trackStreak: _trackStreak,
        ),
      );
    }

    if (!mounted) return;
    ref.read(appNotificationProvider.notifier).state = AppNotification(
      notBeforeWasCleared
          ? _notBeforeWarning
          : existing == null
          ? 'Task created'
          : existing.isRecurringOccurrence || existing.recurrenceRule != null
          ? 'Recurring task updated'
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
    final isAuthenticated =
        ref.read(sessionControllerProvider).asData?.value.status ==
        SessionStatus.authenticated;
    if (task.isRecurringOccurrence ||
        (isAuthenticated && task.recurrenceRule != null)) {
      final target = await showRecurrenceEditTargetDialog(
        context,
        isDelete: true,
        allOccurrencesLabel: 'Whole series',
      );
      if (target == null) return;
      await ref
          .read(tasksProvider(widget.listId).notifier)
          .deleteTaskOccurrence(
            seriesId: task.seriesId ?? task.id,
            occurrenceDate:
                task.occurrenceDate ?? task.recurrenceRule!.seriesStartDate,
            target: target,
          );
      if (!mounted) return;
      final recurrenceStart = task.recurrenceRule?.seriesStartDate;
      final selectedDate = task.occurrenceDate ?? recurrenceStart;
      final archived =
          target == RecurrenceEditTarget.allOccurrences ||
          (target == RecurrenceEditTarget.thisAndFollowing &&
              recurrenceStart != null &&
              selectedDate != null &&
              selectedDate.year == recurrenceStart.year &&
              selectedDate.month == recurrenceStart.month &&
              selectedDate.day == recurrenceStart.day);
      ref.read(appNotificationProvider.notifier).state = AppNotification(
        archived ? 'Task archived' : 'Occurrence deleted',
        icon: archived ? Icons.archive_outlined : Icons.delete_outline,
      );
      _closeInspector();
      return;
    }
    await ref.read(tasksProvider(widget.listId).notifier).deleteTask(task.id);
    if (!mounted) return;
    ref.read(appNotificationProvider.notifier).state = AppNotification(
      isAuthenticated ? 'Task archived' : 'Task deleted',
      icon: isAuthenticated ? Icons.archive_outlined : Icons.delete_outline,
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
