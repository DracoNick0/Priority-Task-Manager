/// What kind of thing a [DevLogEntry] represents.
enum DevLogKind {
  /// An outbound HTTP request/response (task/list/event/schedule/auth calls).
  httpCall,

  /// A repository-level domain action (e.g. Guest/local task mutations that
  /// never go over HTTP).
  domainEvent,
}

/// A single dev-log record, shown by the dev log screen (issue #59) and
/// printed to the console in debug builds. Never constructed/collected
/// outside of `kDebugMode` (see `lib/dev/dev_log_sink.dart`).
class DevLogEntry {
  DevLogEntry({
    required this.kind,
    required this.label,
    required this.timestamp,
    required this.isError,
    this.detail,
    this.durationMs,
  });

  final DevLogKind kind;

  /// Short summary, e.g. `POST /api/tasks -> 201` or `addTask(listId: ...)`.
  final String label;

  final DateTime timestamp;
  final bool isError;

  /// Optional longer-form detail (error message, response snippet). Must
  /// already be redacted of secrets (tokens/passwords) by the caller.
  final String? detail;

  final int? durationMs;

  /// Calls at/above this duration are flagged as slow in the dev log panel.
  static const int slowThresholdMs = 1000;

  bool get isSlow => durationMs != null && durationMs! >= slowThresholdMs;
}
