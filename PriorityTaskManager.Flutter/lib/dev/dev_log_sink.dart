import 'package:flutter/foundation.dart';

import 'dev_log_entry.dart';

/// In-memory ring buffer of recent [DevLogEntry]s, watched by the dev log
/// screen (issue #59). A single app-wide instance ([instance]) is shared by
/// every logging wrapper so the screen sees calls from all repositories.
///
/// Only ever populated in debug builds (`kDebugMode`) — logging wrappers
/// must not be constructed at all in release builds, so this sink simply
/// stays empty rather than gating each [log] call individually.
class DevLogSink extends ChangeNotifier {
  DevLogSink({this.maxEntries = 200});

  static final DevLogSink instance = DevLogSink();

  final int maxEntries;
  final List<DevLogEntry> _entries = [];

  List<DevLogEntry> get entries => List.unmodifiable(_entries);

  void log(DevLogEntry entry) {
    _entries.insert(0, entry);
    if (_entries.length > maxEntries) {
      _entries.removeLast();
    }
    if (kDebugMode) {
      final status = entry.isError ? 'ERROR' : 'ok';
      debugPrint(
        '[DevLog][$status] ${entry.label}${entry.detail == null ? '' : ' — ${entry.detail}'}',
      );
    }
    notifyListeners();
  }

  void clear() {
    _entries.clear();
    notifyListeners();
  }
}
