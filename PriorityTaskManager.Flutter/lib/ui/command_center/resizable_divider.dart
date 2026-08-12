import 'package:flutter/material.dart';

/// A draggable vertical divider between two resizable panes.
///
/// Reports incremental horizontal drag deltas via [onDrag]; callers own the
/// actual width state and are responsible for clamping to min/max bounds.
class ResizableDivider extends StatefulWidget {
  const ResizableDivider({super.key, required this.onDrag});

  final ValueChanged<double> onDrag;

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

    return MouseRegion(
      cursor: SystemMouseCursors.resizeColumn,
      onEnter: (_) => setState(() => _hovering = true),
      onExit: (_) => setState(() => _hovering = false),
      child: GestureDetector(
        behavior: HitTestBehavior.translucent,
        onHorizontalDragStart: (_) => setState(() => _dragging = true),
        onHorizontalDragEnd: (_) => setState(() => _dragging = false),
        onHorizontalDragCancel: () => setState(() => _dragging = false),
        onHorizontalDragUpdate: (details) => widget.onDrag(details.delta.dx),
        child: SizedBox(
          width: 12,
          child: Center(
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 120),
              width: highlighted ? 4 : 1,
              decoration: BoxDecoration(
                color: highlighted
                    ? colorScheme.primary
                    : colorScheme.outlineVariant,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
