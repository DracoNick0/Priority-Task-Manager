import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../providers/app_notifications_provider.dart';
import '../../providers/engine_status_provider.dart';
import '../../providers/event_providers.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../theme/app_theme.dart';

/// Interactive onboarding hero shown in Center Stage when an active list has
/// zero tasks and zero events (resolves issue #77).
class EmptyScheduleHero extends ConsumerWidget {
  const EmptyScheduleHero({super.key, required this.listId});

  final String listId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(AppTheme.spacingLg),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 820),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Icon(
                Icons.calendar_today_outlined,
                size: 52,
                color: colorScheme.primary,
              ),
              const SizedBox(height: AppTheme.spacingMd),
              Text(
                'Welcome to Priority Task Manager',
                style: textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppTheme.spacingSm),
              Text(
                'Make a plan that fits the day you actually have. Choose an action to begin:',
                style: textTheme.bodyMedium?.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppTheme.spacingXl),
              LayoutBuilder(
                builder: (context, constraints) {
                  final isCompact = constraints.maxWidth < 640;
                  final cards = [
                    _ActionCard(
                      icon: Icons.rocket_launch_outlined,
                      title: 'Take a 60-Sec Tour',
                      description:
                          'See how smart planning splits work, protects slack, and schedules around commitments.',
                      buttonLabel: 'Start Tour',
                      onPressed: () => showOnboardingTourDialog(context),
                    ),
                    _ActionCard(
                      icon: Icons.add_task,
                      title: 'Add Your First Task',
                      description:
                          'Describe what needs doing and when it is due. We will schedule the rest.',
                      buttonLabel: '+ New Task',
                      onPressed: () {
                        ref.read(selectedInspectorProvider.notifier).state =
                            const InspectorTarget(
                              kind: InspectorKind.task,
                              id: null,
                            );
                      },
                    ),
                    _ActionCard(
                      icon: Icons.event_available,
                      title: 'Add a Fixed Event',
                      description:
                          'Record meetings, classes, lunch, or appointments. Work wraps around them.',
                      buttonLabel: '+ New Event',
                      onPressed: () {
                        ref.read(selectedInspectorProvider.notifier).state =
                            const InspectorTarget(
                              kind: InspectorKind.event,
                              id: null,
                            );
                      },
                    ),
                  ];

                  if (isCompact) {
                    return Column(
                      children: [
                        for (final card in cards) ...[
                          card,
                          const SizedBox(height: AppTheme.spacingMd),
                        ],
                      ],
                    );
                  }

                  return Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      for (int i = 0; i < cards.length; i++) ...[
                        if (i > 0) const SizedBox(width: AppTheme.spacingMd),
                        Expanded(child: cards[i]),
                      ],
                    ],
                  );
                },
              ),
              const SizedBox(height: AppTheme.spacingXl),
              OutlinedButton.icon(
                onPressed: () => _loadSampleSchedule(context, ref),
                icon: const Icon(Icons.auto_awesome_outlined, size: 18),
                label: const Text('Or load a 1-day sample schedule'),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppTheme.spacingLg,
                    vertical: AppTheme.spacingSm,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _loadSampleSchedule(BuildContext context, WidgetRef ref) async {
    final taskNotifier = ref.read(tasksProvider(listId).notifier);
    final eventNotifier = ref.read(eventsProvider(listId).notifier);
    final now = ref.read(engineClockProvider).asData?.value ?? DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final tomorrow = today.add(const Duration(days: 1));

    // Sample Task 1: High priority divisible task due today
    final task1 = await taskNotifier.addTask(
      title: 'Draft architecture proposal',
      description: 'Core design doc for the upcoming quarter',
      estimatedDurationMinutes: 90,
      importance: 8,
      complexity: 7,
      dueDate: DateTime(today.year, today.month, today.day, 17, 0),
      isDivisible: true,
    );

    // Sample Task 2: Review task due tomorrow
    final task2 = await taskNotifier.addTask(
      title: 'Review pull requests and tickets',
      description: 'Check team code reviews and unblock peers',
      estimatedDurationMinutes: 45,
      importance: 6,
      complexity: 4,
      dueDate: DateTime(tomorrow.year, tomorrow.month, tomorrow.day, 17, 0),
    );

    // Sample Task 3: Study / focus task due today
    final task3 = await taskNotifier.addTask(
      title: 'Study chapter 4 notes',
      description: 'Review slides and practice exercises',
      estimatedDurationMinutes: 60,
      importance: 7,
      complexity: 5,
      dueDate: DateTime(today.year, today.month, today.day, 17, 0),
    );

    // Sample Fixed Event: Meeting today
    final event1 = await eventNotifier.addEvent(
      title: 'Team / Class Sync',
      description: 'Fixed schedule discussion',
      startTime: DateTime(today.year, today.month, today.day, 10, 30),
      endTime: DateTime(today.year, today.month, today.day, 11, 30),
    );

    if (context.mounted) {
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            content: Text(
              'Loaded 1-day sample schedule',
              style: TextStyle(color: colorScheme.onInverseSurface),
            ),
            duration: const Duration(seconds: 8),
            action: SnackBarAction(
              label: 'Undo',
              textColor: colorScheme.inversePrimary,
              onPressed: () async {
                await taskNotifier.deleteTask(task1.id);
                await taskNotifier.deleteTask(task2.id);
                await taskNotifier.deleteTask(task3.id);
                await eventNotifier.deleteEvent(event1.id);
                ref.read(appNotificationProvider.notifier).state =
                    const AppNotification(
                      'Sample schedule removed',
                      icon: Icons.delete_outline,
                    );
              },
            ),
          ),
        );
    }
  }
}

