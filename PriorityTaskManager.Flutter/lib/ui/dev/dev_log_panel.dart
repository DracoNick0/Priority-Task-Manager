import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../dev/dev_log_entry.dart';
import '../../providers/dev_log_provider.dart';
import '../theme/app_theme.dart';

/// Dev-only, bottom-docked panel (issue #59) listing recent API calls and
/// domain events recorded by `DevLogSink`, in the same spirit as an IDE's
/// docked terminal/debug console. Embedded (not pushed as a route) by
/// `command_center_screen.dart`, toggled via the Left Rail's "Dev Log" item
/// and only ever shown behind a `kDebugMode` check.
class DevLogPanel extends ConsumerWidget {
  const DevLogPanel({super.key, required this.onClose});

  final VoidCallback onClose;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final sink = ref.watch(devLogEntriesProvider);
    final entries = sink.entries;
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      color: colorScheme.surface,
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
                  Icon(Icons.bug_report_outlined, color: colorScheme.primary),
                  const SizedBox(width: AppTheme.spacingSm),
                  Expanded(
                    child: Text(
                      'Dev Log',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.delete_sweep_outlined),
                    tooltip: 'Clear',
                    onPressed: sink.clear,
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    tooltip: 'Close',
                    onPressed: onClose,
                  ),
                ],
              ),
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: entries.isEmpty
                ? const Center(child: Text('No calls or events logged yet.'))
                : ListView.separated(
                    itemCount: entries.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) =>
                        _DevLogTile(entries[index]),
                  ),
          ),
        ],
      ),
    );
  }
}

class _DevLogTile extends StatelessWidget {
  const _DevLogTile(this.entry);

  final DevLogEntry entry;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return ListTile(
      dense: true,
      leading: Icon(
        entry.kind == DevLogKind.httpCall ? Icons.cloud_outlined : Icons.bolt,
        color: entry.isError ? colorScheme.error : colorScheme.primary,
      ),
      title: Text(entry.label),
      subtitle: entry.detail == null
          ? null
          : Text(entry.detail!, maxLines: 2, overflow: TextOverflow.ellipsis),
      trailing: Text(
        '${entry.durationMs ?? 0}ms\n${TimeOfDay.fromDateTime(entry.timestamp).format(context)}',
        textAlign: TextAlign.right,
        style: Theme.of(context).textTheme.labelSmall,
      ),
    );
  }
}
