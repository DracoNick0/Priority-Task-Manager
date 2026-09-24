/// Shared collection helpers used across providers and UI code.
extension FirstOrNullExtension<T> on Iterable<T> {
  /// Returns the first element, or null if this iterable is empty.
  T? get firstOrNull => isEmpty ? null : first;
}
