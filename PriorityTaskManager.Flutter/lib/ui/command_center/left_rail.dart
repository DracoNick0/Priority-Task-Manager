import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../providers/engine_status_provider.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../theme/app_theme.dart';

/// The Left Rail: list switcher, global nav (Settings/Archive), and the
/// Engine Status indicator (time simulation clock + algorithm mode).
class LeftRail extends ConsumerWidget {
  const LeftRail({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final colorScheme = Theme.of(context).colorScheme;
    final listsAsync = ref.watch(taskListsProvider);
    final activeListId = ref.watch(activeListIdProvider);

    return Container(
      color: colorScheme.surfaceContainerLow,
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(
              height: AppTheme.paneHeaderHeight,
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppTheme.spacingMd,
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(
                        'Priority Task Manager',
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.bold),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const Divider(height: 1),
            Expanded(
              child: listsAsync.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (error, _) => Center(child: Text('Error: $error')),
                data: (lists) => ListView(
                  padding: const EdgeInsets.symmetric(
                    vertical: AppTheme.spacingSm,
                  ),
                  children: [
                    Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: AppTheme.spacingMd,
                      ),
                      child: Text(
                        'LISTS',
                        style: Theme.of(context).textTheme.labelSmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ),
                    const SizedBox(height: AppTheme.spacingSm),
                    for (final list in lists)
                      _RailItem(
                        icon: Icons.list_alt,
                        label: list.name,
                        isSelected: list.id == activeListId,
                        onTap: () =>
                            ref.read(activeListIdProvider.notifier).state =
                                list.id,
                        onSettings: () =>
                            ref
                                .read(selectedInspectorProvider.notifier)
                                .state = InspectorTarget(
                              kind: InspectorKind.list,
                              id: list.id,
                            ),
                      ),
                    _RailItem(
                      icon: Icons.add,
                      label: 'New list',
                      isSelected: false,
                      onTap: () => _createList(context, ref),
                    ),
                  ],
                ),
              ),
            ),
            const Divider(height: 1),
            _RailItem(
              icon: Icons.archive_outlined,
              label: 'Archive',
              isSelected: false,
              onTap: () => ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Archive is not wired yet.')),
              ),
            ),
            _RailItem(
              icon: Icons.settings_outlined,
              label: 'Settings',
              isSelected: false,
              onTap: () => ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Settings is not wired yet.')),
              ),
            ),
            const _EngineStatus(),
          ],
        ),
      ),
    );
  }

  Future<void> _createList(BuildContext context, WidgetRef ref) async {
    final controller = TextEditingController();
    final name = await showDialog<String>(
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
            onPressed: () => Navigator.of(context).pop(controller.text),
            child: const Text('Create'),
          ),
        ],
      ),
    );
    if (name != null && name.trim().isNotEmpty) {
      await ref.read(taskListsProvider.notifier).createList(name.trim());
    }
  }
}

class _RailItem extends StatelessWidget {
  const _RailItem({
    required this.icon,
    required this.label,
    required this.isSelected,
    required this.onTap,
    this.onSettings,
  });

  final IconData icon;
  final String label;
  final bool isSelected;
  final VoidCallback onTap;
  final VoidCallback? onSettings;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Material(
      color: isSelected ? colorScheme.primaryContainer : Colors.transparent,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppTheme.spacingMd,
            vertical: AppTheme.spacingXs,
          ),
          child: Row(
            children: [
              Icon(
                icon,
                size: 18,
                color: isSelected
                    ? colorScheme.onPrimaryContainer
                    : colorScheme.onSurfaceVariant,
              ),
              const SizedBox(width: AppTheme.spacingSm),
              Expanded(
                child: Text(
                  label,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: isSelected
                        ? colorScheme.onPrimaryContainer
                        : colorScheme.onSurface,
                    fontWeight: isSelected
                        ? FontWeight.w600
                        : FontWeight.normal,
                  ),
                ),
              ),
              if (onSettings != null)
                IconButton(
                  icon: const Icon(Icons.more_horiz, size: 16),
                  onPressed: onSettings,
                  tooltip: 'List settings',
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _EngineStatus extends ConsumerWidget {
  const _EngineStatus();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final colorScheme = Theme.of(context).colorScheme;
    final clockAsync = ref.watch(engineClockProvider);
    final algorithmMode = ref.watch(algorithmModeProvider);

    return Container(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.4),
      ),
      child: Row(
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(
              color: colorScheme.secondary,
              shape: BoxShape.circle,
            ),
          ),
          const SizedBox(width: AppTheme.spacingSm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  clockAsync.when(
                    data: (time) => DateFormat.jm().format(time),
                    loading: () => '--:--',
                    error: (_, error) => '--:--',
                  ),
                  style: Theme.of(
                    context,
                  ).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
                ),
                Text(
                  algorithmMode,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: colorScheme.onSurfaceVariant,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
