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

/// Width/collapse-drag state shared by the Left Rail and Right Inspector
/// panes, since both follow the same anchor-at-drag-start,
/// collapse-past-threshold pattern and only differ in which direction the
/// pane grows relative to the drag offset.
class _PaneResizeState {
  _PaneResizeState({
    required double initialWidth,
    required this.growsWithPositiveOffset,
  }) : width = initialWidth;

  double width;
  // Manually collapsed via drag, independent of the window-size-driven
  // isWide/isMedium/isNarrow breakpoints.
  bool collapsed = false;
  // Whether the divider handle is currently being held, so it stays visible
  // through a drag that collapses the pane but disappears once released
  // while collapsed.
  bool dividerHeld = false;
  final bool growsWithPositiveOffset;
  // Size captured at the start of a drag, used as the anchor for computing
  // the new size directly from the total cursor offset since drag start
  // rather than accumulating per-frame deltas (which drift once a min/max
  // clamp is hit and the drag reverses direction).
  double? _widthAtDragStart;

  void dragStart() {
    _widthAtDragStart = width;
    dividerHeld = true;
  }

  void dragUpdate(
    double totalOffset, {
    required double min,
    required double max,
  }) {
    final anchor = _widthAtDragStart ?? width;
    final signedOffset = growsWithPositiveOffset ? totalOffset : -totalOffset;
    final proposed = anchor + signedOffset;
    // Lower bound intentionally left unclamped past `min` down to the
    // collapse threshold so consecutive small drag offsets below the visual
    // minimum still count toward collapsing, and dragging back the other
    // way un-collapses.
    collapsed = proposed < min - _collapseThreshold;
    width = proposed.clamp(min - _collapseThreshold, max).toDouble();
  }