class _ActionCard extends StatelessWidget {
  const _ActionCard({
    required this.icon,
    required this.title,
    required this.description,
    required this.buttonLabel,
    required this.onPressed,
  });

  final IconData icon;
  final String title;
  final String description;
  final String buttonLabel;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        side: BorderSide(color: colorScheme.outlineVariant),
      ),
      color: colorScheme.surfaceContainerLowest,
      child: Padding(
        padding: const EdgeInsets.all(AppTheme.spacingMd),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(AppTheme.spacingSm),
              decoration: BoxDecoration(
                color: colorScheme.primaryContainer.withAlpha(80),
                borderRadius: BorderRadius.circular(AppTheme.radiusSm),
              ),
              child: Icon(icon, size: 24, color: colorScheme.primary),
            ),
            const SizedBox(height: AppTheme.spacingMd),
            Text(
              title,
              style: textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: AppTheme.spacingXs),
            Text(
              description,
              style: textTheme.bodySmall?.copyWith(
                color: colorScheme.onSurfaceVariant,
                height: 1.4,
              ),
            ),
            const SizedBox(height: AppTheme.spacingMd),
            FilledButton.tonal(
              onPressed: onPressed,
              style: FilledButton.styleFrom(
                minimumSize: const Size(double.infinity, 36),
                padding: const EdgeInsets.symmetric(
                  horizontal: AppTheme.spacingSm,
                ),
              ),
              child: Text(buttonLabel),
            ),
          ],
        ),
      ),
    );
  }
}

/// Shows the 60-second interactive onboarding tour modal dialog.
Future<void> showOnboardingTourDialog(BuildContext context) {
  return showDialog<void>(
    context: context,
    builder: (context) => const OnboardingTourDialog(),
  );
}

/// A 3-step carousel modal explaining the core scheduling model:
/// Step 1: Effort Over Backlogs
/// Step 2: Intelligent Work Splitting
/// Step 3: Honest Slack & Risk Warnings
class OnboardingTourDialog extends StatefulWidget {
  const OnboardingTourDialog({super.key});

  @override
  State<OnboardingTourDialog> createState() => _OnboardingTourDialogState();
}

