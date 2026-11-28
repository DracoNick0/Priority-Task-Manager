import 'package:flutter/material.dart';

import '../../models/task_item.dart';
import '../../utils/dependency_graph_builder.dart';

/// Which side of the selected task receives a newly linked task.
enum DependencyLinkDirection { prerequisite, followUp }

/// Picks a one-off task from the active list without offering existing links.
Future<TaskItem?> showLinkExistingTaskDialog(
  BuildContext context, {
  required List<TaskItem> tasks,
  required TaskItem? task,
  required DependencyLinkDirection direction,
  Set<String> excludedIds = const {},
}) {
  if (direction == DependencyLinkDirection.followUp && task == null) {
    throw ArgumentError.notNull('task');
  }
  return showDialog<TaskItem>(
    context: context,
    builder: (context) => _LinkExistingTaskDialog(
      tasks: tasks,
      task: task,
      direction: direction,
      excludedIds: excludedIds,
    ),
  );
}

class _LinkExistingTaskDialog extends StatefulWidget {
  const _LinkExistingTaskDialog({
    required this.tasks,
    required this.task,
    required this.direction,
    required this.excludedIds,
  });

  final List<TaskItem> tasks;
  final TaskItem? task;
  final DependencyLinkDirection direction;
  final Set<String> excludedIds;

  @override
  State<_LinkExistingTaskDialog> createState() =>
      _LinkExistingTaskDialogState();
}

class _LinkExistingTaskDialogState extends State<_LinkExistingTaskDialog> {
  String _query = '';

  bool _wouldCreateCycle(TaskItem candidate) {
    final task = widget.task;
    if (task == null) return false;
    return DependencyGraphBuilder.wouldCreateCycle(
      widget.tasks,
      taskId: widget.direction == DependencyLinkDirection.followUp
          ? candidate.id
          : task.id,
      proposedDependencyId: widget.direction == DependencyLinkDirection.followUp
          ? task.id
          : candidate.id,
    );
  }

  @override
  Widget build(BuildContext context) {
    final isFollowUp = widget.direction == DependencyLinkDirection.followUp;
    final candidates =
        widget.tasks
            .where(
              (candidate) =>
                  candidate.recurrenceRule == null &&
                  !candidate.isRecurringOccurrence &&
                  candidate.id != widget.task?.id &&
                  (widget.task == null ||
                      candidate.listId == widget.task!.listId) &&
                  !widget.excludedIds.contains(candidate.id) &&
                  !(isFollowUp
                      ? candidate.dependencies.contains(widget.task!.id)
                      : widget.task?.dependencies.contains(candidate.id) ??
                            false),
            )
            .where(
              (candidate) =>
                  candidate.title.toLowerCase().contains(_query.toLowerCase()),
            )
            .toList()
          ..sort((a, b) => a.title.compareTo(b.title));

    return AlertDialog(
      title: Text(
        isFollowUp ? 'Link Existing Follow-Up' : 'Link Existing Prerequisite',
      ),
      content: SizedBox(
        width: 400,
        height: 340,
        child: Column(
          children: [
            TextField(
              autofocus: true,
              decoration: const InputDecoration(
                labelText: 'Search tasks',
                prefixIcon: Icon(Icons.search),
              ),
              onChanged: (value) => setState(() => _query = value),
            ),
            Expanded(
              child: candidates.isEmpty
                  ? const Center(
                      child: Text('No eligible one-off tasks in this list.'),
                    )
                  : ListView(
                      children: [
                        for (final candidate in candidates)
                          Builder(
                            builder: (context) {
                              final wouldCycle = _wouldCreateCycle(candidate);
                              return ListTile(
                                key: ValueKey('link-candidate-${candidate.id}'),
                                title: Text(candidate.title),
                                subtitle: wouldCycle
                                    ? const Text(
                                        'Would create a circular dependency',
                                      )
                                    : null,
                                trailing: Icon(
                                  wouldCycle ? Icons.block : Icons.link,
                                  color: wouldCycle
                                      ? Theme.of(context).colorScheme.error
                                      : null,
                                ),
                                enabled: !wouldCycle,
                                onTap: wouldCycle
                                    ? null
                                    : () =>
                                          Navigator.of(context).pop(candidate),
                              );
                            },
                          ),
                      ],
                    ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
      ],
    );
  }
}
