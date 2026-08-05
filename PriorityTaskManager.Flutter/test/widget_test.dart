// Basic smoke test verifying the app shell builds without crashing.
//
// Repository initialization (Hive) depends on platform channels that aren't
// available under `flutter test`, so this only asserts the widget tree mounts
// cleanly, not that data finishes loading.

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:priority_task_manager/main.dart';

void main() {
  testWidgets('App shell mounts without throwing', (WidgetTester tester) async {
    await tester.pumpWidget(
      const ProviderScope(child: PriorityTaskManagerApp()),
    );
    await tester.pump();

    expect(tester.takeException(), isNull);
    expect(find.byType(MaterialApp), findsOneWidget);
  });
}
