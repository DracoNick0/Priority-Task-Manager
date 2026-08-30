import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../providers/session_provider.dart';
import '../auth/login_screen.dart';
import '../theme/app_theme.dart';

/// The one-time, guest-first entry choice (issue #44): "Continue as Guest"
/// vs. "Log in / Create account". Shown only on first run
/// ([SessionStatus.needsEntryChoice]); never shown again once a choice is
/// made, and never a gate the user must pass through beyond this single
/// screen (see docs/VISION.md's guest-first, never-forced-login principle).
class EntryChoiceScreen extends ConsumerWidget {
  const EntryChoiceScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Padding(
              padding: const EdgeInsets.all(AppTheme.spacingLg),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    'Priority Task Manager',
                    style: textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppTheme.spacingSm),
                  Text(
                    'Use it fully offline as a guest, or log in / create an '
                    'account to unlock online scheduling. You can decide '
                    'this only once — guests are never asked to log in.',
                    style: textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppTheme.spacingXl),
                  FilledButton(
                    onPressed: () => ref
                        .read(sessionControllerProvider.notifier)
                        .continueAsGuest(),
                    child: const Padding(
                      padding: EdgeInsets.symmetric(
                        vertical: AppTheme.spacingSm,
                      ),
                      child: Text('Continue as Guest'),
                    ),
                  ),
                  const SizedBox(height: AppTheme.spacingMd),
                  OutlinedButton(
                    onPressed: () => Navigator.of(context).push(
                      MaterialPageRoute(builder: (_) => const LoginScreen()),
                    ),
                    child: const Padding(
                      padding: EdgeInsets.symmetric(
                        vertical: AppTheme.spacingSm,
                      ),
                      child: Text('Log in / Create account'),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
