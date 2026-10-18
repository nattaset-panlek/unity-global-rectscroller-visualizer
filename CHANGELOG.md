# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-10-18

### Added

- **Layouts**
  - Flow layout: items wrap like tags.
  - Cross-axis alignment for Vertical and Horizontal lists.
- **Item sizes**
  - Size modes: Fixed, Provider (`SizeProvider`) and Measure (reads the bound item's layout).
  - Unmeasured items use the average measured size. The first visible item stays in place when sizes change.
- **Empty placeholder** (`EmptyPrefab`), shown when the data source is set but empty.
- **Selection:** None, Single or Multi (Shift range, Ctrl add/remove), with `OnSelectionChanged`. `VisualItemInfo.IsSelected` and `IsFocused` are passed to items.
- **Focus and navigation**
  - Keyboard and gamepad navigation through the EventSystem: `Focus`, `MoveFocus`, `OnFocusChanged`, `OnNavigateOut`.
  - Focus survives views being recycled.
- **Pointer events:** `OnItemClicked`, `OnItemDoubleClicked`, `OnItemRightClicked`, `OnItemSubmitted`.
- **`FocusOnClick`:** a click also selects the item in the EventSystem. Turn it off when the navigation keys also drive gameplay.
- **Drag-or-scroll** decided by angle (`ItemDragAngle`), with the `IVisualDragHandler` bridge.
- **Scrolling helpers:** `EnsureVisible`, and optional snapping to item edges (`SnapToItems`).
- **`IVisualItemState`** for selection and focus visuals without a rebind.
- **Editor**
  - Hierarchy menu: **Flow (Tags)**, plus an Empty Template for every new visualizer.
  - Inspector warnings for size modes.
- **Interaction Demo** sample.
- **Showcase** sample scene by Axnise. It has four pages: interactive lessons, a playground, a benchmark against ScrollRect + LayoutGroup, and an in-scene self test.
- `VisualViewportWatcher`: the view refreshes when its viewport is resized on its own, for example when a ScrollRect auto-hides a scrollbar.

### Changed

- The zero-garbage scrolling test now measures managed heap growth (`GC.GetTotalMemory`). `GC.GetAllocatedBytesForCurrentThread` always returns 0 in Unity's Mono, so the old test could not fail.
- The view now runs in Edit Mode once it has data (tests, previews and edit-mode tools), so viewport resizes refresh it.
- A resize that arrives during a canvas layout or graphic rebuild is refreshed right after the rebuild (`Canvas.willRenderCanvases`) instead of inside it. This avoids uGUI's "Trying to remove ... from rebuild list" errors when a LayoutGroup drives the visualizer's size.

## [0.1.0] - 2026-10-18

### Added

- **Core**
  - Virtualized `GlobalVisualizerView` with pooled views.
  - Vertical, Horizontal and Grid (Fit, Fixed Columns, Fixed Rows) layouts.
- **Binding:** callback or `IVisualItem<T>`, plus `SetCount` for index-only data.
- **API:** `Notify*` change notifications, `ScrollTo` and `OnVisibleRangeChanged`.
- **Editor tools**
  - Hierarchy menus and the Create Item Template wizard.
  - Edit Mode preview that is never saved.
  - Scene view overlay.
- **Basic Demo** sample and EditMode tests.
