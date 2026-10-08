import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../data/task_repository.dart';
import '../../models/task_item.dart';
import '../../providers/archive_providers.dart';
import '../../providers/event_providers.dart';
import '../../providers/task_providers.dart';
import '../theme/app_theme.dart';

/// Shows the archive dialog with restore and permanent-delete actions,
/// opened from the Left Rail (Authenticated sessions only;
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
    final archivedEventsAsync = ref.watch(archivedEventsProvider);
    final canClear =
        (archivedTasksAsync.asData?.value.isNotEmpty ?? false) ||
        (archivedEventsAsync.asData?.value.isNotEmpty ?? false);

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
            return archivedEventsAsync.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) =>
                  Center(child: Text('Could not load archived events: $error')),
              data: (archivedEvents) =>
                  _archiveItems(context, ref, archivedTasks, archivedEvents),
            );
          },
        ),
      ),
      actions: [
        TextButton(
          onPressed: canClear ? () => _clearArchive(context, ref) : null,
          child: const Text('Clear archive'),
        ),
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Close'),
        ),
      ],
    );
  }

  Widget _archiveItems(
    BuildContext context,
    WidgetRef ref,
    List<TaskItem> tasks,
    List<FixedEvent> events,
  ) {
    if (tasks.isEmpty && events.isEmpty) {
      return const Center(child: Text('No archived items.'));
    }

    return ListView(
      children: [
        if (tasks.isNotEmpty) ...[
          const Padding(
            padding: EdgeInsets.symmetric(vertical: AppTheme.spacingSm),
            child: Text('Tasks'),
          ),
          for (final task in tasks)
            ListTile(
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
            ),
        ],
        if (events.isNotEmpty) ...[
          const Padding(
            padding: EdgeInsets.symmetric(vertical: AppTheme.spacingSm),
            child: Text('Events'),
          ),
          for (final event in events)
            ListTile(
              title: Text(event.title),
              subtitle: Text(
                DateFormat.yMMMd().add_jm().format(event.startTime),
              ),
              trailing: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  FilledButton.tonal(
                    onPressed: () => _restoreEvent(context, ref, event.id),
                    child: const Text('Restore'),
                  ),
                  const SizedBox(width: AppTheme.spacingXs),
                  IconButton(
                    tooltip: 'Delete permanently',
                    icon: const Icon(Icons.delete_outline),
                    onPressed: () => _deleteEvent(context, ref, event.id),
                  ),
                ],
              ),
            ),
        ],
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

  Future<void> _restoreEvent(
    BuildContext context,
    WidgetRef ref,
    String eventId,
  ) async {
    try {
      await ref
          .read(archivedEventsProvider.notifier)
          .restoreArchivedEvent(eventId);
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Could not restore event: $error')),
      );
      return;
    }
    if (!context.mounted) return;
    ref.invalidate(eventsProvider);
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Event restored.')));
  }

  Future<void> _deleteEvent(
    BuildContext context,
    WidgetRef ref,
    String eventId,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete permanently?'),
        content: const Text(
          'This archived event will be permanently deleted and cannot be restored.',
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
      await ref
          .read(archivedEventsProvider.notifier)
          .deleteArchivedEvent(eventId);
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not delete event: $error')));
      return;
    }
    if (!context.mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Event permanently deleted.')));
  }

  Future<void> _clearArchive(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Clear archive permanently?'),
        content: const Text(
          'All archived tasks and events will be permanently deleted and cannot be restored.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Clear archive'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      await ref.read(archivedTasksProvider.notifier).clearArchive();
    } catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Could not clear archive: $error')),
      );
      return;
    }
    if (!context.mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Archive cleared.')));
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
