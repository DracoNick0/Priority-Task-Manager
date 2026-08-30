import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/user_profile.dart';
import 'task_providers.dart';

/// Global default scheduling/urgency preferences, backed by the active
/// [TaskRepository]. See docs/ARCHITECTURE_INTEGRATIONS.md's Hive-as-source-
/// of-truth principle: this is local-only, mirroring `UserProfile` on Core.
final userProfileProvider =
    AsyncNotifierProvider<UserProfileNotifier, UserProfile>(
      UserProfileNotifier.new,
    );

class UserProfileNotifier extends AsyncNotifier<UserProfile> {
  @override
  Future<UserProfile> build() async {
    final repository = await ref.watch(taskRepositoryProvider.future);
    return repository.getProfile();
  }

  Future<void> updateProfile(UserProfile profile) async {
    final repository = await ref.read(taskRepositoryProvider.future);
    await repository.updateProfile(profile);
    ref.invalidateSelf();
    await future;
  }
}