class _OnboardingTourDialogState extends State<OnboardingTourDialog> {
  int _currentStep = 0;
  static const int _totalSteps = 3;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Dialog(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusLg),
      ),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 580, maxHeight: 620),
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.spacingLg),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Header
              Row(
                children: [
                  Expanded(
                    child: Text(
                      'HOW IT WORKS (Step ${_currentStep + 1} of $_totalSteps)',
                      style: textTheme.labelMedium?.copyWith(
                        color: colorScheme.primary,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 1.1,
                      ),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 20),
                    tooltip: 'Skip Tour',
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
              const Divider(),
              const SizedBox(height: AppTheme.spacingSm),
              // Body
              Expanded(
                child: SingleChildScrollView(
                  child: _buildStepContent(context, _currentStep),
                ),
              ),
              const SizedBox(height: AppTheme.spacingMd),
              const Divider(),
              const SizedBox(height: AppTheme.spacingSm),
              // Footer
              Row(
                children: [
                  // Indicators (o) ( ) ( )
                  Row(
                    children: [
                      for (int i = 0; i < _totalSteps; i++) ...[
                        Container(
                          width: 8,
                          height: 8,
                          margin: const EdgeInsets.symmetric(horizontal: 3),
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: i == _currentStep
                                ? colorScheme.primary
                                : colorScheme.outlineVariant,
                          ),
                        ),
                      ],
                    ],
                  ),
                  const Spacer(),
                  if (_currentStep > 0)
                    TextButton(
                      onPressed: () => setState(() => _currentStep--),
                      child: const Text('Back'),
                    ),
                  const SizedBox(width: AppTheme.spacingSm),
                  FilledButton(
                    onPressed: () {
                      if (_currentStep < _totalSteps - 1) {
                        setState(() => _currentStep++);
                      } else {
                        Navigator.of(context).pop();
                      }
                    },
                    child: Text(
                      _currentStep == 0
                          ? 'Next: Work Splitting'
                          : _currentStep == 1
                          ? 'Next: Honest Slack'
                          : 'Get Started',
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStepContent(BuildContext context, int step) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    switch (step) {
      case 0:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.timer_outlined, color: colorScheme.primary, size: 28),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Text(
                    'Effort Over Backlogs',
                    style: textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTheme.spacingMd),
            Text(
              'Traditional to-do lists are endless wishlists. In Priority Task Manager, you assign realistic duration estimates (e.g. 45m, 2h) and deadlines.',
              style: textTheme.bodyMedium?.copyWith(height: 1.4),
            ),
            const SizedBox(height: AppTheme.spacingMd),
            Container(
              padding: const EdgeInsets.all(AppTheme.spacingMd),
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerLow,
                borderRadius: BorderRadius.circular(AppTheme.radiusMd),
                border: Border.all(color: colorScheme.outlineVariant),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.check_circle_outline,
                          size: 18, color: Colors.green),
                      const SizedBox(width: AppTheme.spacingSm),
                      const Text(
                        'Automated Placement',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppTheme.spacingXs),
                  Text(
                    'The engine analyzes your daily work hours and places tasks where they genuinely fit — no manual drag-and-drop Tetris required.',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ],
        );
      case 1:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.call_split, color: colorScheme.primary, size: 28),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Text(
                    'Intelligent Work Splitting',
                    style: textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTheme.spacingMd),
            Text(
              'Large tasks do not need an unbroken 4-hour block. Divisible tasks automatically split around meetings, classes, and lunch:',
              style: textTheme.bodyMedium?.copyWith(height: 1.4),
            ),
            const SizedBox(height: AppTheme.spacingMd),
            // Mock schedule timeline
            Container(
              padding: const EdgeInsets.all(AppTheme.spacingMd),
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerLow,
                borderRadius: BorderRadius.circular(AppTheme.radiusMd),
                border: Border.all(color: colorScheme.outlineVariant),
              ),
              child: Column(
                children: [
                  _mockTimelineItem(
                    context,
                    time: '10:00 AM',
                    title: 'Fixed Meeting (1h)',
                    isFixed: true,
                  ),
                  const SizedBox(height: 6),
                  _mockTimelineItem(
                    context,
                    time: '11:00 AM',
                    title: 'Feature Architecture: Part 1 (1.5h)',
                    isSplit: true,
                  ),
                  const SizedBox(height: 6),
                  _mockTimelineItem(
                    context,
                    time: '12:30 PM',
                    title: 'Lunch Break (1.0h)',
                    isFixed: true,
                  ),
                  const SizedBox(height: 6),
                  _mockTimelineItem(
                    context,
                    time: '01:30 PM',
                    title: 'Feature Architecture: Part 2 (1.5h)',
                    isSplit: true,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppTheme.spacingSm),
            Text(
              'Work flows around your commitments without losing progress.',
              style: textTheme.bodySmall?.copyWith(
                color: colorScheme.onSurfaceVariant,
                fontStyle: FontStyle.italic,
              ),
            ),
          ],
        );
      case 2:
      default:
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.shield_outlined,
                    color: colorScheme.primary, size: 28),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Text(
                    'Honest Slack & Risk Warnings',
                    style: textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTheme.spacingMd),
            Text(
              'No more hidden deadline surprises. Priority Task Manager calculates realistic slack based on actual work hours remaining before a deadline.',
              style: textTheme.bodyMedium?.copyWith(height: 1.4),
            ),
            const SizedBox(height: AppTheme.spacingMd),
            Container(
              padding: const EdgeInsets.all(AppTheme.spacingMd),
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerLow,
                borderRadius: BorderRadius.circular(AppTheme.radiusMd),
                border: Border.all(color: colorScheme.outlineVariant),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.warning_amber_rounded,
                          size: 18, color: Colors.amber),
                      const SizedBox(width: AppTheme.spacingSm),
                      const Text(
                        'Proactive Alerts',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppTheme.spacingXs),
                  Text(
                    'When work exceeds available capacity, the app warns you early so you can extend hours, defer lower-priority tasks, or re-plan with confidence.',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ],
        );
    }
  }

  Widget _mockTimelineItem(
    BuildContext context, {
    required String time,
    required String title,
    bool isFixed = false,
    bool isSplit = false,
  }) {
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: isFixed
            ? colorScheme.surfaceContainerHighest
            : colorScheme.primaryContainer.withAlpha(60),
        borderRadius: BorderRadius.circular(4),
        border: Border.all(
          color: isSplit ? colorScheme.primary : colorScheme.outlineVariant,
          width: isSplit ? 1.5 : 1,
        ),
      ),
      child: Row(
        children: [
          Text(
            time,
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              title,
              style: TextStyle(
                fontSize: 12,
                color: isFixed ? colorScheme.onSurfaceVariant : null,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          if (isSplit)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
              decoration: BoxDecoration(
                color: colorScheme.primary,
                borderRadius: BorderRadius.circular(3),
              ),
              child: Text(
                'Split',
                style: TextStyle(
                  fontSize: 10,
                  color: colorScheme.onPrimary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
        ],
      ),
    );
  }
}
