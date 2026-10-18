# Global Visualizer

Virtualized lists, grids and tag flows for Unity uGUI. Give it a ScrollRect, any prefab and any data, and it creates only the items you can see, then reuses their GameObjects as you scroll. 10,000 items cost about as much as 20.

By **Axnise**. Thai version: [README_TH.md](README_TH.md). Changes: [CHANGELOG.md](CHANGELOG.md). License: MIT.

## Features

- **Any prefab, any data.** Bind with a callback, or with `IVisualItem<T>` on any component in the prefab. No base class to inherit.
- **Layouts.** Vertical, Horizontal, Grid (Fit, Fixed Columns, Fixed Rows) and Flow (items wrap like tags).
- **Item sizes.** Fixed, Provider (sizes from your code) or Measure (read from the prefab's layout components after binding). Grid always uses one cell size.
- **Zero allocation while scrolling.** Views are pooled and component lookups are cached.
- **Interaction.**
  - Click, double click, right click and submit events.
  - Single or multi selection (Shift for a range, Ctrl for add/remove).
  - Keyboard and gamepad navigation through the EventSystem, including into items that are not created yet.
  - Drag-or-scroll: a drag across the scroll axis starts an item drag through `IVisualDragHandler`, along it scrolls.
  - Optional snapping to item edges.
- **Empty placeholder.** Shown when the data source is set but empty.
- **Editor tools.**
  - Hierarchy menu **Global Visualizer > Vertical List / Horizontal List / Grid / Flow (Tags)**.
  - **Create Item Template...** wizard that writes a prefab and an `IVisualItem<T>` script.
  - Edit Mode preview that is never saved.
  - Scene view overlay of the viewport, buffer and alive items.

## Requirements

- Unity 2022.3 or newer.
- uGUI (`com.unity.ugui`).
- Optional:
  - Input System, for modifier keys and the sample event system;
  - TextMeshPro, used by the template wizard when installed.

## Install

Pick one:

- **Package Manager > + > Install package from git URL...** with the repository URL of this package folder.
- **Package Manager > + > Install package from disk...** and pick `package.json`.
- Copy the folder into your project's `Packages/` folder (embedded package).

Then open **Package Manager > Global Visualizer > Samples** and import **Showcase**. Open `GlobalVisualizerShowcase.unity` and press Play. It contains:

- **Learn:** nine interactive lessons with goals that tick off as you try them, plus the code for each step.
- **Playground:** every setting live.
- **Benchmark:** a normal ScrollRect + LayoutGroup against Global Visualizer with the same data. It compares build time, GameObjects, memory, frame time, heap growth and the cost of a change.
- **Self Test:** the package guarantees checked in your build.

**Basic Demo** and **Interaction Demo** are smaller samples. Put the demo component on an empty GameObject and press Play.

## Quick start

1. Right-click in the Hierarchy: **Global Visualizer > Vertical List**.
   - You get a ScrollRect, a `GlobalVisualizerView` and an inactive **Item Template**.
2. Optional: right-click it again and choose **Create Item Template...** to generate your own prefab and script.
3. Bind data from code:

```csharp
using GlobalVisualizer;

public sealed class ScoreBoard : MonoBehaviour
{
    [SerializeField] private GlobalVisualizerView _view;
    private readonly List<Score> _scores = new List<Score>();

    private void Start()
    {
        _view.SetItems(_scores, (go, score, index) => go.GetComponentInChildren<Text>().text = score.Name);
    }

    public void Add(Score score)
    {
        _scores.Add(score);
        _view.NotifyInserted(_scores.Count - 1, 1);
    }
}
```

The same with an item component (no `GetComponent` per bind, no allocation):

```csharp
public sealed class ScoreRowView : MonoBehaviour, IVisualItem<Score>
{
    [SerializeField] private Text _name;
    public void Bind(Score data, VisualItemInfo info) => _name.text = data.Name;
    public void Unbind() { }
}

_view.SetItems(_scores);
```

## API overview

| Area | Members |
|---|---|
| Data | `SetItems<T>(list, onBind)`, `SetCount(count, onBind)`, `Clear()`, `Count` |
| Changes | `NotifyChanged(i)`, `NotifyRangeChanged(i, n)`, `NotifyInserted(i, n)`, `NotifyRemoved(i, n)`, `NotifyReset()`, `Refresh()` |
| Layout | `SetLayout(VisualLayoutSettings)`, `VisualLayoutSettings.Vertical / Horizontal / Grid / Flow(...)`, `CrossAlignment` |
| Sizes | `SizeMode` (Fixed, Provider, Measure), `SizeProvider`, `InvalidateSizes()` |
| Scrolling | `ScrollTo(i, alignment)`, `EnsureVisible(i)`, `ScrollPosition`, `SnapToItems`, `OnVisibleRangeChanged` |
| Selection | `SelectionMode`, `Select(i)`, `ClearSelection()`, `IsSelected(i)`, `SelectedIndex`, `GetSelected(list)`, `OnSelectionChanged` |
| Focus | `Focus(i)`, `MoveFocus(direction)`, `FocusedIndex`, `OnFocusChanged`, `OnNavigateOut` |
| Pointer | `OnItemClicked`, `OnItemDoubleClicked`, `OnItemRightClicked`, `OnItemSubmitted`, `FocusOnClick` (turn off when WASD also moves the player) |
| Drag | `DragHandler` (`IVisualDragHandler`), `ItemDragAngle` |
| Views | `GetView(i)`, `VisibleRange`, `ActiveViewCount`, `CreatedViewCount`, `EmptyPrefab`, `IsShowingEmpty` |

Item components can also implement `IVisualItemState` to update selection and focus visuals without a full rebind. `VisualItemInfo` carries `Index`, `Owner`, `IsSelected` and `IsFocused`.

## Size modes

- **Fixed:** every item is `Item Size` (or the prefab size where it is 0). Fastest.
- **Provider:** `view.SizeProvider = index => new Vector2(0f, rows[index].IsHeader ? 32f : 56f);` Sizes are cached; call `NotifyChanged(i)` when one changes.
- **Measure:** after binding, the item root is measured with `LayoutUtility` (it needs a Layout Group, Layout Element or text on the root). Unmeasured items use the average of measured ones, and the first visible item stays still when sizes are corrected.

## Drag or scroll

Set `view.DragHandler` to your own `IVisualDragHandler`. When the user drags an item:

- across the scroll axis (more than `ItemDragAngle`, default 45 degrees), your handler gets the drag;
- along it, the list scrolls.

Items that already implement Unity drag handlers themselves are left alone.

## Common mistakes

- **Content LayoutGroup or ContentSizeFitter.** The visualizer positions items itself and disables them (with a warning). Remove them.
- **Rebuilding on every change.** Call one `Notify*` per change, or `NotifyReset()` once after a batch.
- **State stored on views.** Views are reused for other indices. Keep state in your data, or use selection, and apply it in `Bind`.
- **Measure mode without a layout component on the item root.** The size cannot be read and the estimate is used. The Inspector warns about this.

## Tests

The package ships EditMode tests (`Tests/Editor`). Add the package to `testables` in `Packages/manifest.json` to run them in another project:

```json
"testables": ["com.globalvisualizer.core"]
```

## License

MIT. See [LICENSE](LICENSE).
