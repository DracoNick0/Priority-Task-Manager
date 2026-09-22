import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../dev/dev_log_entry.dart';
import '../../providers/dev_log_provider.dart';
import '../theme/app_theme.dart';

/// Dev-only, bottom-docked panel (issue #59) listing recent API calls and
/// domain events recorded by `DevLogSink`, in the same spirit as an IDE's
/// docked terminal/debug console. Embedded (not pushed as a route) by
/// `command_center_screen.dart`, toggled via the Left Rail's "Dev Log" item
/// and only ever shown behind a `kDebugMode` check.
class DevLogPanel extends ConsumerStatefulWidget {
  const DevLogPanel({super.key, required this.onClose});

  final VoidCallback onClose;

  @override
  ConsumerState<DevLogPanel> createState() => _DevLogPanelState();
}

class _DevLogPanelState extends ConsumerState<DevLogPanel> {
  final _searchController = TextEditingController();
  DevLogKind? _kindFilter;
  bool _errorsOnly = false;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  List<DevLogEntry> _applyFilter(List<DevLogEntry> entries) {
    final query = _searchController.text.trim().toLowerCase();
    return entries.where((entry) {
      if (_kindFilter != null && entry.kind != _kindFilter) return false;
      if (_errorsOnly && !entry.isError) return false;
      if (query.isEmpty) return true;
      return entry.label.toLowerCase().contains(query) ||
          (entry.detail?.toLowerCase().contains(query) ?? false);
    }).toList();
  }

  Future<void> _copyEntries(List<DevLogEntry> entries) async {
    final text = entries.map(_formatEntry).join('\n\n');
    await Clipboard.setData(ClipboardData(text: text));
    if (!mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text('Copied ${entries.length} entries')));
  }

  String _formatEntry(DevLogEntry entry) {
    final header =
        '[${entry.timestamp.toIso8601String()}] ${entry.label} '
        '(${entry.durationMs ?? 0}ms)';
    return entry.detail == null ? header : '$header\n${entry.detail}';
  }

  @override
  Widget build(BuildContext context) {
    final sink = ref.watch(devLogEntriesProvider);
    final entries = _applyFilter(sink.entries);
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
                    icon: const Icon(Icons.copy_all_outlined),
                    tooltip: 'Copy filtered entries',
                    onPressed: entries.isEmpty
                        ? null
                        : () => _copyEntries(entries),
                  ),
                  IconButton(
                    icon: const Icon(Icons.delete_sweep_outlined),
                    tooltip: 'Clear',
                    onPressed: sink.clear,
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    tooltip: 'Close',
                    onPressed: widget.onClose,
                  ),
                ],
              ),
            ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppTheme.spacingMd,
              vertical: AppTheme.spacingSm,
            ),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _searchController,
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(
                      isDense: true,
                      prefixIcon: Icon(Icons.search, size: 18),
                      hintText: 'Filter by label or detail…',
                      border: OutlineInputBorder(),
                    ),
                  ),
                ),
                const SizedBox(width: AppTheme.spacingSm),
                _FilterChip(
                  label: 'All',
                  selected: _kindFilter == null,
                  onTap: () => setState(() => _kindFilter = null),
                ),
                const SizedBox(width: AppTheme.spacingXs),
                _FilterChip(
                  label: 'HTTP',
                  selected: _kindFilter == DevLogKind.httpCall,
                  onTap: () =>
                      setState(() => _kindFilter = DevLogKind.httpCall),
                ),
                const SizedBox(width: AppTheme.spacingXs),
                _FilterChip(
                  label: 'Events',
                  selected: _kindFilter == DevLogKind.domainEvent,
                  onTap: () =>
                      setState(() => _kindFilter = DevLogKind.domainEvent),
                ),
                const SizedBox(width: AppTheme.spacingXs),
                FilterChip(
                  label: const Text('Errors'),
                  selected: _errorsOnly,
                  onSelected: (value) => setState(() => _errorsOnly = value),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: entries.isEmpty
                ? Center(
                    child: Text(
                      sink.entries.isEmpty
                          ? 'No calls or events logged yet.'
                          : 'No entries match the current filter.',
                    ),
                  )
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

class _FilterChip extends StatelessWidget {
  const _FilterChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ChoiceChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) => onTap(),
    );
  }
}

class _DevLogTile extends StatelessWidget {
  const _DevLogTile(this.entry);

  final DevLogEntry entry;

  Future<void> _showDetail(BuildContext context) async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(entry.label),
        content: SingleChildScrollView(
          child: SelectableText(
            entry.detail ?? '(no detail captured)',
            style: Theme.of(context).textTheme.bodySmall,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () async {
              await Clipboard.setData(
                ClipboardData(text: entry.detail ?? entry.label),
              );
              if (context.mounted) Navigator.of(context).pop();
            },
            child: const Text('Copy'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final durationColor = entry.isSlow
        ? colorScheme.tertiary
        : colorScheme.onSurfaceVariant;
    return ListTile(
      dense: true,
      onTap: () => _showDetail(context),
      leading: Icon(
        entry.kind == DevLogKind.httpCall ? Icons.cloud_outlined : Icons.bolt,
        color: entry.isError ? colorScheme.error : colorScheme.primary,
      ),
      title: Text(entry.label),
      subtitle: entry.detail == null
          ? null
          : Text(entry.detail!, maxLines: 2, overflow: TextOverflow.ellipsis),
      trailing: Text(
        '${entry.isSlow ? '⚠ ' : ''}${entry.durationMs ?? 0}ms\n'
        '${TimeOfDay.fromDateTime(entry.timestamp).format(context)}',
        textAlign: TextAlign.right,
        style: Theme.of(
          context,
        ).textTheme.labelSmall?.copyWith(color: durationColor),
      ),
    );
  }
}
