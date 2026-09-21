import 'package:flutter/material.dart';

/// A draggable divider between two resizable panes.
///
/// [axis] selects the drag direction (and which drag-delta axis is reported
/// via [onDrag]): [Axis.horizontal] (default) is a vertical line dragged
/// left/right, used between side panes; [Axis.vertical] is a horizontal line
/// dragged up/down, used above a bottom-docked panel (e.g. the Dev Log
/// panel). Callers own the actual size state and are responsible for
/// clamping to min/max bounds.
class ResizableDivider extends StatefulWidget {
  const ResizableDivider({
    super.key,
    required this.onDrag,
    this.axis = Axis.horizontal,
  });

  final ValueChanged<double> onDrag;
  final Axis axis;

  @override
  State<ResizableDivider> createState() => _ResizableDividerState();
}

class _ResizableDividerState extends State<ResizableDivider> {
  bool _hovering = false;
  bool _dragging = false;

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
            ? (_) => setState(() => _dragging = true)
            : null,
        onHorizontalDragEnd: isHorizontalDrag
            ? (_) => setState(() => _dragging = false)
            : null,
        onHorizontalDragCancel: isHorizontalDrag
            ? () => setState(() => _dragging = false)
            : null,
        onHorizontalDragUpdate: isHorizontalDrag
            ? (details) => widget.onDrag(details.delta.dx)
            : null,
        onVerticalDragStart: isHorizontalDrag
            ? null
            : (_) => setState(() => _dragging = true),
        onVerticalDragEnd: isHorizontalDrag
            ? null
            : (_) => setState(() => _dragging = false),
        onVerticalDragCancel: isHorizontalDrag
            ? null
            : () => setState(() => _dragging = false),
        onVerticalDragUpdate: isHorizontalDrag
            ? null
            : (details) => widget.onDrag(details.delta.dy),
        child: SizedBox(
          width: isHorizontalDrag ? 12 : double.infinity,
          height: isHorizontalDrag ? double.infinity : 12,
          child: Center(child: bar),
        ),
      ),
    );
  }
}
