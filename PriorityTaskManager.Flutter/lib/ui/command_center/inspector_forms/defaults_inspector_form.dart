import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../models/user_profile.dart';
import '../../../providers/user_profile_provider.dart';
import '../../theme/app_theme.dart';
import 'settings_fields.dart';

/// Inline form for editing the global default scheduling/urgency
/// preferences (mirrors `PriorityTaskManager.Models.UserProfile`), shown in
/// the Right Inspector when Settings is opened from the Left Rail.
class DefaultsInspectorForm extends ConsumerStatefulWidget {
  const DefaultsInspectorForm({super.key});

  @override
  ConsumerState<DefaultsInspectorForm> createState() =>
      _DefaultsInspectorFormState();
}

class _DefaultsInspectorFormState extends ConsumerState<DefaultsInspectorForm> {
  int? _sortOption;
  int? _schedulingMode;
  int? _workStartMinutes;
  int? _workEndMinutes;
  Set<int>? _workDays;
  double? _slackThresholdDire;
  double? _slackThresholdPressing;
  double? _slackThresholdFocus;
  double? _slackThresholdSafe;
  UserProfile? _loadedFrom;

  void _loadFrom(UserProfile profile) {
    if (identical(_loadedFrom, profile)) return;
    _loadedFrom = profile;
    _sortOption = profile.defaultListSortOption;
    _schedulingMode = profile.schedulingMode;
    _workStartMinutes = profile.workStartMinutes;
    _workEndMinutes = profile.workEndMinutes;
    _workDays = profile.workDays.toSet();
    _slackThresholdDire = profile.slackThresholdDire;
    _slackThresholdPressing = profile.slackThresholdPressing;
    _slackThresholdFocus = profile.slackThresholdFocus;
    _slackThresholdSafe = profile.slackThresholdSafe;
  }

  @override
  Widget build(BuildContext context) {
    final profileAsync = ref.watch(userProfileProvider);
    final profile = profileAsync.asData?.value;
    if (profile == null) {
      return const Center(child: CircularProgressIndicator());
    }
    _loadFrom(profile);

    return ListView(
      padding: const EdgeInsets.all(AppTheme.spacingMd),
      children: [
        Row(
          children: [
            Icon(Icons.tune, color: Theme.of(context).colorScheme.primary),
            const SizedBox(width: AppTheme.spacingSm),
            Text(
              'Default Settings',
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
          ],
        ),
        const SizedBox(height: AppTheme.spacingXs),
        Text(
          'Applied to every list unless overridden in that list\'s settings.',
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            color: Theme.of(context).colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        SettingsSectionCard(
          icon: Icons.sort,
          title: 'Sorting & scheduling',
          child: Column(
            children: [
              SortOptionField(
                value: _sortOption!,
                onChanged: (v) => setState(() => _sortOption = v),
              ),
              const SizedBox(height: AppTheme.spacingMd),
              SchedulingModeField(
                value: _schedulingMode!,
                onChanged: (v) => setState(() => _schedulingMode = v),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        SettingsSectionCard(
          icon: Icons.schedule,
          title: 'Work hours',
          child: WorkHoursField(
            startMinutes: _workStartMinutes!,
            endMinutes: _workEndMinutes!,
            onStartChanged: (v) => setState(() => _workStartMinutes = v),
            onEndChanged: (v) => setState(() => _workEndMinutes = v),
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        SettingsSectionCard(
          icon: Icons.calendar_today,
          title: 'Work days',
          child: WorkDaysField(
            selected: _workDays!,
            onChanged: (v) => setState(() => _workDays = v),
          ),
        ),
        const SizedBox(height: AppTheme.spacingMd),
        SettingsSectionCard(
          icon: Icons.speed,
          title: 'Urgency thresholds',
          child: SlackThresholdsField(
            dire: _slackThresholdDire!,
            pressing: _slackThresholdPressing!,
            focus: _slackThresholdFocus!,
            safe: _slackThresholdSafe!,
            onDireChanged: (v) => setState(() => _slackThresholdDire = v),
            onPressingChanged: (v) =>
                setState(() => _slackThresholdPressing = v),
            onFocusChanged: (v) => setState(() => _slackThresholdFocus = v),
            onSafeChanged: (v) => setState(() => _slackThresholdSafe = v),
          ),
        ),
        const SizedBox(height: AppTheme.spacingLg),
        FilledButton.icon(
          onPressed: () => _save(profile),
          icon: const Icon(Icons.save_outlined),
          label: const Text('Save'),
        ),
      ],
    );
  }

  Future<void> _save(UserProfile profile) async {
    final updated = profile.copyWith(
      defaultListSortOption: _sortOption,
      schedulingMode: _schedulingMode,
      workStartMinutes: _workStartMinutes,
      workEndMinutes: _workEndMinutes,
      workDays: _workDays!.toList()..sort(),
      slackThresholdDire: _slackThresholdDire,
      slackThresholdPressing: _slackThresholdPressing,
      slackThresholdFocus: _slackThresholdFocus,
      slackThresholdSafe: _slackThresholdSafe,
    );
    await ref.read(userProfileProvider.notifier).updateProfile(updated);
  }
}
