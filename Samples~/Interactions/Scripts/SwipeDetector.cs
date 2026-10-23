using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Woopi.Samples.Interactions
{
    public enum SwipeDirection { Left, Right, Up, Down }

    [Flags]
    public enum SwipeDirections
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
        Horizontal = Left | Right,
        Vertical = Up | Down,
        Any = Horizontal | Vertical,
    }

    /// <summary>
    /// Detects a swipe anywhere on screen, or only if it starts on a sprite. Choose which directions count
    /// (any, horizontal only, left only...) and listen to <see cref="onSwipe"/> for all of them or to a
    /// per-direction event. Fires when the finger is released.
    /// </summary>
    public sealed class SwipeDetector : MonoBehaviour
    {
        [Tooltip("Which swipe directions are reported. Any other direction is ignored.")]
        [SerializeField] private SwipeDirections accepted = SwipeDirections.Any;

        [Tooltip("Shortest travel that counts as a swipe, as a fraction of the screen height. Shorter is a tap.")]
        [SerializeField, Range(0.02f, 0.9f)] private float minDistance = 0.12f;

        [Tooltip("Longest a swipe may take, in seconds. Slower movement is a slow drag, not a swipe.")]
        [SerializeField, Min(0.05f)] private float maxDuration = 0.6f;

        [Tooltip("How far from a pure horizontal / vertical line a swipe may stray, in degrees.")]
        [SerializeField, Range(1f, 45f)] private float angleTolerance = 35f;

        [Tooltip("Optional: the swipe must start on this sprite. Empty: anywhere on screen.")]
        [SerializeField] private SpriteRenderer startArea;

        [Tooltip("Extra room around the start area, in world units.")]
        [SerializeField, Min(0f)] private float padding = 0.3f;

        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera cam;

        [Tooltip("Fires for every accepted swipe, with its direction.")]
        public UnityEvent<SwipeDirection> onSwipe;

        public UnityEvent onSwipeLeft;
        public UnityEvent onSwipeRight;
        public UnityEvent onSwipeUp;
        public UnityEvent onSwipeDown;

        private bool _tracking;
        private Vector2 _startScreen;
        private float _startTime;

        private void OnDisable() => _tracking = false;

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null)
                return;

            if (pointer.press.wasPressedThisFrame)
                Begin(pointer.position.ReadValue());
            else if (_tracking && pointer.press.wasReleasedThisFrame)
                End(pointer.position.ReadValue());
        }

        private void Begin(Vector2 screen)
        {
            _tracking = false;

            if (startArea != null)
            {
                if (cam == null)
                    cam = Camera.main;
                if (cam == null)
                    return;

                var world = (Vector2)cam.ScreenToWorldPoint(screen);
                if (!PointerHit.Test(startArea, world, padding, out _, out _))
                    return;
            }

            _tracking = true;
            _startScreen = screen;
            _startTime = Time.unscaledTime;
        }

        private void End(Vector2 screen)
        {
            _tracking = false;

            var delta = screen - _startScreen;
            if (delta.magnitude < minDistance * Screen.height || Time.unscaledTime - _startTime > maxDuration)
                return;

            if (!TryGetDirection(delta, out var direction) || !IsAccepted(direction))
                return;

            onSwipe?.Invoke(direction);
            switch (direction)
            {
                case SwipeDirection.Left: onSwipeLeft?.Invoke(); break;
                case SwipeDirection.Right: onSwipeRight?.Invoke(); break;
                case SwipeDirection.Up: onSwipeUp?.Invoke(); break;
                case SwipeDirection.Down: onSwipeDown?.Invoke(); break;
            }
        }

        private bool TryGetDirection(Vector2 delta, out SwipeDirection direction)
        {
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg; // 0 = right, 90 = up
            direction = SwipeDirection.Right;

            if (Mathf.Abs(Mathf.DeltaAngle(angle, 0f)) <= angleTolerance) direction = SwipeDirection.Right;
            else if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) <= angleTolerance) direction = SwipeDirection.Left;
            else if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) <= angleTolerance) direction = SwipeDirection.Up;
            else if (Mathf.Abs(Mathf.DeltaAngle(angle, -90f)) <= angleTolerance) direction = SwipeDirection.Down;
            else return false; // a diagonal that is not close enough to any axis

            return true;
        }

        private bool IsAccepted(SwipeDirection direction) => direction switch
        {
            SwipeDirection.Left => (accepted & SwipeDirections.Left) != 0,
            SwipeDirection.Right => (accepted & SwipeDirections.Right) != 0,
            SwipeDirection.Up => (accepted & SwipeDirections.Up) != 0,
            _ => (accepted & SwipeDirections.Down) != 0,
        };
    }
}
