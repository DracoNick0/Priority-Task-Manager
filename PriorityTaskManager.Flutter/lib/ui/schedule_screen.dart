import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/schedule_models.dart';
import '../providers/view_provider.dart';
import 'views/dense_data_view.dart';
import 'views/minimalist_timeline_view.dart';

class ScheduleScreen extends ConsumerWidget {
  const ScheduleScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final viewMode = ref.watch(viewModeProvider);
    final scheduleAsync = ref.watch(scheduleProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Daily Schedule'),
        actions: [
          IconButton(
            icon: Icon(
              viewMode == ViewMode.minimalist
                  ? Icons.table_chart
                  : Icons.calendar_view_day,
            ),
            tooltip: 'Toggle View',
            onPressed: () {
              ref
                  .read(viewModeProvider.notifier)
                  .state = viewMode == ViewMode.minimalist
                  ? ViewMode.dense
                  : ViewMode.minimalist;
            },
          ),
        ],
      ),
      body: scheduleAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, stackTrace) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text(
              'Could not compute the schedule: $error',
              textAlign: TextAlign.center,
            ),
          ),
        ),
        data: (DailySchedule schedule) => AnimatedSwitcher(
          duration: const Duration(milliseconds: 300),
          child: viewMode == ViewMode.minimalist
              ? MinimalistTimelineView(
                  key: const ValueKey('minimalist'),
                  schedule: schedule,
                )
              : DenseDataView(key: const ValueKey('dense'), schedule: schedule),
        ),
      ),
    );
  }
}
