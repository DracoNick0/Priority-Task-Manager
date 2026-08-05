import 'package:flutter/material.dart';
import '../../models/schedule_models.dart';
import 'package:intl/intl.dart';

class DenseDataView extends StatelessWidget {
  final DailySchedule schedule;

  const DenseDataView({super.key, required this.schedule});

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16.0),
      children: [
        _buildMetricsDashboard(context),
        const SizedBox(height: 24),
        Text("Scheduled Tasks", style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        _buildDataTable(context, schedule.todayTasks),
        const SizedBox(height: 32),
        Text(
          "Backlog (Unscheduled / Future)",
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 8),
        _buildDataTable(context, schedule.futureTasks, isFuture: true),
      ],
    );
  }

  Widget _buildMetricsDashboard(BuildContext context) {
    return Card(
      elevation: 0,
      color: Theme.of(context).colorScheme.surfaceContainerHighest,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              "Scheduling Metrics",
              style: Theme.of(
                context,
              ).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
            ),
            const Divider(),
            _metricRow("Task with least slack:", schedule.leastSlackTask),
            _metricRow("Realistic Slack:", schedule.realisticSlack),
            _metricRow("Actual Slack:", schedule.actualSlack),
          ],
        ),
      ),
    );
  }

  Widget _metricRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontWeight: FontWeight.w500)),
          Text(value),
        ],
      ),
    );
  }

  Widget _buildDataTable(
    BuildContext context,
    List<ScheduledTask> tasks, {
    bool isFuture = false,
  }) {
    final timeFormat = DateFormat('HH:mm');
    final dateFormat = DateFormat('MM-dd');

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        headingRowColor: WidgetStateProperty.all(
          Theme.of(context).colorScheme.surfaceContainer,
        ),
        columns: const [
          DataColumn(label: Text('ID')),
          DataColumn(label: Text('Time')),
          DataColumn(label: Text('Task')),
          DataColumn(label: Text('Due')),
          DataColumn(label: Text('Chunk')),
        ],
        rows: tasks.map((task) {
          final timeString = isFuture
              ? '---'
              : '${timeFormat.format(task.startTime)} - ${timeFormat.format(task.endTime)}';

          return DataRow(
            cells: [
              DataCell(Text(task.id)),
              DataCell(Text(timeString)),
              DataCell(Text(task.title)),
              DataCell(
                Text(
                  task.dueDate == null ? '—' : dateFormat.format(task.dueDate!),
                ),
              ),
              DataCell(Text('${task.chunkHours}h')),
            ],
          );
        }).toList(),
      ),
    );
  }
}
