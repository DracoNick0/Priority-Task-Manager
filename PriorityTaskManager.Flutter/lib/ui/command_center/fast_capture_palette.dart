import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../models/task_list.dart';
import '../../providers/app_notifications_provider.dart';
import '../../providers/engine_status_provider.dart';
import '../../providers/event_providers.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../../providers/view_provider.dart';
import '../../utils/iterable_extensions.dart';
import '../../utils/working_day_defaults.dart';
import '../theme/app_theme.dart';
import 'empty_schedule_hero.dart';

/// Data structure representing candidate attributes for task creation.
/// Designed for Phase 1 smart defaults and Phase 2 LLM-assisted intake (Issue #18).
class CandidateTaskDetails {
  const CandidateTaskDetails({
    required this.title,
    required this.estimatedDurationMinutes,
    required this.dueDate,
    required this.importance,
    required this.listId,
    this.isDivisible = false,
    this.confidence,
  });

  final String title;
  final int estimatedDurationMinutes;
  final DateTime? dueDate;
  final int importance;
  final String listId;
  final bool isDivisible;
  final double? confidence;

  CandidateTaskDetails copyWith({
    String? title,
    int? estimatedDurationMinutes,
    DateTime? dueDate,
    int? importance,
    String? listId,
    bool? isDivisible,
    double? confidence,
  }) {
    return CandidateTaskDetails(
      title: title ?? this.title,
      estimatedDurationMinutes:
          estimatedDurationMinutes ?? this.estimatedDurationMinutes,
      dueDate: dueDate ?? this.dueDate,
      importance: importance ?? this.importance,
      listId: listId ?? this.listId,
      isDivisible: isDivisible ?? this.isDivisible,
      confidence: confidence ?? this.confidence,
    );
  }
}

/// Shows the universal Command Palette overlay dialog.
Future<void> showFastCapturePalette(BuildContext context) {
  return showDialog<void>(
    context: context,
    builder: (context) => const FastCapturePaletteDialog(),
  );
}

/// Universal Command Palette (`Ctrl + K` / `Cmd + K`) overlay.
class FastCapturePaletteDialog extends ConsumerStatefulWidget {
  const FastCapturePaletteDialog({super.key});

  @override
  ConsumerState<FastCapturePaletteDialog> createState() =>
      _FastCapturePaletteDialogState();
}

