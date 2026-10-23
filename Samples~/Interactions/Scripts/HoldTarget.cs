using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Woopi.Samples.Interactions
{
    /// <summary>
    /// Press and hold on or near this object's sprite. Works as a charge ("hold for X seconds to complete":
    /// set <see cref="holdDuration"/> and listen to <see cref="onCharged"/> and <see cref="onProgress"/>), as a
    /// plain hold / release (set the duration to 0 and use <see cref="onHoldStarted"/> and
    /// <see cref="onReleased"/>), or both at once.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HoldTarget : MonoBehaviour, IPressTarget
    {
        [Tooltip("Sprite whose bounds count as the holdable area. Defaults to this object's own sprite.")]
        [SerializeField] private SpriteRenderer holdArea;

        [Tooltip("Extra room around the sprite that still counts, in world units. Helps with fingers.")]
        [SerializeField, Min(0f)] private float padding = 0.3f;

        [Tooltip("ClosestOnly: of the sprites under the press, only the nearest is held. All: every one is.")]
        [SerializeField] private ClaimMode mode = ClaimMode.ClosestOnly;

        [Tooltip("Seconds to hold before it counts as charged. 0 = no charge, only hold start / release events.")]
        [SerializeField, Min(0f)] private float holdDuration = 1.5f;

        [Tooltip("Cancel the hold if the pointer slides off the sprite (plus padding) before it is released.")]
        [SerializeField] private bool cancelWhenPointerLeaves = true;

        [Tooltip("Progress falls back to 0 over this many seconds after an early release, so a charge bar can drain. 0 = instant.")]
        [SerializeField, Min(0f)] private float drainTime = 0.3f;

        [Tooltip("Off: charges once. On: can be charged again after each completion.")]
        [SerializeField] private bool repeatable = true;

        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera cam;

        [Tooltip("Fires when the hold starts.")]
        public UnityEvent<Vector2> onHoldStarted;

        [Tooltip("Fires every frame while holding and draining, with the charge from 0 to 1.")]
        public UnityEvent<float> onProgress;

        [Tooltip("Fires once when the hold lasts as long as Hold Duration.")]
        public UnityEvent onCharged;

        [Tooltip("Fires on every release (or cancel) with how many seconds it was held.")]
        public UnityEvent<float> onReleased;

        [Tooltip("Fires when released or cancelled before the charge completed.")]
        public UnityEvent onCancelled;

        public bool IsHolding { get; private set; }
        public float Progress { get; private set; }

        private float _heldTime;
        private bool _charged;

        private void Awake()
        {
            if (holdArea == null)
                holdArea = GetComponent<SpriteRenderer>();
        }

        private void OnDisable()
        {
            IsHolding = false;
            Progress = 0f;
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null)
                return;

            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            var world = (Vector2)cam.ScreenToWorldPoint(pointer.position.ReadValue());

            if (IsHolding)
            {
                TickHold(pointer, world);
                return;
            }

            DrainProgress();

            if (!pointer.press.wasPressedThisFrame)
                return;

            if (!PointerHit.Test(holdArea, world, padding, out var inside, out var distance))
                return;

            if (mode == ClaimMode.All)
                OnPressClaimed(world);
            else
                PressArbiter.Hold.Submit(this, world, inside, distance);
        }

        private void LateUpdate() => PressArbiter.Hold.Resolve();

        public void OnPressClaimed(Vector2 world)
        {
            IsHolding = true;
            _charged = false;
            _heldTime = 0f;
            onHoldStarted?.Invoke(world);
        }

        private void TickHold(Pointer pointer, Vector2 world)
        {
            var released = !pointer.press.isPressed;
            var left = cancelWhenPointerLeaves && !PointerHit.Test(holdArea, world, padding, out _, out _);

            if (released || left)
            {
                Finish();
                return;
            }

            _heldTime += Time.unscaledDeltaTime;
            Progress = holdDuration > 0f ? Mathf.Clamp01(_heldTime / holdDuration) : 0f;
            onProgress?.Invoke(Progress);

            if (!_charged && holdDuration > 0f && _heldTime >= holdDuration)
            {
                _charged = true;
                onCharged?.Invoke();
            }
        }

        private void Finish()
        {
            IsHolding = false;
            onReleased?.Invoke(_heldTime);

            if (_charged)
            {
                Progress = 0f;
                onProgress?.Invoke(0f);
                if (!repeatable)
                    enabled = false;
            }
            else
            {
                onCancelled?.Invoke();
                if (drainTime <= 0f)
                {
                    Progress = 0f;
                    onProgress?.Invoke(0f);
                }
            }
        }

        private void DrainProgress()
        {
            if (Progress <= 0f)
                return;

            Progress = drainTime > 0f ? Mathf.Max(0f, Progress - Time.unscaledDeltaTime / drainTime) : 0f;
            onProgress?.Invoke(Progress);
        }
    }
}
