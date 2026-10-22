# Interactions

Reusable pointer interactions for books. Each one is a component you add to a sprite, with UnityEvents you wire
to whatever should happen. Every interaction has its own demo scene in `Scenes/`.

All of them use the Input System (`Pointer.current`), so mouse and touch behave the same, and they read
`Camera.main` unless you assign a camera.

## Who reacts when sprites overlap

Tap, MultiTap, Drag and Hold have a **Mode**:

- **ClosestOnly**: of the sprites under the press, only one reacts. A press inside a sprite's bounds beats one that only
  landed in another sprite's padding, and among equals the sprite whose centre is nearest (relative to its size) wins.
- **All**: every sprite under the press reacts.

Each interaction type has its own arbiter, so a TapTarget and a HoldTarget on the same sprite never compete.

## Components

| Component | Scene | What it does |
|---|---|---|
| `TapTarget` | `Tap` | One tap on or near the sprite. `onTapped(Vector2)`. |
| `MultiTapTarget` | `MultiTap` | Counts quick taps. `requiredTaps` 2 = double tap, 3 = triple tap, any other number = custom counter. `onCompleted`, `onTapCounted(int)` for each tap, `onSequenceEnded(int)` with the final count so 1, 2 and 3 taps can do different things. |
| `DragTarget` | `Drag` | Press and drag. Optionally moves the sprite (keeping the grab offset). `onDragStarted`, `onDragged`, `onDragEnded`. |
| `HoldTarget` | `Hold` | Charge: hold for `holdDuration` seconds, `onProgress(0..1)` then `onCharged`. Hold / release: set the duration to 0 and use `onHoldStarted` and `onReleased(seconds)`. `onCancelled` if let go early. |
| `SwipeDetector` | `Swipe` | Swipe anywhere or starting on a sprite. Choose the accepted directions; `onSwipe(direction)` plus one event per direction. Fires on release. |

`DemoFeedback` only exists to make the demo scenes visible (flashes, tints, logs). You don't need it in your own books.

## Using one

1. Add the component to a GameObject with a `SpriteRenderer`.
2. Pick the mode and tune padding / timings.
3. Add listeners to the events in the Inspector.