class _FastCapturePaletteDialogState
    extends ConsumerState<FastCapturePaletteDialog> {
  final _controller = TextEditingController();
  final _focusNode = FocusNode();

  late int _durationMinutes;
  late DateTime? _dueDate;
  late int _importance;
  late String? _selectedListId;

  @override
  void initState() {
    super.initState();
    _durationMinutes = 60;
    _importance = 5;

    final now = ref.read(engineClockProvider).asData?.value ?? DateTime.now();
    _dueDate = nextWorkingDayAtTime(
          currentTime: now,
          workDays: const [
            DateTime.monday,
            DateTime.tuesday,
            DateTime.wednesday,
            DateTime.thursday,
            DateTime.friday,
          ],
          minutesSinceMidnight: 17 * 60,
        ) ??
        DateTime(now.year, now.month, now.day + 1, 17, 0);

    _selectedListId = ref.read(activeListIdProvider);
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  CandidateTaskDetails get _candidateDetails => CandidateTaskDetails(
        title: _controller.text.trim(),
        estimatedDurationMinutes: _durationMinutes,
        dueDate: _dueDate,
        importance: _importance,
        listId: _selectedListId ?? '',
      );

  Future<void> _submitTask() async {
    final title = _controller.text.trim();
    if (title.isEmpty) return;

    final listId = _selectedListId ?? ref.read(activeListIdProvider);
    if (listId == null) return;

    final taskNotifier = ref.read(tasksProvider(listId).notifier);
    final createdTask = await taskNotifier.addTask(
      title: title,
      estimatedDurationMinutes: _durationMinutes,
      dueDate: _dueDate,
      importance: _importance,
    );

    if (mounted) {
      Navigator.of(context).pop();
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              'Added task "$title"',
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 6),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                await taskNotifier.deleteTask(createdTask.id);
                ref.read(appNotificationProvider.notifier).state =
                    const AppNotification(
                      'Task removed',
                      icon: Icons.delete_outline,
                    );
              },
            ),
          ),
        );
    }
  }

  Future<void> _submitEvent() async {
    final title = _controller.text.trim();
    if (title.isEmpty) return;

    final listId = _selectedListId ?? ref.read(activeListIdProvider);
    if (listId == null) return;

    final now = ref.read(engineClockProvider).asData?.value ?? DateTime.now();
    final start = nextWorkingHour(
          currentTime: now,
          workDays: const [
            DateTime.monday,
            DateTime.tuesday,
            DateTime.wednesday,
            DateTime.thursday,
            DateTime.friday,
          ],
          workStartMinutes: 9 * 60,
          workEndMinutes: 17 * 60,
        ) ??
        DateTime(now.year, now.month, now.day, 10, 0);
    final end = start.add(Duration(minutes: _durationMinutes));

    final eventNotifier = ref.read(eventsProvider(listId).notifier);
    final createdEvent = await eventNotifier.addEvent(
      title: title,
      startTime: start,
      endTime: end,
    );

    if (mounted) {
      Navigator.of(context).pop();
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              'Added event "$title"',
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 6),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                await eventNotifier.deleteEvent(createdEvent.id);
              },
            ),
          ),
        );
    }
  }

  void _openInInspector() {
    Navigator.of(context).pop();
    ref.read(selectedInspectorProvider.notifier).state =
        const InspectorTarget(kind: InspectorKind.task, id: null);
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final lists = ref.watch(taskListsProvider).asData?.value ?? const [];

    return Dialog(
      alignment: Alignment.topCenter,
      insetPadding: const EdgeInsets.only(top: 80, left: 24, right: 24),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusLg),
      ),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 680),
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.spacingMd),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Header
              Row(
                children: [
                  Icon(
                    Icons.flash_on_rounded,
                    size: 20,
                    color: colorScheme.primary,
                  ),
                  const SizedBox(width: AppTheme.spacingSm),
                  Text(
                    'Fast Capture & Command Palette',
                    style: textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const Spacer(),
                  Text(
                    'Esc to close',
                    style: textTheme.labelSmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(width: AppTheme.spacingXs),
                  IconButton(
                    icon: const Icon(Icons.close, size: 18),
                    onPressed: () => Navigator.of(context).pop(),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                  ),
                ],
              ),
              const SizedBox(height: AppTheme.spacingSm),
              // Search / Capture input
              KeyboardListener(
                focusNode: FocusNode(),
                onKeyEvent: (event) {
                  if (event is KeyDownEvent &&
                      event.logicalKey == LogicalKeyboardKey.enter) {
                    if (HardwareKeyboard.instance.isShiftPressed) {
                      _submitEvent();
                    } else if (HardwareKeyboard.instance.isAltPressed) {
                      _openInInspector();
                    }
                  }
                },
                child: TextField(
                  controller: _controller,
                  focusNode: _focusNode,
                  autofocus: true,
                  textInputAction: TextInputAction.done,
                  decoration: InputDecoration(
                    prefixIcon: const Icon(Icons.chevron_right, size: 24),
                    hintText:
                        'Type a task or event... Press Enter to schedule',
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(AppTheme.radiusMd),
                    ),
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: AppTheme.spacingMd,
                      vertical: AppTheme.spacingMd,
                    ),
                  ),
                  onSubmitted: (_) => _submitTask(),
                  onChanged: (_) => setState(() {}),
                ),
              ),
              const SizedBox(height: AppTheme.spacingSm),
              // Candidate Pills Preview (clickable micro-chips)
              CandidatePillsPreview(
                details: _candidateDetails,
                lists: lists,
                onDurationChanged: (duration) =>
                    setState(() => _durationMinutes = duration),
                onDueDateChanged: (due) => setState(() => _dueDate = due),
                onImportanceChanged: (importance) =>
                    setState(() => _importance = importance),
                onListChanged: (listId) =>
                    setState(() => _selectedListId = listId),
              ),
              const Divider(height: AppTheme.spacingLg),
              // Quick Actions / Hints
              Wrap(
                spacing: AppTheme.spacingMd,
                runSpacing: AppTheme.spacingXs,
                children: const [
                  _ShortcutHint(keyLabel: 'Enter', description: 'Add Task'),
                  _ShortcutHint(
                    keyLabel: 'Shift+Enter',
                    description: 'Add Event',
                  ),
                  _ShortcutHint(
                    keyLabel: 'Alt+Enter',
                    description: 'Inspector',
                  ),
                ],
              ),
              const SizedBox(height: AppTheme.spacingSm),
              // Quick Commands
              const Divider(height: AppTheme.spacingMd),
              Text(
                'QUICK COMMANDS',
                style: textTheme.labelSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: colorScheme.onSurfaceVariant,
                  letterSpacing: 1.1,
                ),
              ),
              const SizedBox(height: AppTheme.spacingXs),
              _CommandTile(
                icon: Icons.today_outlined,
                title: 'Switch view to: Today Execution View',
                onTap: () {
                  ref.read(viewModeProvider.notifier).state = ViewMode.today;
                  Navigator.of(context).pop();
                },
              ),
              _CommandTile(
                icon: Icons.calendar_view_week_outlined,
                title: 'Switch view to: Week Horizon Grid',
                onTap: () {
                  ref.read(viewModeProvider.notifier).state = ViewMode.horizon;
                  Navigator.of(context).pop();
                },
              ),
              _CommandTile(
                icon: Icons.folder_outlined,
                title: 'Switch view to: Projects Backlog',
                onTap: () {
                  ref.read(viewModeProvider.notifier).state = ViewMode.projects;
                  Navigator.of(context).pop();
                },
              ),
              _CommandTile(
                icon: Icons.rocket_launch_outlined,
                title: 'Take a 60-Sec Tour',
                onTap: () {
                  Navigator.of(context).pop();
                  showOnboardingTourDialog(context);
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Candidate pills preview bar: renders clickable micro-chips.
/// Directly accepts candidate models from MVP LLM intake (Issue #18).
class CandidatePillsPreview extends StatelessWidget {
  const CandidatePillsPreview({
    super.key,
    required this.details,
    required this.lists,
    required this.onDurationChanged,
    required this.onDueDateChanged,
    required this.onImportanceChanged,
    required this.onListChanged,
  });

  final CandidateTaskDetails details;
  final List<TaskList> lists;
  final ValueChanged<int> onDurationChanged;
  final ValueChanged<DateTime?> onDueDateChanged;
  final ValueChanged<int> onImportanceChanged;
  final ValueChanged<String> onListChanged;

  String _formatDuration(int minutes) {
    if (minutes <= 60) return '${minutes}m';
    final hours = minutes / 60;
    return hours == hours.roundToDouble()
        ? '${hours.toInt()}h'
        : '${hours.toStringAsFixed(1)}h';
  }

  String _formatDueDate(DateTime? date) {
    if (date == null) return 'No deadline';
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final tomorrow = today.add(const Duration(days: 1));
    final target = DateTime(date.year, date.month, date.day);

    final timeStr =
        '${date.hour > 12 ? date.hour - 12 : date.hour == 0 ? 12 : date.hour}:${date.minute.toString().padLeft(2, '0')} ${date.hour >= 12 ? 'PM' : 'AM'}';
    if (target == today) return 'Today $timeStr';
    if (target == tomorrow) return 'Tomorrow $timeStr';
    return '${date.month}/${date.day} $timeStr';
  }

  String _importanceLabel(int importance) {
    if (importance >= 9) return 'Critical ($importance)';
    if (importance >= 7) return 'High ($importance)';
    if (importance >= 4) return 'Normal ($importance)';
    return 'Low ($importance)';
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final activeList =
        lists.where((l) => l.id == details.listId).firstOrNull;

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          // Duration Chip
          PopupMenuButton<int>(
            tooltip: 'Change Duration',
            onSelected: onDurationChanged,
            itemBuilder: (context) => [
              const PopupMenuItem(value: 15, child: Text('15m (Quick task)')),
              const PopupMenuItem(value: 30, child: Text('30m')),
              const PopupMenuItem(value: 45, child: Text('45m')),
              const PopupMenuItem(value: 60, child: Text('60m (Default)')),
              const PopupMenuItem(value: 90, child: Text('90m (1.5h)')),
              const PopupMenuItem(value: 120, child: Text('2h')),
              const PopupMenuItem(value: 180, child: Text('3h')),
              const PopupMenuItem(value: 240, child: Text('4h')),
            ],
            child: Chip(
              avatar: const Icon(Icons.timer_outlined, size: 16),
              label: Text(_formatDuration(details.estimatedDurationMinutes)),
              visualDensity: VisualDensity.compact,
            ),
          ),
          const SizedBox(width: AppTheme.spacingXs),
          // Due Date Chip
          PopupMenuButton<int>(
            tooltip: 'Change Due Date',
            onSelected: (option) {
              final now = DateTime.now();
              switch (option) {
                case 1: // Today 5 PM
                  onDueDateChanged(
                    DateTime(now.year, now.month, now.day, 17, 0),
                  );
                  break;
                case 2: // Tomorrow 5 PM
                  onDueDateChanged(
                    DateTime(now.year, now.month, now.day + 1, 17, 0),
                  );
                  break;
                case 3: // End of Week (Friday 5 PM)
                  final daysUntilFri = (DateTime.friday - now.weekday) % 7;
                  onDueDateChanged(
                    DateTime(now.year, now.month, now.day + daysUntilFri, 17, 0),
                  );
                  break;
                case 4: // None
                  onDueDateChanged(null);
                  break;
              }
            },
            itemBuilder: (context) => const [
              PopupMenuItem(value: 1, child: Text('Today 5:00 PM')),
              PopupMenuItem(value: 2, child: Text('Tomorrow 5:00 PM')),
              PopupMenuItem(value: 3, child: Text('End of Week (Friday)')),
              PopupMenuItem(value: 4, child: Text('No Due Date')),
            ],
            child: Chip(
              avatar: const Icon(Icons.event_outlined, size: 16),
              label: Text('Due: ${_formatDueDate(details.dueDate)}'),
              visualDensity: VisualDensity.compact,
            ),
          ),
          const SizedBox(width: AppTheme.spacingXs),
          // Importance Chip
          PopupMenuButton<int>(
            tooltip: 'Change Importance',
            onSelected: onImportanceChanged,
            itemBuilder: (context) => const [
              PopupMenuItem(value: 3, child: Text('Low (3)')),
              PopupMenuItem(value: 5, child: Text('Normal (5)')),
              PopupMenuItem(value: 8, child: Text('High (8)')),
              PopupMenuItem(value: 10, child: Text('Critical (10)')),
            ],
            child: Chip(
              avatar: const Icon(Icons.priority_high, size: 16),
              label: Text(_importanceLabel(details.importance)),
              visualDensity: VisualDensity.compact,
            ),
          ),
          if (lists.isNotEmpty) ...[
            const SizedBox(width: AppTheme.spacingXs),
            PopupMenuButton<String>(
              tooltip: 'Change List',
              onSelected: onListChanged,
              itemBuilder: (context) => [
                for (final list in lists)
                  PopupMenuItem(
                    value: list.id,
                    child: Text(list.name),
                  ),
              ],
              child: Chip(
                avatar: const Icon(Icons.list_alt, size: 16),
                label: Text('List: ${activeList?.name ?? 'Active'}'),
                visualDensity: VisualDensity.compact,
              ),
            ),
          ],
          if (details.confidence != null) ...[
            const SizedBox(width: AppTheme.spacingXs),
            Chip(
              avatar: Icon(
                Icons.auto_awesome,
                size: 14,
                color: colorScheme.primary,
              ),
              label: Text(
                'AI ${(details.confidence! * 100).toInt()}%',
                style: TextStyle(color: colorScheme.primary, fontSize: 11),
              ),
              visualDensity: VisualDensity.compact,
            ),
          ],
        ],
      ),
    );
  }
}

