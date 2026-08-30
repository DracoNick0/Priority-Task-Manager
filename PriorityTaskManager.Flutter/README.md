# priority_task_manager (Flutter client)

Local-only (offline/guest) Flutter web + Windows desktop client for Priority Task Manager. See [docs/ARCHITECTURE_INTEGRATIONS.md](../docs/ARCHITECTURE_INTEGRATIONS.md) and [docs/STATUS.md](../docs/STATUS.md) for how this client fits into the overall project.

## First-Time Setup

1. Install a Flutter SDK that satisfies the `environment.sdk` constraint in [pubspec.yaml](pubspec.yaml). If your installed Flutter is older than that constraint, `flutter pub get` will fail with an SDK version solving error — run `flutter upgrade` (on the `stable` channel) and try again.
2. From this directory, fetch dependencies:

   ```bash
   flutter pub get
   ```

3. Run the app:

   ```bash
   flutter run -d windows   # Windows desktop
   flutter run -d chrome    # Web
   ```

This client calls a locally-running `PriorityTaskManager.API` instance (started separately, e.g. via the "API (Cloud, dev seed :5299)" launch config or `dotnet run` from `PriorityTaskManager.API/`) to compute schedules using the real scheduling strategies for logged-in accounts; see the root [README.md](../README.md) and [docs/WORKFLOW.md](../docs/WORKFLOW.md) for how to run it.

## Verifying Changes

```bash
flutter analyze
flutter test
```

## Getting Started With Flutter

If this is your first Flutter project, see:

- [Lab: Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Cookbook: Useful Flutter samples](https://docs.flutter.dev/cookbook)
- [Flutter documentation](https://docs.flutter.dev/)
