import 'package:flutter/material.dart';
import 'package:priority_task_manager/ui/theme/app_theme.dart';

/// A multi-line [TextField] with a drag handle beneath it so the user can
/// adjust its height, useful for long free-form text like descriptions.
class ResizableTextField extends StatefulWidget {
  const ResizableTextField({
    super.key,
    required this.controller,
    required this.label,
    this.hintText,
    this.initialHeight = 90,
    this.minHeight = 60,
    this.maxHeight = 400,
  });

  final TextEditingController controller;
  final String label;
  final String? hintText;
  final double initialHeight;
  final double minHeight;
  final double maxHeight;

  @override
  State<ResizableTextField> createState() => _ResizableTextFieldState();
}

class _ResizableTextFieldState extends State<ResizableTextField> {
  double _height = 90;

  @override
  void initState() {
    super.initState();
    _height = widget.initialHeight;
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SizedBox(
          height: _height,
          child: TextField(
            controller: widget.controller,
            decoration: InputDecoration(
              labelText: widget.label,
              hintText: widget.hintText,
              contentPadding: const EdgeInsets.fromLTRB(
                AppTheme.spacingMd,
                AppTheme.spacingMd,
                AppTheme.spacingSm,
                AppTheme.spacingSm,
              ),
            ),
            maxLines: null,
            expands: true,
            textAlignVertical: TextAlignVertical.top,
          ),
        ),
        MouseRegion(
          cursor: SystemMouseCursors.resizeRow,
          child: GestureDetector(
            behavior: HitTestBehavior.translucent,
            onVerticalDragUpdate: (details) {
              setState(() {
                _height = (_height + details.delta.dy).clamp(
                  widget.minHeight,
                  widget.maxHeight,
                );
              });
            },
            child: SizedBox(
              height: 12,
              child: Center(
                child: Container(
                  width: 32,
                  height: 4,
                  decoration: BoxDecoration(
                    color: colorScheme.outlineVariant,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}
