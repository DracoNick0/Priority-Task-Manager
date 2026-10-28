import 'package:flutter/material.dart';

const Color horizonEventBlockColor = Color(0xFF2C3440);
const Color horizonEventHatchLineColor = Color(0x19FFFFFF);
const double horizonEventHatchSpacing = 12.0;

/// Paints diagonal hatch marks over fixed-event blocks.
class DiagonalHatchPainter extends CustomPainter {
  const DiagonalHatchPainter({required this.lineColor, this.spacing = 10.0});

  final Color lineColor;
  final double spacing;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = lineColor
      ..strokeWidth = 1.0;

    for (double x = -size.height; x < size.width; x += spacing) {
      canvas.drawLine(
        Offset(x, 0),
        Offset(x + size.height, size.height),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant DiagonalHatchPainter oldDelegate) =>
      oldDelegate.lineColor != lineColor || oldDelegate.spacing != spacing;
}
