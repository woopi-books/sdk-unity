using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Woopi.Samples.Interactions
{
    /// <summary>
    /// Counts quick consecutive taps on or near this object's sprite. Use <see cref="onCompleted"/> to react
    /// once the wanted number of taps (2 for a double tap, 3 for a triple tap, any other for a custom counter)
    /// has been reached, <see cref="onTapCounted"/> to react to every tap in the sequence, or
    /// <see cref="onSequenceEnded"/> to learn how many taps a sequence ended up with so single, double and
    /// triple taps can each do something different.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MultiTapTarget : MonoBehaviour, IPressTarget
    {
        [Tooltip("Sprite whose bounds count as the tappable area. Defaults to this object's own sprite.")]
        [SerializeField] private SpriteRenderer tapArea;

        [Tooltip("Extra room around the sprite that still counts as a tap, in world units. Helps with fingers.")]
        [SerializeField, Min(0f)] private float padding = 0.3f;

        [Tooltip("ClosestOnly: of the sprites under a tap, only the nearest counts it. All: every one does.")]
        [SerializeField] private ClaimMode mode = ClaimMode.ClosestOnly;

        [Tooltip("Taps needed to complete: 2 = double tap, 3 = triple tap, or any custom count.")]
        [SerializeField, Min(1)] private int requiredTaps = 2;

        [Tooltip("Longest allowed pause between two taps of the same sequence, in seconds. Waiting longer starts over.")]
        [SerializeField, Min(0.05f)] private float maxInterval = 0.4f;

        [Tooltip("Furthest a tap may land from the previous one and still count towards the sequence, in world units.")]
        [SerializeField, Min(0f)] private float maxTapDrift = 1f;

        [Tooltip("Off: completes once. On: starts counting again after each completion.")]
        [SerializeField] private bool repeatable = true;

        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera cam;

        [Tooltip("Fires every time the required number of taps is reached.")]
        public UnityEvent<Vector2> onCompleted;

        [Tooltip("Fires on every tap of a sequence with the number counted so far (1, 2, 3...).")]
        public UnityEvent<int> onTapCounted;

        [Tooltip("Fires once a sequence is over (after Max Interval with no further tap, or on completion) with how many taps it had.")]
        public UnityEvent<int> onSequenceEnded;

        private int _count;
        private float _lastTapTime;
        private Vector2 _lastTapPosition;

        private void Awake()
        {
            if (tapArea == null)
                tapArea = GetComponent<SpriteRenderer>();
        }

        private void OnDisable() => _count = 0;

        private void Update()
        {
            if (_count > 0 && Time.unscaledTime - _lastTapTime > maxInterval)
                EndSequence();

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return;

            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            var world = (Vector2)cam.ScreenToWorldPoint(pointer.position.ReadValue());
            if (!PointerHit.Test(tapArea, world, padding, out var inside, out var distance))
            {
                // A tap elsewhere breaks the sequence.
                if (_count > 0)
                    EndSequence();
                return;
            }

            if (mode == ClaimMode.All)
                OnPressClaimed(world);
            else
                PressArbiter.MultiTap.Submit(this, world, inside, distance);
        }

        // Every Update of the frame has run by now, so all the targets hit by this tap are known.
        private void LateUpdate() => PressArbiter.MultiTap.Resolve();

        public void OnPressClaimed(Vector2 world)
        {
            // Too far from the last tap: that one ended its sequence and this one starts a new sequence.
            if (_count > 0 && (world - _lastTapPosition).magnitude > maxTapDrift)
                EndSequence();

            _count++;
            _lastTapTime = Time.unscaledTime;
            _lastTapPosition = world;
            onTapCounted?.Invoke(_count);

            if (_count >= requiredTaps)
            {
                onCompleted?.Invoke(world);
                EndSequence();

                if (!repeatable)
                    enabled = false;
            }
        }

        // Called when the sequence times out, is broken, or completes.
        private void EndSequence()
        {
            var taps = _count;
            _count = 0;
            if (taps > 0)
                onSequenceEnded?.Invoke(taps);
        }
    }
}
