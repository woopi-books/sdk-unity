using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Woopi.Samples.Interactions
{
    /// <summary>
    /// Press on or near this sprite and drag. Raises start / drag / end events and can optionally move the
    /// object with the pointer. <see cref="ClaimMode.ClosestOnly"/> lets only the nearest sprite under the
    /// press be dragged; <see cref="ClaimMode.All"/> drags every sprite under it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DragTarget : MonoBehaviour, IPressTarget
    {
        [Tooltip("Sprite whose bounds count as the grabbable area. Defaults to this object's own sprite.")]
        [SerializeField] private SpriteRenderer dragArea;

        [Tooltip("Extra room around the sprite that still counts as a grab, in world units.")]
        [SerializeField, Min(0f)] private float padding = 0.3f;

        [Tooltip("ClosestOnly: of the sprites under the press, only the nearest is dragged. All: every one is.")]
        [SerializeField] private ClaimMode mode = ClaimMode.ClosestOnly;

        [Tooltip("Move this object along with the pointer. Off: only raise the events and let a script do the moving.")]
        [SerializeField] private bool moveWithPointer = true;

        [Tooltip("Keep the grab point under the finger instead of snapping the object's pivot to it.")]
        [SerializeField] private bool keepGrabOffset = true;

        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera cam;

        public UnityEvent<Vector2> onDragStarted;

        [Tooltip("Fires every frame of the drag with the pointer's world position.")]
        public UnityEvent<Vector2> onDragged;

        public UnityEvent<Vector2> onDragEnded;

        public bool IsDragging { get; private set; }

        private Vector2 _grabOffset;

        private void Awake()
        {
            if (dragArea == null)
                dragArea = GetComponent<SpriteRenderer>();
        }

        private void OnDisable() => IsDragging = false;

        private bool TryGetWorld(Pointer pointer, out Vector2 world)
        {
            world = default;
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return false;

            world = cam.ScreenToWorldPoint(pointer.position.ReadValue());
            return true;
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !TryGetWorld(pointer, out var world))
                return;

            if (IsDragging)
            {
                if (!pointer.press.isPressed)
                {
                    IsDragging = false;
                    onDragEnded?.Invoke(world);
                    return;
                }

                if (moveWithPointer)
                {
                    var target = world + _grabOffset;
                    transform.position = new Vector3(target.x, target.y, transform.position.z);
                }

                onDragged?.Invoke(world);
                return;
            }

            if (!pointer.press.wasPressedThisFrame)
                return;

            if (!PointerHit.Test(dragArea, world, padding, out var inside, out var distance))
                return;

            if (mode == ClaimMode.All)
                OnPressClaimed(world);
            else
                PressArbiter.Drag.Submit(this, world, inside, distance);
        }

        private void LateUpdate() => PressArbiter.Drag.Resolve();

        public void OnPressClaimed(Vector2 world)
        {
            IsDragging = true;
            _grabOffset = keepGrabOffset ? (Vector2)transform.position - world : Vector2.zero;
            onDragStarted?.Invoke(world);
        }
    }
}
