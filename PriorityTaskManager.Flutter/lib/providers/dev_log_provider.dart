import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../dev/dev_log_sink.dart';

/// Bridges [DevLogSink] (a plain [ChangeNotifier], not Riverpod-native) into
/// a provider so widgets rebuild as new entries arrive.
final devLogEntriesProvider = ChangeNotifierProvider<DevLogSink>(
  (ref) => DevLogSink.instance,
);

/// Whether the dev-only Dev Log bottom panel (issue #59) is currently open.
/// Toggled by the Left Rail's "Dev Log" item; only ever read behind a
/// `kDebugMode` check (see `command_center_screen.dart`).
final devLogPanelOpenProvider = StateProvider<bool>((ref) => false);
