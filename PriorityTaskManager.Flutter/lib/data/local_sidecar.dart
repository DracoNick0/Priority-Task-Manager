import 'dart:async';
import 'dart:io';

/// Manages a local, unauthenticated instance of `PriorityTaskManager.API`
/// (its `/api/local/*` routes, see docs/VISION.md MVP scope) so the fully
/// offline Flutter desktop client can run the real scheduling algorithms
/// without any account/login. Only supported on desktop platforms that can
/// spawn a child process; web is intentionally online-only (no sidecar).
///
/// Dev-only limitation: this launches the API from source via `dotnet run`,
/// assuming the monorepo's sibling `PriorityTaskManager.API` folder and the
/// .NET SDK are present. Bundling a published, self-contained sidecar
/// executable into a distributable Flutter build is separate follow-up work.
class LocalSidecar {
  LocalSidecar._();

  static final LocalSidecar instance = LocalSidecar._();

  static const int port = 5299;
  static final Uri baseUri = Uri.parse('http://127.0.0.1:$port');

  Process? _process;
  Future<void>? _startFuture;
  final StringBuffer _stderr = StringBuffer();
  int? _exitCode;

  /// True on platforms where this sidecar can be spawned as a child process.
  bool get isSupported =>
      !Platform.environment.containsKey('FLUTTER_TEST') &&
      (Platform.isWindows || Platform.isLinux || Platform.isMacOS);

  /// Ensures the local API is reachable, starting it if necessary. Throws a
  /// [StateError] if the platform can't host a sidecar (e.g. web).
  Future<Uri> ensureRunning() async {
    if (!isSupported) {
      throw StateError(
        'The local scheduling sidecar is only available on desktop; this '
        'platform must reach a real API server instead.',
      );
    }
    if (await _isReachable()) {
      return baseUri;
    }
    _startFuture ??= _start();
    await _startFuture;

    // `dotnet run` from source has to restore/build before it starts
    // listening, which can take well over a minute on a cold build.
    const maxWait = Duration(minutes: 2);
    const pollInterval = Duration(milliseconds: 500);
    final deadline = DateTime.now().add(maxWait);
    while (DateTime.now().isBefore(deadline)) {
      if (await _isReachable()) {
        return baseUri;
      }
      if (_exitCode != null) {
        throw StateError(
          'Local scheduling sidecar exited early (code $_exitCode).\n$_stderr',
        );
      }
      await Future<void>.delayed(pollInterval);
    }
    throw StateError(
      'Timed out waiting for the local scheduling sidecar to start.',
    );
  }

  Future<bool> _isReachable() async {
    try {
      final socket = await Socket.connect(
        baseUri.host,
        baseUri.port,
        timeout: const Duration(milliseconds: 200),
      );
      await socket.close();
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> _start() async {
    final apiProjectPath = Directory.current.parent.uri
        .resolve('PriorityTaskManager.API/')
        .toFilePath();

    _process = await Process.start(
      'dotnet',
      ['run', '--no-launch-profile', '--project', apiProjectPath],
      environment: {
        'ASPNETCORE_URLS': 'http://127.0.0.1:$port',
        'LocalOnly': 'true',
      },
    );
    _process!.stderr
        .transform(const SystemEncoding().decoder)
        .listen(_stderr.write);
    _process!.exitCode.then((code) => _exitCode = code);
  }

  /// Stops the sidecar process, if this instance started one.
  void dispose() {
    _process?.kill();
    _process = null;
    _startFuture = null;
  }
}
