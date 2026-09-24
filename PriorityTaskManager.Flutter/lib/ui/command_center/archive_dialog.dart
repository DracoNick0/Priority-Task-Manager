import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../data/task_repository.dart';
import '../../providers/archive_providers.dart';
import '../../providers/task_providers.dart';
import '../theme/app_theme.dart';

/// Shows the Archive dialog: a simple list of archived tasks with a restore
/// action per task, opened from the Left Rail (Authenticated sessions only;
/// see docs/VISION.md on Archive being an online-exclusive feature).
Future<void> showArchiveDialog(BuildContext context) {
  return showDialog<void>(
    context: context,
    builder: (context) => const _ArchiveDialog(),
  );
}

class _ArchiveDialog extends ConsumerWidget {
  const _ArchiveDialog();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final archivedTasksAsync = ref.watch(archivedTasksProvider);

    return AlertDialog(
      title: const Text('Archive'),
      content: SizedBox(
        width: 420,
        height: 420,
        child: archivedTasksAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) =>
              Center(child: Text('Could not load archive: $error')),
          data: (archivedTasks) {
            if (archivedTasks.isEmpty) {
              return const Center(child: Text('No archived tasks.'));
            }
            return ListView.separated(
              itemCount: archivedTasks.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) {
                final task = archivedTasks[index];
                return ListTile(
                  title: Text(task.title),
                  subtitle: task.dueDate == null
                      ? null
                      : Text('Due ${DateFormat.yMMMd().format(task.dueDate!)}'),
                  trailing: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      FilledButton.tonal(
                        onPressed: () => _restore(context, ref, task.id),
                        child: const Text('Restore'),
                      ),
                      const SizedBox(width: AppTheme.spacingXs),
                      IconButton(
                        tooltip: 'Delete permanently',
                        icon: const Icon(Icons.delete_outline),
                        onPressed: () => _delete(context, ref, task.id),
                      ),
                    ],
                  ),
                );
              },
            );
          },
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Close'),
        ),
      ],
    );
  }

  Future<void> _restore(
    BuildContext context,
    WidgetRef ref,
    String taskId, {
    String? targetListId,
  }) async {
    try {
      await ref
          .read(archivedTasksProvider.notifier)
          .restoreArchivedTask(taskId, targetListId: targetListId);
    } on RestoreTargetListRequiredException {
      if (!context.mounted) return;
      final chosenListId = await _pickTargetList(context, ref);
      if (chosenListId == null) return;
      if (!context.mounted) return;
      await _restore(context, ref, taskId, targetListId: chosenListId);
      return;
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not restore task: $error')));
      return;
    }
    if (!context.mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Task restored.')));
  }

  Future<void> _delete(
    BuildContext context,
    WidgetRef ref,
    String taskId,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete permanently?'),
        content: const Text(
          'This archived task will be permanently deleted and cannot be restored.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      await ref.read(archivedTasksProvider.notifier).deleteArchivedTask(taskId);
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not delete task: $error')));
      return;
    }
    if (!context.mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Task permanently deleted.')));
  }

  Future<String?> _pickTargetList(BuildContext context, WidgetRef ref) async {
    final lists = ref.read(taskListsProvider).asData?.value ?? const [];
    return showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Choose a list'),
        content: SizedBox(
          width: 320,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Padding(
                padding: EdgeInsets.only(bottom: AppTheme.spacingSm),
                child: Text(
                  "This task's original list no longer exists. Choose a list to restore it into:",
                ),
              ),
              for (final list in lists)
                ListTile(
                  title: Text(list.name),
                  onTap: () => Navigator.of(context).pop(list.id),
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
      ),
    );
  }
}
