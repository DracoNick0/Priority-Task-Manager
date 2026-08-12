import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../providers/event_providers.dart';
import '../theme/app_theme.dart';

/// A card for a fixed, immovable [FixedEvent].
///
/// Rendered with a dark background and a subtle diagonal hash pattern so it
/// visually reads as immovable, distinct from schedulable task cards.
class EventCard extends StatelessWidget {
  const EventCard({super.key, required this.event, required this.onTap});

  final FixedEvent event;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final timeFormat = DateFormat.jm();
    const darkBackground = Color(0xFF111827);

    return Container(
      margin: const EdgeInsets.symmetric(vertical: AppTheme.spacingXs),
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(AppTheme.radiusMd),
        border: Border.all(color: Colors.white24),
      ),
      child: Material(
        color: darkBackground,
        child: InkWell(
          onTap: onTap,
          child: Stack(
            children: [
              Positioned.fill(
                child: CustomPaint(painter: _DiagonalHashPainter()),
              ),
              Padding(
                padding: const EdgeInsets.all(AppTheme.spacingSm),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Icon(Icons.event, size: 16, color: Colors.white70),
                    const SizedBox(width: AppTheme.spacingSm),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            event.title,
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              fontWeight: FontWeight.w600,
                              color: Colors.white,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            '${timeFormat.format(event.startTime)} \u2013 ${timeFormat.format(event.endTime)}',
                            style: const TextStyle(
                              fontSize: 12,
                              color: Colors.white70,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _DiagonalHashPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white.withOpacity(0.06)
      ..strokeWidth = 1;

    const spacing = 10.0;
    for (double x = -size.height; x < size.width; x += spacing) {
      canvas.drawLine(
        Offset(x, size.height),
        Offset(x + size.height, 0),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
