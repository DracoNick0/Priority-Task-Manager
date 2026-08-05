import 'package:flutter/material.dart';
import '../../models/schedule_models.dart';
import 'package:intl/intl.dart';

class MinimalistTimelineView extends StatelessWidget {
  final DailySchedule schedule;

  const MinimalistTimelineView({super.key, required this.schedule});

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(24.0),
      children: [
        _buildLoadBar(context),
        const SizedBox(height: 32),
        Text(
          "Today's Schedule",
          style: Theme.of(
            context,
          ).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        ...schedule.todayTasks.map((task) => _buildTaskItem(context, task)),
        const SizedBox(height: 32),
        Text(
          "Coming Up",
          style: Theme.of(context).textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.bold,
            color: Colors.grey.shade600,
          ),
        ),
        const SizedBox(height: 16),
        ...schedule.futureTasks.map(
          (task) => _buildFutureTaskItem(context, task),
        ),
      ],
    );
  }

  Widget _buildLoadBar(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text("Capacity", style: Theme.of(context).textTheme.titleSmall),
            Text(
              "Slack: ${schedule.realisticSlack}",
              style: Theme.of(
                context,
              ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
            ),
          ],
        ),
        const SizedBox(height: 8),
        Container(
          height: 12,
          decoration: BoxDecoration(
            color: Colors.grey.shade200,
            borderRadius: BorderRadius.circular(6),
          ),
          child: Row(
            children: [
              Expanded(
                flex: 70, // Mock percentage
                child: Container(
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.primary,
                    borderRadius: BorderRadius.circular(6),
                  ),
                ),
              ),
              Expanded(flex: 30, child: Container()),
            ],
          ),
        ),
        const SizedBox(height: 4),
        Text(
          "Least slack: ${schedule.leastSlackTask}",
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            fontSize: 10,
            color: Colors.grey.shade600,
          ),
        ),
      ],
    );
  }

  Widget _buildTaskItem(BuildContext context, ScheduledTask task) {
    final timeFormat = DateFormat('HH:mm');
    final dateFormat = DateFormat('MM-dd');

    return Padding(
      padding: const EdgeInsets.only(bottom: 24.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 60,
            child: Text(
              timeFormat.format(task.startTime),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                color: Theme.of(context).colorScheme.primary,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16.0),
            child: Text("────", style: TextStyle(color: Colors.grey.shade300)),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  task.title,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 4),
                Text(
                  "Due: ${task.dueDate == null ? 'None' : dateFormat.format(task.dueDate!)} • ${task.chunkHours}h",
                  style: Theme.of(
                    context,
                  ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFutureTaskItem(BuildContext context, ScheduledTask task) {
    final dateFormat = DateFormat('MM-dd');
    return Padding(
      padding: const EdgeInsets.only(bottom: 12.0),
      child: Row(
        children: [
          Icon(Icons.circle_outlined, size: 16, color: Colors.grey.shade400),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              task.title,
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: Colors.grey.shade700),
            ),
          ),
          Text(
            task.dueDate == null ? '—' : dateFormat.format(task.dueDate!),
            style: Theme.of(
              context,
            ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade500),
          ),
        ],
      ),
    );
  }
}
