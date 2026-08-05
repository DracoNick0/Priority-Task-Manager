import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/task_item.dart';
import '../../providers/task_providers.dart';

/// Dialog used for both creating a new task and editing an existing one,
/// including managing its prerequisite dependencies within the same list.
class TaskFormDialog extends ConsumerStatefulWidget {
  const TaskFormDialog({super.key, required this.listId, this.task});

  final String listId;
  final TaskItem? task;

  @override
  ConsumerState<TaskFormDialog> createState() => _TaskFormDialogState();
}

class _TaskFormDialogState extends ConsumerState<TaskFormDialog> {
  late final TextEditingController _titleController;
  late final TextEditingController _descriptionController;
  late Set<String> _selectedDependencyIds;
  DateTime? _dueDate;

  bool get _isEditing => widget.task != null;

  @override
  void initState() {
    super.initState();
    _titleController = TextEditingController(text: widget.task?.title ?? '');
    _descriptionController = TextEditingController(
      text: widget.task?.description ?? '',
    );
    _selectedDependencyIds = {...?widget.task?.dependencies};
    _dueDate = widget.task?.dueDate;
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tasksAsync = ref.watch(tasksProvider(widget.listId));
    final candidateDependencies =
        tasksAsync.asData?.value
            .where((task) => task.id != widget.task?.id)
            .toList() ??
        const <TaskItem>[];

    return AlertDialog(
      title: Text(_isEditing ? 'Edit task' : 'New task'),
      content: SizedBox(
        width: 400,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              TextField(
                controller: _titleController,
                autofocus: true,
                decoration: const InputDecoration(
                  labelText: 'Title',
                  border: OutlineInputBorder(),
                  filled: true,
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _descriptionController,
                decoration: const InputDecoration(
                  labelText: 'Description',
                  border: OutlineInputBorder(),
                  filled: true,
                ),
                maxLines: 3,
              ),
              const SizedBox(height: 16),
              Card(
                elevation: 0,
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
                child: Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: Row(
                    children: [
                      Icon(
                        Icons.calendar_today,
                        size: 20,
                        color: Theme.of(context).colorScheme.primary,
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          _dueDate == null
                              ? 'No due date set'
                              : 'Due: ${_dueDate!.toLocal().toString().split(' ').first}',
                          style: Theme.of(context).textTheme.bodyMedium
                              ?.copyWith(
                                color: _dueDate == null
                                    ? Theme.of(
                                        context,
                                      ).colorScheme.onSurfaceVariant
                                    : Theme.of(context).colorScheme.onSurface,
                              ),
                        ),
                      ),
                      TextButton(
                        onPressed: _pickDueDate,
                        child: const Text('Pick date'),
                      ),
                      if (_dueDate != null)
                        IconButton(
                          icon: const Icon(Icons.clear),
                          onPressed: () => setState(() => _dueDate = null),
                          tooltip: 'Clear date',
                        ),
                    ],
                  ),
                ),
              ),
              if (candidateDependencies.isNotEmpty) ...[
                const SizedBox(height: 16),
                Text(
                  'Dependencies',
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    color: Theme.of(context).colorScheme.primary,
                  ),
                ),
                const SizedBox(height: 8),
                Card(
                  elevation: 0,
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                  child: Column(
                    children: [
                      for (final candidate in candidateDependencies)
                        CheckboxListTile(
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
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        FilledButton(onPressed: _save, child: const Text('Save')),
      ],
    );
  }

  Future<void> _pickDueDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _dueDate ?? DateTime.now(),
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365 * 5)),
    );
    if (picked != null) {
      setState(() => _dueDate = picked);
    }
  }

  Future<void> _save() async {
    final title = _titleController.text.trim();
    if (title.isEmpty) return;

    final notifier = ref.read(tasksProvider(widget.listId).notifier);
    final existing = widget.task;

    if (existing == null) {
      final created = await notifier.addTask(
        title: title,
        description: _descriptionController.text.trim(),
        dueDate: _dueDate,
      );
      for (final dependencyId in _selectedDependencyIds) {
        await notifier.addDependency(created.id, dependencyId);
      }
    } else {
      await notifier.updateTask(
        existing.copyWith(
          title: title,
          description: _descriptionController.text.trim(),
          dueDate: _dueDate,
          clearDueDate: _dueDate == null,
          dependencies: _selectedDependencyIds.toList(),
        ),
      );
    }

    if (mounted) Navigator.of(context).pop();
  }
}
