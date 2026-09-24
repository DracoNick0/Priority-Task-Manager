import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/task_item.dart';
import 'task_providers.dart';

/// Archived tasks for the active (Authenticated-only) session. Archive is an
/// online-exclusive feature (see docs/VISION.md); the UI must not watch this
/// provider for a Guest session (see `LocalTaskRepository.getArchivedTasks`,
/// which fails fast if reached).
final archivedTasksProvider =
    AsyncNotifierProvider<ArchivedTasksNotifier, List<TaskItem>>(
      ArchivedTasksNotifier.new,
    );

class ArchivedTasksNotifier extends AsyncNotifier<List<TaskItem>> {
  @override
  Future<List<TaskItem>> build() async {
    final repository = await ref.watch(taskRepositoryProvider.future);
    return repository.getArchivedTasks();
  }

  /// Restores an archived task, refreshing this list and the target list's
  /// tasks on success. Rethrows [RestoreTargetListRequiredException] (from
  /// `../data/task_repository.dart`) unchanged so the caller can prompt for
  /// a target list and retry.
  Future<void> restoreArchivedTask(
    String taskId, {
    String? targetListId,
  }) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    final restored = await repository.restoreArchivedTask(
      taskId,
      targetListId: targetListId,
    );
    ref.invalidateSelf();
    await future;
    ref.invalidate(tasksProvider(restored.listId));
  }

  /// Permanently deletes an archived task, refreshing this list on success.
  Future<void> deleteArchivedTask(String taskId) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.deleteArchivedTask(taskId);
    ref.invalidateSelf();
    await future;
  }
}
