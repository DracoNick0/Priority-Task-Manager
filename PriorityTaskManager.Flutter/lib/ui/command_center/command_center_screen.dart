import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../providers/task_providers.dart';
import 'center_stage.dart';
import 'left_rail.dart';
import 'resizable_divider.dart';
import 'right_inspector.dart';

const double _leftMinWidth = 200;
const double _leftMaxWidth = 420;
const double _centerMinWidth = 400;
const double _rightMinWidth = 300;
const double _rightMaxWidth = 520;
const double _dividerWidth = 12;

/// Root widget for the "Three-Pane Command Center" layout: a persistent
/// Left Rail, a horizontally scrolling Center Stage pipeline, and a Right
/// Inspector, with responsive collapsing so no pane is ever squeezed below
/// its minimum width (avoiding RenderFlex overflow).
class CommandCenterScreen extends ConsumerStatefulWidget {
  const CommandCenterScreen({super.key});

  @override
  ConsumerState<CommandCenterScreen> createState() =>
      _CommandCenterScreenState();
}

class _CommandCenterScreenState extends ConsumerState<CommandCenterScreen> {
  final _scaffoldKey = GlobalKey<ScaffoldState>();
  double _leftWidth = 260;
  double _rightWidth = 340;

  @override
  void initState() {
    super.initState();
    // Ensure a list is selected once lists finish loading, so the pipeline
    // and inspector have something to show by default.
    Future.microtask(() {
      final lists = ref.read(taskListsProvider).asData?.value;
      if (lists != null &&
          lists.isNotEmpty &&
          ref.read(activeListIdProvider) == null) {
        ref.read(activeListIdProvider.notifier).state = lists.first.id;
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    // Keep a freshly-loaded list selected by default without fighting user
    // navigation once something is already selected.
    ref.listen(taskListsProvider, (previous, next) {
      final lists = next.asData?.value;
      if (lists != null &&
          lists.isNotEmpty &&
          ref.read(activeListIdProvider) == null) {
        ref.read(activeListIdProvider.notifier).state = lists.first.id;
      }
    });

    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final isWide = width >= _leftMinWidth + _centerMinWidth + _rightMinWidth;
        final isMedium =
            !isWide && width >= _leftMinWidth + _centerMinWidth;
        final isNarrow = !isWide && !isMedium;

        final leftWidth = _leftWidth.clamp(
          _leftMinWidth,
          _leftMaxWidth,
        ).toDouble();
        final rightWidth = _rightWidth.clamp(
          _rightMinWidth,
          _rightMaxWidth,
        ).toDouble();

        return Scaffold(
          key: _scaffoldKey,
          drawer: isNarrow
              ? Drawer(width: 280, child: const LeftRail())
              : null,
          endDrawer: !isWide
              ? Drawer(
                  width: rightWidth.clamp(_rightMinWidth, 400).toDouble(),
                  child: RightInspector(
                    onClose: () => Navigator.of(context).pop(),
                  ),
                )
              : null,
          body: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (isWide || isMedium)
                AnimatedContainer(
                  duration: const Duration(milliseconds: 150),
                  width: leftWidth,
                  child: const LeftRail(),
                ),
              if (isWide || isMedium)
                ResizableDivider(
                  onDrag: (delta) {
                    final maxLeft = (width - rightWidth - _centerMinWidth -
                            _dividerWidth * 2)
                        .clamp(_leftMinWidth, _leftMaxWidth);
                    setState(() {
                      _leftWidth = (_leftWidth + delta)
                          .clamp(_leftMinWidth, maxLeft)
                          .toDouble();
                    });
                  },
                ),
              Expanded(
                child: CenterStage(
                  showHamburger: isNarrow,
                  onOpenLeftRail: () =>
                      _scaffoldKey.currentState?.openDrawer(),
                  showInspectorToggle: !isWide,
                  onOpenInspector: () =>
                      _scaffoldKey.currentState?.openEndDrawer(),
                ),
              ),
              if (isWide) ...[
                ResizableDivider(
                  onDrag: (delta) {
                    final maxRight = (width - leftWidth - _centerMinWidth -
                            _dividerWidth * 2)
                        .clamp(_rightMinWidth, _rightMaxWidth);
                    setState(() {
                      _rightWidth = (_rightWidth - delta)
                          .clamp(_rightMinWidth, maxRight)
                          .toDouble();
                    });
                  },
                ),
                AnimatedContainer(
                  duration: const Duration(milliseconds: 150),
                  width: rightWidth,
                  child: const RightInspector(),
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}
