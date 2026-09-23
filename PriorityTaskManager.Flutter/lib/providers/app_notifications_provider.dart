import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// A one-shot, transient UI notification (e.g. "Task created") surfaced as
/// a SnackBar. Each instance is distinct (no `==` override) so pushing the
/// same message text twice in a row still triggers a fresh SnackBar via
/// [appNotificationProvider]'s listener.
@immutable
class AppNotification {
  const AppNotification(this.message, {this.icon = Icons.check_circle});

  final String message;

  /// Leading icon shown alongside [message]; callers pick one that matches
  /// the action (created/saved/deleted/completed).
  final IconData icon;
}

/// Holds the latest notification to surface. Set to a new [AppNotification]
/// instance to trigger a SnackBar; consumed by a listener in
/// CommandCenterScreen.
final appNotificationProvider = StateProvider<AppNotification?>((ref) => null);
