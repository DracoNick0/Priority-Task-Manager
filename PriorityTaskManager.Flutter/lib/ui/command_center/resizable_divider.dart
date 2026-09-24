import 'package:flutter/material.dart';

/// A draggable divider between two resizable panes.
///
/// [axis] selects the drag direction (and which axis of movement is
/// reported via [onDragUpdate]): [Axis.horizontal] (default) is a vertical
/// line dragged left/right, used between side panes; [Axis.vertical] is a
/// horizontal line dragged up/down, used above a bottom-docked panel (e.g.
/// the Dev Log panel).
///
/// [onDragUpdate] reports the total pointer offset since [onDragStart]
/// fired, not the per-frame delta, so callers should anchor the resized
/// pane's size to whatever it was at drag start and compute the new size as
/// `anchorSize +/- totalOffset` each update. This keeps the pane's edge
/// glued to the cursor even after a min/max clamp is hit and the drag
/// reverses direction, which accumulating per-frame deltas cannot do.
class ResizableDivider extends StatefulWidget {
  const ResizableDivider({
    super.key,
    required this.onDragStart,
    required this.onDragUpdate,
    this.onDragEnd,
    this.axis = Axis.horizontal,
  });

  final VoidCallback onDragStart;
  final ValueChanged<double> onDragUpdate;
  final VoidCallback? onDragEnd;
  final Axis axis;

  @override
  State<ResizableDivider> createState() => _ResizableDividerState();
}

class _ResizableDividerState extends State<ResizableDivider> {
  bool _hovering = false;
  bool _dragging = false;
  Offset? _dragStartGlobalPosition;

  void _handleDragStart(Offset globalPosition) {
    _dragStartGlobalPosition = globalPosition;
    setState(() => _dragging = true);
    widget.onDragStart();
  }

  void _handleDragUpdate(double currentGlobalCoordinate, bool isHorizontal) {
    final start = _dragStartGlobalPosition;
    if (start == null) return;
    final startCoordinate = isHorizontal ? start.dx : start.dy;
    widget.onDragUpdate(currentGlobalCoordinate - startCoordinate);
  }

  void _handleDragEnd() {
    _dragStartGlobalPosition = null;
    setState(() => _dragging = false);
    widget.onDragEnd?.call();
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final highlighted = _hovering || _dragging;
    final isHorizontalDrag = widget.axis == Axis.horizontal;

    final bar = AnimatedContainer(
      duration: const Duration(milliseconds: 120),
      width: isHorizontalDrag ? (highlighted ? 4 : 1) : double.infinity,
      height: isHorizontalDrag ? double.infinity : (highlighted ? 4 : 1),
      decoration: BoxDecoration(
        color: highlighted ? colorScheme.primary : colorScheme.outlineVariant,
        borderRadius: BorderRadius.circular(2),
      ),
    );

    return MouseRegion(
      cursor: isHorizontalDrag
          ? SystemMouseCursors.resizeColumn
          : SystemMouseCursors.resizeRow,
      onEnter: (_) => setState(() => _hovering = true),
      onExit: (_) => setState(() => _hovering = false),
      child: GestureDetector(
        behavior: HitTestBehavior.translucent,
        onHorizontalDragStart: isHorizontalDrag
            ? (details) => _handleDragStart(details.globalPosition)
            : null,
        onHorizontalDragEnd: isHorizontalDrag ? (_) => _handleDragEnd() : null,
        onHorizontalDragCancel: isHorizontalDrag ? _handleDragEnd : null,
        onHorizontalDragUpdate: isHorizontalDrag
            ? (details) => _handleDragUpdate(details.globalPosition.dx, true)
            : null,
        onVerticalDragStart: isHorizontalDrag
            ? null
            : (details) => _handleDragStart(details.globalPosition),
        onVerticalDragEnd: isHorizontalDrag ? null : (_) => _handleDragEnd(),
        onVerticalDragCancel: isHorizontalDrag ? null : _handleDragEnd,
        onVerticalDragUpdate: isHorizontalDrag
            ? null
            : (details) => _handleDragUpdate(details.globalPosition.dy, false),
        child: SizedBox(
          width: isHorizontalDrag ? 12 : double.infinity,
          height: isHorizontalDrag ? double.infinity : 12,
          child: Center(child: bar),
        ),
      ),
    );
  }
}