  void dragEnd() {
    dividerHeld = false;
  }
}

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
  final _left = _PaneResizeState(
    initialWidth: 260,
    growsWithPositiveOffset: true,
  );
  final _right = _PaneResizeState(
    initialWidth: 340,
    growsWithPositiveOffset: false,
  );
  double _devLogHeight = 260;
  double? _devLogHeightAtDragStart;

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

  bool _isRightDocked(double windowWidth) {
    final isWide =
        windowWidth >= _leftMinWidth + _centerMinWidth + _rightMinWidth;
    return isWide && !_right.collapsed;
  }

  // Pops the inspector open like the "show inspector" button would whenever
  // something gets selected but the pane isn't docked, and collapses/undocks
  // it once the selection is cleared (e.g. after a save or delete) while it
  // is docked.
  void _onInspectorTargetChanged(InspectorTarget next, bool rightDocked) {
    if (next.kind != InspectorKind.none && !rightDocked) {
      _scaffoldKey.currentState?.openEndDrawer();
    } else if (next.kind == InspectorKind.none && rightDocked) {
      setState(() => _right.collapsed = true);
    }
  }

  void _showNotificationSnackBar(
    BuildContext context,
    AppNotification? notification,
  ) {
    if (notification == null) return;
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
              Icon(
                notification.icon,
                color: colorScheme.inversePrimary,
                size: 20,
              ),
              const SizedBox(width: AppTheme.spacingSm),
              Expanded(
                child: Text(
                  notification.message,
                  style: TextStyle(color: colorScheme.onInverseSurface),
                ),
              ),
            ],
          ),
        ),
      );
  }

  // Swiping/tapping the scrim to dismiss the end drawer doesn't go through
  // the inspector's own close handler, so the selection would otherwise stay
  // stuck (e.g. re-opening "Add Task" sets an equal InspectorTarget, which
  // Riverpod treats as a no-op and never reopens the drawer).
  void _onEndDrawerChanged(bool isOpened) {
    if (!isOpened &&
        ref.read(selectedInspectorProvider).kind != InspectorKind.none) {
      ref.read(selectedInspectorProvider.notifier).state =
          const InspectorTarget.none();
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
    final rightDockedForInspector = _isRightDocked(
      MediaQuery.sizeOf(context).width,
    );
    ref.listen<InspectorTarget>(
      selectedInspectorProvider,
      (previous, next) =>
          _onInspectorTargetChanged(next, rightDockedForInspector),
    );
    ref.listen<AppNotification?>(
      appNotificationProvider,
      (previous, next) => _showNotificationSnackBar(context, next),
    );

    final devLogOpen = kDebugMode && ref.watch(devLogPanelOpenProvider);

    return LayoutBuilder(
      builder: (context, constraints) =>
          _buildScaffold(context, constraints, devLogOpen),
    );
  }

  Widget _buildScaffold(
    BuildContext context,
    BoxConstraints constraints,
    bool devLogOpen,
  ) {
    final width = constraints.maxWidth;
    final isWide = width >= _leftMinWidth + _centerMinWidth + _rightMinWidth;
    final isMedium = !isWide && width >= _leftMinWidth + _centerMinWidth;
    final isNarrow = !isWide && !isMedium;

    // Docked (in-line) visibility, factoring in manual drag-to-collapse on
    // top of the window-size-driven breakpoints.
    final leftDocked = (isWide || isMedium) && !_left.collapsed;
    final rightDocked = isWide && !_right.collapsed;

    final leftWidth = _left.width
        .clamp(_leftMinWidth, _leftMaxWidth)
        .toDouble();
    final rightWidth = _right.width
        .clamp(_rightMinWidth, _rightMaxWidth)
        .toDouble();
    final devLogMaxHeight = (constraints.maxHeight * 0.7)
        .clamp(_devLogMinHeight, _devLogMaxHeight)
        .toDouble();
    final devLogHeight = _devLogHeight
        .clamp(_devLogMinHeight, devLogMaxHeight)
        .toDouble();

    return Scaffold(
      key: _scaffoldKey,
      drawer: leftDocked ? null : _buildLeftDrawer(context, isNarrow),
      onEndDrawerChanged: _onEndDrawerChanged,
      endDrawer: rightDocked
          ? null
          : _buildRightDrawer(context, isWide, rightWidth),
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
                if ((isWide || isMedium) && (leftDocked || _left.dividerHeld))
                  _buildLeftDivider(width, rightWidth),
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
                if (isWide && (rightDocked || _right.dividerHeld))
                  _buildRightDivider(width, leftWidth),
                if (rightDocked)
                  SizedBox(
                    width: rightWidth,
                    child: RightInspector(
                      onClose: () => setState(() => _right.collapsed = true),
                    ),
                  ),
              ],
            ),
          ),
          if (devLogOpen) ..._buildDevLogSection(devLogHeight, devLogMaxHeight),
        ],
      ),
    );
  }

  Widget _buildLeftDrawer(BuildContext context, bool isNarrow) {
    return Drawer(
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
                    _left.collapsed = false;
                    _left.width = _leftMinWidth;
                  });
                },
              ),
      ),
    );
  }

  Widget _buildRightDrawer(
    BuildContext context,
    bool isWide,
    double rightWidth,
  ) {
    return Drawer(
      width: rightWidth.clamp(_rightMinWidth, 400).toDouble(),
      child: RightInspector(
        headerAction: isWide
            ? IconButton(
                icon: const Icon(Icons.dock),
                tooltip: 'Dock panel',
                onPressed: () {
                  Navigator.of(context).pop();
                  setState(() {
                    _right.collapsed = false;
                    _right.width = _rightMinWidth;
                  });
                },
              )
            : null,
      ),
    );
  }

  Widget _buildLeftDivider(double totalWidth, double rightWidth) {
    return ResizableDivider(
      onDragStart: () => setState(_left.dragStart),
      onDragUpdate: (totalOffset) {
        final maxLeft =
            (totalWidth - rightWidth - _centerMinWidth - _dividerWidth * 2)
                .clamp(_leftMinWidth, _leftMaxWidth)
                .toDouble();
        setState(
          () => _left.dragUpdate(totalOffset, min: _leftMinWidth, max: maxLeft),
        );
      },
      onDragEnd: () => setState(_left.dragEnd),
    );
  }

  Widget _buildRightDivider(double totalWidth, double leftWidth) {
    return ResizableDivider(
      onDragStart: () => setState(_right.dragStart),
      onDragUpdate: (totalOffset) {
        final maxRight =
            (totalWidth - leftWidth - _centerMinWidth - _dividerWidth * 2)
                .clamp(_rightMinWidth, _rightMaxWidth)
                .toDouble();
        setState(
          () => _right.dragUpdate(
            totalOffset,
            min: _rightMinWidth,
            max: maxRight,
          ),
        );
      },
      onDragEnd: () => setState(_right.dragEnd),
    );
  }

  List<Widget> _buildDevLogSection(
    double devLogHeight,
    double devLogMaxHeight,
  ) {
    return [
      ResizableDivider(
        axis: Axis.vertical,
        onDragStart: () => _devLogHeightAtDragStart = _devLogHeight,
        onDragUpdate: (totalOffset) {
          final proposed =
              (_devLogHeightAtDragStart ?? _devLogHeight) - totalOffset;
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
              ref.read(devLogPanelOpenProvider.notifier).state = false,
        ),
      ),
    ];
  }
}