/// Persistent compact inline quick-add input at the top of Center Stage views.
class InlineFastCaptureBar extends ConsumerStatefulWidget {
  const InlineFastCaptureBar({super.key, required this.listId});

  final String listId;

  @override
  ConsumerState<InlineFastCaptureBar> createState() =>
      _InlineFastCaptureBarState();
}

class _InlineFastCaptureBarState extends ConsumerState<InlineFastCaptureBar> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final title = _controller.text.trim();
    if (title.isEmpty) return;

    _controller.clear();
    final taskNotifier = ref.read(tasksProvider(widget.listId).notifier);
    final now = ref.read(engineClockProvider).asData?.value ?? DateTime.now();
    final dueDate = nextWorkingDayAtTime(
          currentTime: now,
          workDays: const [
            DateTime.monday,
            DateTime.tuesday,
            DateTime.wednesday,
            DateTime.thursday,
            DateTime.friday,
          ],
          minutesSinceMidnight: 17 * 60,
        ) ??
        DateTime(now.year, now.month, now.day + 1, 17, 0);

    final createdTask = await taskNotifier.addTask(
      title: title,
      estimatedDurationMinutes: 60,
      dueDate: dueDate,
      importance: 5,
    );

    if (mounted) {
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              'Added task "$title"',
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 6),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                await taskNotifier.deleteTask(createdTask.id);
              },
            ),
          ),
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppTheme.spacingMd,
        vertical: AppTheme.spacingSm,
      ),
      decoration: BoxDecoration(
        color: colorScheme.surface,
        border: Border(
          bottom: BorderSide(color: colorScheme.outlineVariant),
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: _controller,
              textInputAction: TextInputAction.done,
              decoration: InputDecoration(
                isDense: true,
                prefixIcon: const Icon(Icons.add, size: 20),
                hintText:
                    'Add task (e.g. "Math homework due tomorrow 5pm"). Press Enter to schedule',
                hintStyle: TextStyle(
                  color: colorScheme.onSurfaceVariant.withAlpha(160),
                  fontSize: 13,
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: AppTheme.spacingSm,
                  vertical: AppTheme.spacingSm,
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(AppTheme.radiusMd),
                  borderSide: BorderSide(color: colorScheme.outlineVariant),
                ),
                filled: true,
                fillColor: colorScheme.surfaceContainerLowest,
              ),
              onSubmitted: (_) => _submit(),
            ),
          ),
          const SizedBox(width: AppTheme.spacingSm),
          IconButton(
            icon: const Icon(Icons.keyboard_command_key, size: 18),
            tooltip: 'Command Palette (Ctrl + K)',
            onPressed: () => showFastCapturePalette(context),
          ),
        ],
      ),
    );
  }
}

class _ShortcutHint extends StatelessWidget {
  const _ShortcutHint({required this.keyLabel, required this.description});

  final String keyLabel;
  final String description;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
          decoration: BoxDecoration(
            color: colorScheme.surfaceContainerHigh,
            borderRadius: BorderRadius.circular(4),
            border: Border.all(color: colorScheme.outlineVariant),
          ),
          child: Text(
            keyLabel,
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 11),
          ),
        ),
        const SizedBox(width: 4),
        Text(
          description,
          style: TextStyle(
            fontSize: 11,
            color: colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}

class _CommandTile extends StatelessWidget {
  const _CommandTile({
    required this.icon,
    required this.title,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon, size: 18),
      title: Text(title, style: const TextStyle(fontSize: 13)),
      dense: true,
      visualDensity: VisualDensity.compact,
      contentPadding: const EdgeInsets.symmetric(horizontal: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusSm),
      ),
      onTap: onTap,
    );
  }
}
