import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:hive_ce_flutter/hive_flutter.dart';

import 'hive_registrar.g.dart';
import 'providers/session_provider.dart';
import 'ui/command_center/command_center_screen.dart';
import 'ui/onboarding/entry_choice_screen.dart';
import 'ui/theme/app_theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Hive.initFlutter();
  Hive.registerAdapters();

  runApp(const ProviderScope(child: PriorityTaskManagerApp()));
}

class PriorityTaskManagerApp extends ConsumerWidget {
  const PriorityTaskManagerApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);

    return MaterialApp(
      title: 'Priority Task Manager',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: ThemeMode.system,
      home: session.when(
        // Resolving the persisted session (or dev auto-login) on startup;
        // never longer than a brief local storage read/dev-login call.
        loading: () => const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        ),
        // Secure storage reads shouldn't fail, but never block/nag the user
        // if one does — fall back to the entry choice rather than an error page.
        error: (error, stackTrace) => const EntryChoiceScreen(),
        data: (state) => state.status == SessionStatus.needsEntryChoice
            ? const EntryChoiceScreen()
            : const CommandCenterScreen(),
      ),
    );
  }
}

