import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../providers/dev_log_provider.dart';
import '../../providers/engine_status_provider.dart';
import '../../providers/selection_provider.dart';
import '../../providers/session_provider.dart';
import '../../providers/task_providers.dart';
import '../../utils/iterable_extensions.dart';
import '../auth/login_screen.dart';
import '../theme/app_theme.dart';
import 'archive_dialog.dart';
import 'inspector_forms/combined_date_time_picker.dart';

/// The Left Rail: list switcher, global nav (Settings/Archive), and the
/// Engine Status indicator (time simulation clock + algorithm mode).
class LeftRail extends ConsumerWidget {
  const LeftRail({super.key, this.dockAction});

  /// Optional leading action (e.g. a "dock panel" button) shown alongside
  /// the title, used when the rail is displayed in an undocked Drawer.
  final Widget? dockAction;

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
                    if (dockAction != null) ...[
                      dockAction!,
                      const SizedBox(width: AppTheme.spacingSm),
                    ],
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
              onTap: () => _openArchive(context, ref),
            ),
            _RailItem(
              icon: Icons.settings_outlined,
              label: 'Settings',
              isSelected: false,
              onTap: () => ref.read(selectedInspectorProvider.notifier).state =
                  const InspectorTarget(kind: InspectorKind.defaults),
            ),
            if (kDebugMode)
              _RailItem(
                icon: Icons.bug_report_outlined,
                label: 'Dev Log',
                isSelected: ref.watch(devLogPanelOpenProvider),
                onTap: () => ref.read(devLogPanelOpenProvider.notifier).state =
                    !ref.read(devLogPanelOpenProvider),
              ),
            const Divider(height: 1),
            const _AccountRow(),
            const _EngineStatus(),
          ],
        ),
      ),
    );
  }

  Future<void> _openArchive(BuildContext context, WidgetRef ref) async {
    final isAuthenticated =
        ref.read(sessionControllerProvider).asData?.value.status ==
        SessionStatus.authenticated;
    if (!isAuthenticated) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Archive is an online-exclusive feature. Log in to use it.',
          ),
        ),
      );
      return;
    }
    await showArchiveDialog(context);
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

class _AccountRow extends ConsumerWidget {
  const _AccountRow();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final colorScheme = Theme.of(context).colorScheme;
    final session = ref.watch(sessionControllerProvider).valueOrNull;
    final isAuthenticated = session?.status == SessionStatus.authenticated;

    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppTheme.spacingMd,
        vertical: AppTheme.spacingSm,
      ),
      child: Row(
        children: [
          Icon(
            isAuthenticated ? Icons.person : Icons.person_outline,
            size: 18,
            color: colorScheme.onSurfaceVariant,
          ),
          const SizedBox(width: AppTheme.spacingSm),
          Expanded(
            child: Text(
              isAuthenticated ? (session?.email ?? 'Signed in') : 'Guest',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          TextButton(
            onPressed: () => isAuthenticated
                ? ref.read(sessionControllerProvider.notifier).logout()
                : Navigator.of(context).push(
                    MaterialPageRoute(builder: (_) => const LoginScreen()),
                  ),
            child: Text(isAuthenticated ? 'Log out' : 'Log in'),
          ),
        ],
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
    final isSimulated = ref.watch(isSimulatedTimeProvider);
    final session = ref.watch(sessionControllerProvider).asData?.value;
    final isAuthenticated = session?.status == SessionStatus.authenticated;
    final activeListId = ref.watch(activeListIdProvider);
    final canSetSimulatedTime = isAuthenticated && activeListId != null;

    return InkWell(
      onTap: canSetSimulatedTime
          ? () => _pickSimulatedTime(
              context,
              ref,
              activeListId,
              clockAsync.asData?.value,
            )
          : null,
      child: Container(
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
                color: isSimulated
                    ? colorScheme.tertiary
                    : colorScheme.secondary,
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: AppTheme.spacingSm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(
                        clockAsync.when(
                          data: (time) => isSimulated
                              ? DateFormat('MMM d, h:mm a').format(time)
                              : DateFormat.jm().format(time),
                          loading: () => '--:--',
                          error: (_, error) => '--:--',
                        ),
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      if (isSimulated) ...[
                        const SizedBox(width: AppTheme.spacingXs),
                        Tooltip(
                          message: 'Frozen simulated time, not real time',
                          child: Icon(
                            Icons.science_outlined,
                            size: 14,
                            color: colorScheme.tertiary,
                          ),
                        ),
                      ],
                      if (canSetSimulatedTime) ...[
                        const SizedBox(width: AppTheme.spacingXs),
                        Icon(
                          Icons.edit_calendar_outlined,
                          size: 14,
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ],
                    ],
                  ),
                  Text(
                    algorithmMode,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: isSimulated
                          ? colorScheme.tertiary
                          : colorScheme.onSurfaceVariant,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickSimulatedTime(
    BuildContext context,
    WidgetRef ref,
    String activeListId,
    DateTime? currentTime,
  ) async {
    final lists = ref.read(taskListsProvider).asData?.value ?? const [];
    final list = lists.where((l) => l.id == activeListId).firstOrNull;
    if (list == null) return;
    final initial =
        list.simulatedTime ??
        list.lastSimulatedTime ??
        currentTime ??
        DateTime.now();
    final result =
        await showCombinedDateTimePicker<CombinedDateTimePickerResult>(
          context,
          initialDateTime: initial,
          subtitle: 'Set simulated time',
          showEnabledToggle: true,
          initialEnabled: list.simulatedTime != null,
          enabledToggleLabel: 'Simulated time',
        );
    if (result == null || !context.mounted) return;

    if (!result.enabled) {
      await ref
          .read(taskListsProvider.notifier)
          .updateList(list.copyWith(clearSimulatedTime: true));
      return;
    }

    await ref
        .read(taskListsProvider.notifier)
        .updateList(list.copyWith(simulatedTime: result.dateTime));
  }
}
