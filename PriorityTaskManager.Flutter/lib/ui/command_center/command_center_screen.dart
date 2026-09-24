import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../providers/app_notifications_provider.dart';
import '../../providers/dev_log_provider.dart';
import '../../providers/selection_provider.dart';
import '../../providers/task_providers.dart';
import '../../models/task_list.dart';
import '../dev/dev_log_panel.dart';
import '../theme/app_theme.dart';
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
const double _devLogMinHeight = 120;
const double _devLogMaxHeight = 480;

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
  double _devLogHeight = 260;
  // Sizes captured at the start of a drag, used as the anchor for computing
  // the new size directly from the total cursor offset since drag start
  // rather than accumulating per-frame deltas (which drift once a min/max
  // clamp is hit and the drag reverses direction).
  double? _leftWidthAtDragStart;
  double? _rightWidthAtDragStart;
  double? _devLogHeightAtDragStart;
  // Manually collapsed via drag, independent of the window-size-driven
  // isWide/isMedium/isNarrow breakpoints.
  bool _leftCollapsed = false;
  bool _rightCollapsed = false;
  // Whether the left divider's handle is currently being held, so it stays
  // visible through a drag that collapses the rail but disappears once
  // released while collapsed.
  bool _leftDividerHeld = false;
  // Whether the right divider's handle is currently being held, so it stays
  // visible through a drag that collapses the inspector but disappears once
  // released while collapsed.
  bool _rightDividerHeld = false;

  // Selects the first list whenever nothing is selected yet, or whenever the
  // currently selected id no longer exists in the loaded lists (e.g. it was
  // left over from a different session/account) — otherwise a stale id
  // lingers un-highlighted in the Left Rail while still being queried for
  // tasks/events, making it look like "no list" is selected.
  void _ensureValidSelection(AsyncValue<List<TaskList>> listsAsync) {
    final lists = listsAsync.asData?.value;
    if (lists == null || lists.isEmpty) return;
    final currentId = ref.read(activeListIdProvider);
    final stillValid = currentId != null && lists.any((l) => l.id == currentId);
    if (!stillValid) {
      ref.read(activeListIdProvider.notifier).state = lists.first.id;
    }
  }

  @override
  void initState() {
    super.initState();
    // Ensure a list is selected once lists finish loading, so the pipeline
    // and inspector have something to show by default.
    Future.microtask(() => _ensureValidSelection(ref.read(taskListsProvider)));
  }

  @override
  Widget build(BuildContext context) {
    // Keep a valid list selected by default without fighting user
    // navigation once something is already (still validly) selected.
    ref.listen(
      taskListsProvider,
      (previous, next) => _ensureValidSelection(next),
    );

    // ref.listen must run directly in build(), not inside a nested builder
    // closure, so the right-docked check is mirrored here off MediaQuery
    // rather than the LayoutBuilder constraints used below.
    final mediaWidth = MediaQuery.sizeOf(context).width;
    final isWideForInspector =
        mediaWidth >= _leftMinWidth + _centerMinWidth + _rightMinWidth;
    final rightDockedForInspector = isWideForInspector && !_rightCollapsed;

    // Pop the inspector open like the "show inspector" button would
    // whenever something gets selected but the pane isn't docked, and
    // collapse/undock it once the selection is cleared (e.g. after a save
    // or delete) while it is docked.
    ref.listen<InspectorTarget>(selectedInspectorProvider, (previous, next) {
      if (next.kind != InspectorKind.none && !rightDockedForInspector) {
        _scaffoldKey.currentState?.openEndDrawer();
      } else if (next.kind == InspectorKind.none && rightDockedForInspector) {
        setState(() => _rightCollapsed = true);
      }
    });

    ref.listen<AppNotification?>(appNotificationProvider, (previous, next) {
      if (next == null) return;
      final colorScheme = Theme.of(context).colorScheme;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            backgroundColor: colorScheme.inverseSurface,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(AppTheme.radiusMd),
            ),
            margin: const EdgeInsets.all(AppTheme.spacingMd),
            duration: const Duration(seconds: 2),
            content: Row(
              children: [
                Icon(next.icon, color: colorScheme.inversePrimary, size: 20),
                const SizedBox(width: AppTheme.spacingSm),
                Expanded(
                  child: Text(
                    next.message,
                    style: TextStyle(color: colorScheme.onInverseSurface),
                  ),
                ),
              ],
            ),
          ),
        );
    });

    final devLogOpen = kDebugMode && ref.watch(devLogPanelOpenProvider);

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
        final devLogMaxHeight = (constraints.maxHeight * 0.7).clamp(
          _devLogMinHeight,
          _devLogMaxHeight,
        );
        final devLogHeight = _devLogHeight
            .clamp(_devLogMinHeight, devLogMaxHeight)
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
          // Swiping/tapping the scrim to dismiss the end drawer doesn't go
          // through _closeInspector, so the selection would otherwise stay
          // stuck (e.g. re-opening "Add Task" sets an equal InspectorTarget,
          // which Riverpod treats as a no-op and never reopens the drawer).
          onEndDrawerChanged: (isOpened) {
            if (!isOpened &&
                ref.read(selectedInspectorProvider).kind !=
                    InspectorKind.none) {
              ref.read(selectedInspectorProvider.notifier).state =
                  const InspectorTarget.none();
            }
          },
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
          body: Column(
            children: [
              Expanded(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (leftDocked)
                      SizedBox(width: leftWidth, child: const LeftRail()),
                    // Only rendered while docked or mid-drag, so the handle
                    // disappears once the rail is collapsed and released.
                    if ((isWide || isMedium) &&
                        (leftDocked || _leftDividerHeld))
                      ResizableDivider(
                        onDragStart: () {
                          _leftWidthAtDragStart = _leftWidth;
                          setState(() => _leftDividerHeld = true);
                        },
                        onDragUpdate: (totalOffset) {
                          final maxLeft =
                              (width -
                                      rightWidth -
                                      _centerMinWidth -
                                      _dividerWidth * 2)
                                  .clamp(_leftMinWidth, _leftMaxWidth);
                          final proposed =
                              (_leftWidthAtDragStart ?? _leftWidth) +
                              totalOffset;
                          // Lower bound intentionally left unclamped (down to
                          // the collapse threshold) so consecutive small drag
                          // offsets below the visual minimum still count
                          // toward collapsing, and dragging back the other
                          // way un-collapses.
                          setState(() {
                            _leftCollapsed =
                                proposed < _leftMinWidth - _collapseThreshold;
                            _leftWidth = proposed
                                .clamp(
                                  _leftMinWidth - _collapseThreshold,
                                  maxLeft,
                                )
                                .toDouble();
                          });
                        },
                        onDragEnd: () =>
                            setState(() => _leftDividerHeld = false),
                      ),
                    Expanded(
                      child: CenterStage(
                        showHamburger: !leftDocked,
                        onOpenLeftRail: () =>
                            _scaffoldKey.currentState?.openDrawer(),
                        showInspectorToggle: !rightDocked,
                        onOpenInspector: () =>
                            _scaffoldKey.currentState?.openEndDrawer(),
                      ),
                    ),
                    // Only rendered while docked or mid-drag, so the handle
                    // disappears once the inspector is collapsed and released.
                    if (isWide && (rightDocked || _rightDividerHeld))
                      ResizableDivider(
                        onDragStart: () {
                          _rightWidthAtDragStart = _rightWidth;
                          setState(() => _rightDividerHeld = true);
                        },
                        onDragUpdate: (totalOffset) {
                          final maxRight =
                              (width -
                                      leftWidth -
                                      _centerMinWidth -
                                      _dividerWidth * 2)
                                  .clamp(_rightMinWidth, _rightMaxWidth);
                          final proposed =
                              (_rightWidthAtDragStart ?? _rightWidth) -
                              totalOffset;
                          // See the left divider's onDragUpdate for why the
                          // lower bound is intentionally left unclamped here.
                          setState(() {
                            _rightCollapsed =
                                proposed < _rightMinWidth - _collapseThreshold;
                            _rightWidth = proposed
                                .clamp(
                                  _rightMinWidth - _collapseThreshold,
                                  maxRight,
                                )
                                .toDouble();
                          });
                        },
                        onDragEnd: () =>
                            setState(() => _rightDividerHeld = false),
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
              ),
              if (devLogOpen) ...[
                ResizableDivider(
                  axis: Axis.vertical,
                  onDragStart: () => _devLogHeightAtDragStart = _devLogHeight,
                  onDragUpdate: (totalOffset) {
                    final proposed =
                        (_devLogHeightAtDragStart ?? _devLogHeight) -
                        totalOffset;
                    setState(() {
                      _devLogHeight = proposed
                          .clamp(_devLogMinHeight, devLogMaxHeight)
                          .toDouble();
                    });
                  },
                ),
                SizedBox(
                  height: devLogHeight,
                  child: DevLogPanel(
                    onClose: () =>
                        ref.read(devLogPanelOpenProvider.notifier).state =
                            false,
                  ),
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}
