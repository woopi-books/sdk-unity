using UnityEngine;

namespace Woopi.Samples.Interactions
{
    /// <summary>
    /// Visible feedback for the demo scenes: hook its methods to any interaction event and the sprite
    /// blinks, grows or tints, so it is easy to see which targets reacted. Not needed in your own books.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DemoFeedback : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.yellow;
        [SerializeField] private Color progressColor = Color.green;
        [SerializeField, Min(0.05f)] private float duration = 0.3f;

        private SpriteRenderer _renderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private float _remaining;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseColor = _renderer.color;
            _baseScale = transform.localScale;
        }

        public void Flash(Vector2 world)
        {
            Blink();
            Debug.Log($"[Interactions] {name} reacted at {world}", this);
        }

        public void FlashEmpty()
        {
            Blink();
            Debug.Log($"[Interactions] {name} reacted", this);
        }

        public void FlashSwipe(SwipeDirection direction)
        {
            Blink();
            Debug.Log($"[Interactions] {name}: swipe {direction}", this);
        }

        public void FlashCount(int count)
        {
            Blink();
            Debug.Log($"[Interactions] {name}: {count}", this);
        }

        public void LogHeld(float seconds) => Debug.Log($"[Interactions] {name} released after {seconds:0.00}s", this);

        /// <summary>Tints and grows the sprite from 0 to 1, e.g. to show a charge filling up.</summary>
        public void SetProgress(float progress)
        {
            if (_remaining > 0f)
                return;

            _renderer.color = Color.Lerp(_baseColor, progressColor, progress);
            transform.localScale = _baseScale * (1f + 0.3f * progress);
        }

        private void Blink() => _remaining = duration;

        private void Update()
        {
            if (_remaining <= 0f)
                return;

            _remaining -= Time.deltaTime;
            _renderer.color = _remaining > 0f ? flashColor : _baseColor;
        }
    }
}
