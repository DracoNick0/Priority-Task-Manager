import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Ticks once a minute so the Left Rail's Engine Status clock stays live.
///
/// Placeholder for a future time-simulation provider (see
/// docs/ARCHITECTURE_SCHEDULING.md); currently always reflects wall-clock time.
final engineClockProvider = StreamProvider<DateTime>((ref) async* {
  yield DateTime.now();
  yield* Stream.periodic(const Duration(minutes: 1), (_) => DateTime.now());
});

/// The active scheduling/algorithm mode label shown in the Engine Status
/// indicator. Placeholder until list/profile-level algorithm selection is
/// exposed from the sidecar to the Flutter client.
final algorithmModeProvider = StateProvider<String>((ref) => 'Gold Panning');
