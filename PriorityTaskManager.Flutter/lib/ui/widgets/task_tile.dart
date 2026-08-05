import 'package:flutter/material.dart';

import '../../models/task_item.dart';

class TaskTile extends StatelessWidget {
  const TaskTile({
    super.key,
    required this.task,
    required this.dependencyTitles,
    required this.onToggleCompleted,
    required this.onTap,
    required this.onDelete,
  });

  final TaskItem task;
  final List<String> dependencyTitles;
  final ValueChanged<bool> onToggleCompleted;
  final VoidCallback onTap;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
      elevation: 2,
      child: ListTile(
        leading: Checkbox(
          value: task.isCompleted,
          onChanged: (value) => onToggleCompleted(value ?? false),
        ),
        title: Text(
          task.title,
          style: task.isCompleted
              ? const TextStyle(decoration: TextDecoration.lineThrough)
              : const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: dependencyTitles.isEmpty
            ? null
            : Text('Depends on: ${dependencyTitles.join(', ')}'),
        onTap: onTap,
        trailing: IconButton(
          icon: const Icon(Icons.delete_outline),
          onPressed: onDelete,
        ),
      ),
    );
  }
}
