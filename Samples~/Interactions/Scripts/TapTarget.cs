using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Woopi.Samples.Interactions
{
    /// <summary>
    /// Fires <see cref="onTapped"/> when the reader taps (or clicks) on or near this object's sprite.
    /// Pick <see cref="ClaimMode.ClosestOnly"/> when several sprites sit close together and only the nearest
    /// should react, or <see cref="ClaimMode.All"/> when every sprite under the tap should.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TapTarget : MonoBehaviour, IPressTarget
    {
        [Tooltip("Sprite whose bounds count as the tappable area. Defaults to this object's own sprite.")]
        [SerializeField] private SpriteRenderer tapArea;

        [Tooltip("Extra room around the sprite that still counts as a tap, in world units. Helps with fingers.")]
        [SerializeField, Min(0f)] private float padding = 0.3f;

        [Tooltip("ClosestOnly: of the sprites under a tap, only the nearest reacts. All: every one reacts.")]
        [SerializeField] private ClaimMode mode = ClaimMode.ClosestOnly;

        [Tooltip("Off: reacts to the first tap only. On: reacts to every tap.")]
        [SerializeField] private bool repeatable = true;

        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera cam;

        [Tooltip("Fires with the world position of the tap.")]
        public UnityEvent<Vector2> onTapped;

        private void Awake()
        {
            if (tapArea == null)
                tapArea = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return;

            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            var world = (Vector2)cam.ScreenToWorldPoint(pointer.position.ReadValue());
            if (!PointerHit.Test(tapArea, world, padding, out var inside, out var distance))
                return;

            if (mode == ClaimMode.All)
                OnPressClaimed(world);
            else
                PressArbiter.Tap.Submit(this, world, inside, distance);
        }

        // Every Update of the frame has run by now, so all the targets hit by this tap are known.
        private void LateUpdate() => PressArbiter.Tap.Resolve();

        public void OnPressClaimed(Vector2 world)
        {
            if (!repeatable)
                enabled = false;

            onTapped?.Invoke(world);
        }
    }
}
