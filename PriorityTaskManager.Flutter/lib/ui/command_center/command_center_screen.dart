import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import 'center_stage.dart';
import 'left_rail.dart';
import 'resizable_divider.dart';
import 'right_inspector.dart';

const double _leftMinWidth = 150;
const double _leftMaxWidth = 350;
const double _centerMinWidth = 400;
const double _rightMinWidth = 300;
const double _rightMaxWidth = 500;
const double _dividerWidth = 12;
// How far past a pane's minimum width the user must drag before it snaps
// shut and collapses into a drawer/button, independent of window size.
const double _collapseThreshold = 60;

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
  // Manually collapsed via drag, independent of the window-size-driven
  // isWide/isMedium/isNarrow breakpoints.
  bool _leftCollapsed = false;
  bool _rightCollapsed = false;

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

    // ref.listen must run directly in build(), not inside a nested builder
    // closure, so the right-docked check is mirrored here off MediaQuery
    // rather than the LayoutBuilder constraints used below.
    final mediaWidth = MediaQuery.sizeOf(context).width;
    final isWideForInspector =
        mediaWidth >= _leftMinWidth + _centerMinWidth + _rightMinWidth;
    final rightDockedForInspector = isWideForInspector && !_rightCollapsed;

    // Pop the inspector open like the "show inspector" button would
    // whenever something gets selected but the pane isn't docked.
    ref.listen<InspectorTarget>(selectedInspectorProvider, (previous, next) {
      if (next.kind != InspectorKind.none && !rightDockedForInspector) {
        _scaffoldKey.currentState?.openEndDrawer();
      }
    });

    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final isWide =
            width >= _leftMinWidth + _centerMinWidth + _rightMinWidth;
        final isMedium = !isWide && width >= _leftMinWidth + _centerMinWidth;
        final isNarrow = !isWide && !isMedium;

        // Docked (in-line) visibility, factoring in manual drag-to-collapse
        // on top of the window-size-driven breakpoints.
        final leftDocked = (isWide || isMedium) && !_leftCollapsed;
        final rightDocked = isWide && !_rightCollapsed;

        final leftWidth = _leftWidth
            .clamp(_leftMinWidth, _leftMaxWidth)
            .toDouble();
        final rightWidth = _rightWidth
            .clamp(_rightMinWidth, _rightMaxWidth)
            .toDouble();

        return Scaffold(
          key: _scaffoldKey,
          drawer: !leftDocked
              ? Drawer(
                  width: 280,
                  child: LeftRail(
                    dockAction: isNarrow
                        ? null
                        : IconButton(
                            icon: const Icon(Icons.dock),
                            tooltip: 'Dock panel',
                            onPressed: () {
                              Navigator.of(context).pop();
                              setState(() {
                                _leftCollapsed = false;
                                _leftWidth = _leftMinWidth;
                              });
                            },
                          ),
                  ),
                )
              : null,
          endDrawer: !rightDocked
              ? Drawer(
                  width: rightWidth.clamp(_rightMinWidth, 400).toDouble(),
                  child: RightInspector(
                    headerAction: isWide
                        ? IconButton(
                            icon: const Icon(Icons.dock),
                            tooltip: 'Dock panel',
                            onPressed: () {
                              Navigator.of(context).pop();
                              setState(() {
                                _rightCollapsed = false;
                                _rightWidth = _rightMinWidth;
                              });
                            },
                          )
                        : null,
                  ),
                )
              : null,
          body: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (leftDocked)
                SizedBox(width: leftWidth, child: const LeftRail()),
              // Kept mounted even while collapsed (not gated on leftDocked)
              // so a single continuous drag can collapse and then
              // re-expand the pane without releasing the mouse button.
              if (isWide || isMedium)
                ResizableDivider(
                  onDrag: (delta) {
                    final maxLeft =
                        (width -
                                rightWidth -
                                _centerMinWidth -
                                _dividerWidth * 2)
                            .clamp(_leftMinWidth, _leftMaxWidth);
                    final proposed = _leftWidth + delta;
                    // Lower bound intentionally left unclamped (down to the
                    // collapse threshold) so consecutive small drag deltas
                    // below the visual minimum still accumulate toward
                    // collapsing, and dragging back the other way un-collapses.
                    // TODO: the divider should stay glued to the cursor
                    // 1:1 throughout the drag instead of lagging/detaching
                    // once a clamp/collapse limit is hit.
                    setState(() {
                      _leftCollapsed =
                          proposed < _leftMinWidth - _collapseThreshold;
                      _leftWidth = proposed
                          .clamp(_leftMinWidth - _collapseThreshold, maxLeft)
                          .toDouble();
                    });
                  },
                ),
              Expanded(
                child: CenterStage(
                  showHamburger: !leftDocked,
                  onOpenLeftRail: () => _scaffoldKey.currentState?.openDrawer(),
                  showInspectorToggle: !rightDocked,
                  onOpenInspector: () =>
                      _scaffoldKey.currentState?.openEndDrawer(),
                ),
              ),
              // Kept mounted even while collapsed (not gated on rightDocked)
              // so a single continuous drag can collapse and then
              // re-expand the pane without releasing the mouse button.
              if (isWide)
                ResizableDivider(
                  onDrag: (delta) {
                    final maxRight =
                        (width -
                                leftWidth -
                                _centerMinWidth -
                                _dividerWidth * 2)
                            .clamp(_rightMinWidth, _rightMaxWidth);
                    final proposed = _rightWidth - delta;
                    // See the left divider's onDrag for why the lower bound
                    // is intentionally left unclamped here.
                    // TODO: same as the left divider — it should stay
                    // glued to the cursor 1:1 instead of lagging/detaching.
                    setState(() {
                      _rightCollapsed =
                          proposed < _rightMinWidth - _collapseThreshold;
                      _rightWidth = proposed
                          .clamp(_rightMinWidth - _collapseThreshold, maxRight)
                          .toDouble();
                    });
                  },
                ),
              if (rightDocked)
                SizedBox(
                  width: rightWidth,
                  child: RightInspector(
                    onClose: () => setState(() => _rightCollapsed = true),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}
