import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/api_schedule_repository.dart';
import '../dev/dev_auto_login.dart';

/// The bearer token to attach to authenticated API calls, resolved once via
/// dev auto-login (see `lib/dev/dev_auto_login.dart`). Resolves to `null`
/// when `PTM_DEV_AUTOLOGIN` isn't set, since the app has no login screen yet;
/// callers should fall back to unauthenticated local endpoints in that case.
final authTokenProvider = FutureProvider<String?>((ref) {
  return resolveDevAutoLoginToken(
    baseUri: ApiScheduleRepository.defaultBaseUri,
  );
});
