import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/task_list.dart';
import '../../../providers/selection_provider.dart';
import '../../../providers/task_providers.dart';
import '../../theme/app_theme.dart';

/// Inline CRUD form for renaming/deleting a task list, shown in the Right
/// Inspector.
class ListInspectorForm extends ConsumerStatefulWidget {
  const ListInspectorForm({super.key, required this.listId});

  final String listId;

  @override
  ConsumerState<ListInspectorForm> createState() => _ListInspectorFormState();
}

class _ListInspectorFormState extends ConsumerState<ListInspectorForm> {
  late final TextEditingController _nameController;
  late final TextEditingController _descriptionController;
  TaskList? _loadedFrom;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController();
    _descriptionController = TextEditingController();
  }

  void _loadFrom(TaskList list) {
    if (identical(_loadedFrom, list)) return;
    _loadedFrom = list;
    _nameController.text = list.name;
    _descriptionController.text = list.description ?? '';
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];
    final list = lists.where((l) => l.id == widget.listId).firstOrNull;
    if (list == null) {
      return const Center(child: Text('List not found.'));
    }
    _loadFrom(list);

    return ListView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: [
        Text(
          'Edit List',
          style: Theme.of(
            context,
          ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _nameController,
          decoration: const InputDecoration(labelText: 'Name'),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        TextField(
          controller: _descriptionController,
          decoration: const InputDecoration(labelText: 'Description'),
          maxLines: 3,
        ),
        const SizedBox(height: AppTheme.spacingLg),
        Row(
          children: [
            Expanded(
              child: FilledButton(
                onPressed: () => _save(list),
                child: const Text('Save'),
              ),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            IconButton(
              icon: const Icon(Icons.delete_outline),
              tooltip: 'Delete list',
              onPressed: () => _delete(list),
            ),
          ],
        ),
      ],
    );
  }

  Future<void> _save(TaskList list) async {
    final name = _nameController.text.trim();
    if (name.isEmpty) return;
    await ref
        .read(taskListsProvider.notifier)
        .updateList(
          list.copyWith(
            name: name,
            description: _descriptionController.text.trim(),
          ),
        );
  }

  Future<void> _delete(TaskList list) async {
    await ref.read(taskListsProvider.notifier).deleteList(list.id);
    if (ref.read(activeListIdProvider) == list.id) {
      ref.read(activeListIdProvider.notifier).state = null;
    }
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget.none();
  }
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
