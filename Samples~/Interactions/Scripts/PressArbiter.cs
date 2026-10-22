using System.Collections.Generic;
using UnityEngine;

namespace Woopi.Samples.Interactions
{
    /// <summary>How targets under the same press share it.</summary>
    public enum ClaimMode
    {
        /// <summary>Only the target the press is closest to reacts. A press inside a sprite beats one that only landed in another's padding.</summary>
        ClosestOnly,

        /// <summary>Every target under the press reacts.</summary>
        All,
    }

    /// <summary>Implemented by anything that can win a press from a <see cref="PressArbiter"/>.</summary>
    public interface IPressTarget
    {
        void OnPressClaimed(Vector2 world);
    }

    /// <summary>
    /// Collects every target hit by this frame's press and hands it to the best one. Targets submit from
    /// Update and resolve from LateUpdate, so all of the frame's candidates are known by then. Each kind of
    /// interaction uses its own arbiter, so a tap target and a drag target on the same sprite never compete.
    /// </summary>
    public sealed class PressArbiter
    {
        public static readonly PressArbiter Tap = new();
        public static readonly PressArbiter Drag = new();
        public static readonly PressArbiter MultiTap = new();
        public static readonly PressArbiter Hold = new();

        private readonly List<(IPressTarget target, Vector2 world, bool inside, float distance)> _candidates = new();

        public void Submit(IPressTarget target, Vector2 world, bool inside, float distance) =>
            _candidates.Add((target, world, inside, distance));

        public void Resolve()
        {
            if (_candidates.Count == 0)
                return;

            var best = _candidates[0];
            foreach (var candidate in _candidates)
            {
                if ((candidate.inside && !best.inside)
                    || (candidate.inside == best.inside && candidate.distance < best.distance))
                    best = candidate;
            }

            _candidates.Clear();
            best.target.OnPressClaimed(best.world);
        }

        // Domain reload may be off, so statics would otherwise leak between Play sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearOnPlay()
        {
            Tap._candidates.Clear();
            Drag._candidates.Clear();
            MultiTap._candidates.Clear();
            Hold._candidates.Clear();
        }
    }

    /// <summary>Where a world point lands relative to a sprite's bounds plus some padding.</summary>
    public static class PointerHit
    {
        public static bool Test(SpriteRenderer area, Vector2 world, float padding, out bool inside, out float distance)
        {
            var bounds = area.bounds;
            inside = false;
            distance = 0f;

            if (world.x < bounds.min.x - padding || world.x > bounds.max.x + padding
                || world.y < bounds.min.y - padding || world.y > bounds.max.y + padding)
                return false;

            inside = world.x >= bounds.min.x && world.x <= bounds.max.x
                && world.y >= bounds.min.y && world.y <= bounds.max.y;

            // Distance from the center relative to the sprite's size, so a big sprite is not favoured over a small one.
            var offset = world - (Vector2)bounds.center;
            distance = new Vector2(
                offset.x / Mathf.Max(bounds.extents.x, 0.01f),
                offset.y / Mathf.Max(bounds.extents.y, 0.01f)).magnitude;
            return true;
        }
    }
}
