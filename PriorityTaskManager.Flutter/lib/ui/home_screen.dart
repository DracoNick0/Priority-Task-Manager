import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/task_list.dart';
import '../providers/task_providers.dart';
import 'widgets/task_form_dialog.dart';
import 'widgets/task_tile.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final listsAsync = ref.watch(taskListsProvider);

    return listsAsync.when(
      loading: () =>
          const Scaffold(body: Center(child: CircularProgressIndicator())),
      error: (error, stackTrace) =>
          Scaffold(body: Center(child: Text('Failed to load lists: $error'))),
      data: (lists) {
        final activeListId =
            ref.watch(activeListIdProvider) ??
            (lists.isNotEmpty ? lists.first.id : null);

        if (activeListId != null && ref.read(activeListIdProvider) == null) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            ref.read(activeListIdProvider.notifier).state = activeListId;
          });
        }

        final activeList = lists
            .where((list) => list.id == activeListId)
            .firstOrNull;

        return Scaffold(
          appBar: AppBar(
            title: Text(activeList?.name ?? 'Priority Task Manager'),
          ),
          drawer: _ListsDrawer(lists: lists, activeListId: activeListId),
          body: activeListId == null
              ? const Center(child: Text('Create a list to get started.'))
              : _TaskListView(listId: activeListId),
          floatingActionButton: activeListId == null
              ? null
              : FloatingActionButton(
                  onPressed: () =>
                      _showAddTaskDialog(context, ref, activeListId),
                  tooltip: 'Add task',
                  child: const Icon(Icons.add),
                ),
        );
      },
    );
  }

  void _showAddTaskDialog(BuildContext context, WidgetRef ref, String listId) {
    showDialog<void>(
      context: context,
      builder: (context) => TaskFormDialog(listId: listId),
    );
  }
}

class _ListsDrawer extends ConsumerWidget {
  const _ListsDrawer({required this.lists, required this.activeListId});

  final List<TaskList> lists;
  final String? activeListId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Drawer(
      child: SafeArea(
        child: Column(
          children: [
            const DrawerHeader(
              child: Text('Lists', style: TextStyle(fontSize: 20)),
            ),
            Expanded(
              child: ListView(
                children: [
                  for (final list in lists)
                    ListTile(
                      title: Text(list.name),
                      selected: list.id == activeListId,
                      onTap: () {
                        ref.read(activeListIdProvider.notifier).state = list.id;
                        Navigator.of(context).pop();
                      },
                      trailing: lists.length > 1
                          ? IconButton(
                              icon: const Icon(Icons.delete_outline),
                              onPressed: () =>
                                  _confirmDeleteList(context, ref, list),
                            )
                          : null,
                    ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(8.0),
              child: FilledButton.icon(
                onPressed: () => _showCreateListDialog(context, ref),
                icon: const Icon(Icons.add),
                label: const Text('New list'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showCreateListDialog(BuildContext context, WidgetRef ref) {
    final controller = TextEditingController();
    showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('New list'),
        content: TextField(
          controller: controller,
          autofocus: true,
          decoration: const InputDecoration(labelText: 'Name'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () async {
              final name = controller.text.trim();
              if (name.isEmpty) return;
              await ref.read(taskListsProvider.notifier).createList(name);
              if (context.mounted) Navigator.of(context).pop();
            },
            child: const Text('Create'),
          ),
        ],
      ),
    );
  }

  void _confirmDeleteList(BuildContext context, WidgetRef ref, TaskList list) {
    showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete list'),
        content: Text('Delete "${list.name}" and all of its tasks?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () async {
              await ref.read(taskListsProvider.notifier).deleteList(list.id);
              if (context.mounted) Navigator.of(context).pop();
            },
            child: const Text('Delete'),
          ),
        ],
      ),
    );
  }
}

class _TaskListView extends ConsumerWidget {
  const _TaskListView({required this.listId});

  final String listId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tasksAsync = ref.watch(tasksProvider(listId));

    return tasksAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, stackTrace) =>
          Center(child: Text('Failed to load tasks: $error')),
      data: (tasks) {
        if (tasks.isEmpty) {
          return const Center(child: Text('No tasks yet. Tap + to add one.'));
        }
        final tasksById = {for (final task in tasks) task.id: task};
        return ListView.builder(
          itemCount: tasks.length,
          itemBuilder: (context, index) {
            final task = tasks[index];
            return TaskTile(
              task: task,
              dependencyTitles: task.dependencies
                  .map((id) => tasksById[id]?.title)
                  .whereType<String>()
                  .toList(),
              onToggleCompleted: (isCompleted) => ref
                  .read(tasksProvider(listId).notifier)
                  .setCompleted(task.id, isCompleted),
              onTap: () => showDialog<void>(
                context: context,
                builder: (context) =>
                    TaskFormDialog(listId: listId, task: task),
              ),
              onDelete: () =>
                  ref.read(tasksProvider(listId).notifier).deleteTask(task.id),
            );
          },
        );
      },
    );
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
