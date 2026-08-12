import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../theme/app_theme.dart';
import 'inspector_forms/event_inspector_form.dart';
import 'inspector_forms/list_inspector_form.dart';
import 'inspector_forms/task_inspector_form.dart';

/// The Right Inspector: shows the full CRUD form for whatever task, event,
/// or list is currently selected via [selectedInspectorProvider].
class RightInspector extends ConsumerWidget {
  const RightInspector({super.key, this.onClose});

  /// If provided, shows a close button (used when the inspector is a
  /// detached overlay/EndDrawer rather than a docked panel).
  final VoidCallback? onClose;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selection = ref.watch(selectedInspectorProvider);
    final activeListId = ref.watch(activeListIdProvider);
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      color: colorScheme.surface,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.all(AppTheme.spacingMd),
            child: Row(
              children: [
                const Expanded(
                  child: Text(
                    'Inspector',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                if (onClose != null)
                  IconButton(icon: const Icon(Icons.close), onPressed: onClose),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 200),
              child: _buildContent(context, selection, activeListId),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildContent(
    BuildContext context,
    InspectorTarget selection,
    String? activeListId,
  ) {
    switch (selection.kind) {
      case InspectorKind.none:
        return _EmptyInspector(key: const ValueKey('empty'));
      case InspectorKind.task:
        if (activeListId == null) return const SizedBox.shrink();
        return TaskInspectorForm(
          key: ValueKey('task-${selection.id}'),
          listId: activeListId,
          taskId: selection.id,
        );
      case InspectorKind.event:
        if (activeListId == null) return const SizedBox.shrink();
        return EventInspectorForm(
          key: ValueKey('event-${selection.id}'),
          listId: activeListId,
          eventId: selection.id,
        );
      case InspectorKind.list:
        return ListInspectorForm(
          key: ValueKey('list-${selection.id}'),
          listId: selection.id!,
        );
    }
  }
}

class _EmptyInspector extends ConsumerWidget {
  const _EmptyInspector({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final activeListId = ref.watch(activeListIdProvider);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingLg),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.touch_app_outlined,
              size: 32,
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
            const SizedBox(height: AppTheme.spacingMd),
            Text(
              'Select a task, event, or list\nto see details here.',
              textAlign: TextAlign.center,
              style: TextStyle(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
            if (activeListId != null) ...[
              const SizedBox(height: AppTheme.spacingLg),
              FilledButton.icon(
                onPressed: () =>
                    ref.read(selectedInspectorProvider.notifier).state =
                        const InspectorTarget(kind: InspectorKind.task),
                icon: const Icon(Icons.add),
                label: const Text('New Task'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
