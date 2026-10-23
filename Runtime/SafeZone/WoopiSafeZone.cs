using System.Runtime.InteropServices;
using UnityEngine;

namespace Woopi.Core.SafeZone
{
    /// <summary>
    /// Where the Woopi WebGL template turns pages by itself, so a book can keep its own interactions away from it.
    /// The template (WebGLTemplate~/index.html) listens on the whole window, not on the book, and never
    /// cancels the input, so the book receives those touches too. Two gestures turn the page:
    ///
    ///  - a double tap inside an edge zone: the left zone goes back, the right zone goes forward;
    ///  - a fast horizontal swipe anywhere: left goes forward, right goes back.
    ///
    /// A single tap is always left to the book. The constants below mirror the template's page-turn settings and
    /// must be kept in sync with them.
    ///
    /// The Woopi reader page (the host, outside the book) also has a dock at the bottom: moving the pointer into a thin
    /// strip along the bottom edge (<see cref="DockHotzoneHeightCssPx"/>), or touching it on a mobile device, opens a menu
    /// (language, restart, exit, full screen). A tap or a drag the book wants down there can open it instead, so keep book
    /// interactions out of that strip. It is part of the screen the safe area leaves out.
    ///
    /// Everything here is in Unity screen pixels (origin bottom-left), the space of Screen.width and
    /// Input positions. The template measures in browser CSS pixels, so the edge zone's minimum width is scaled by
    /// the device pixel ratio.
    /// </summary>
    public static class WoopiSafeZone
    {
        /// <summary>Each edge zone is this fraction of the width...</summary>
        public const float EdgeZoneRatio = 0.18f;

        /// <summary>...or at least this many CSS pixels, whichever is wider.</summary>
        public const float EdgeZoneMinCssPx = 80f;

        /// <summary>
        /// Height of the reader dock's hotzone: the strip along the bottom edge, across the full width, where the pointer
        /// opens the host's menu (a touch does too on mobile). It is set by the host page (<c>.reader-dock__hotzone</c>, inline style), so check it
        /// there if the reader is updated.
        /// </summary>
        public const float DockHotzoneHeightCssPx = 26f;

        /// <summary>A page-turning swipe travels at least this fraction of the width...</summary>
        public const float SwipeMinDistanceRatio = 0.18f;

        /// <summary>...or at least this many CSS pixels, whichever is more.</summary>
        public const float SwipeMinDistanceCssPx = 70f;

        /// <summary>...takes at most this long...</summary>
        public const float SwipeMaxSeconds = 0.65f;

        /// <summary>...and is at least this many times more horizontal than vertical.</summary>
        public const float SwipeHorizontalRatio = 1.8f;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern float WoopiBook_GetDevicePixelRatio();
#endif

        /// <summary>Screen pixels per browser CSS pixel. 1 outside a WebGL build.</summary>
        public static float DevicePixelRatio
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var ratio = WoopiBook_GetDevicePixelRatio();
                return ratio > 0f ? ratio : 1f;
#else
                return 1f;
#endif
            }
        }

        /// <summary>Width of one edge zone for a screen this wide, in screen pixels.</summary>
        public static float GetEdgeZoneWidth(float screenWidth)
        {
            var ratio = DevicePixelRatio;
            var cssWidth = screenWidth / ratio;
            var zone = Mathf.Max(EdgeZoneMinCssPx, cssWidth * EdgeZoneRatio) * ratio;
            return Mathf.Min(zone, screenWidth * 0.5f);
        }

        /// <summary>Height of the reader dock's hotzone along the bottom edge, in screen pixels.</summary>
        public static float GetDockHotzoneHeight() => DockHotzoneHeightCssPx * DevicePixelRatio;

        /// <summary>
        /// The screen area the host leaves to the book: between the edge zones (where a double tap turns the page) and above the
        /// dock's hotzone (where the pointer opens the reader menu).
        /// </summary>
        public static Rect GetSafeScreenRect()
        {
            var zone = GetEdgeZoneWidth(Screen.width);
            var dock = GetDockHotzoneHeight();
            return new Rect(zone, dock, Screen.width - 2f * zone, Screen.height - dock);
        }

        /// <summary>True if a double tap at this screen position would turn the page.</summary>
        public static bool IsInEdgeZone(Vector2 screenPoint)
        {
            var zone = GetEdgeZoneWidth(Screen.width);
            return screenPoint.x <= zone || screenPoint.x >= Screen.width - zone;
        }

        /// <summary>True if the pointer at this screen position is over the reader dock's hotzone (the bottom strip).</summary>
        public static bool IsInDockHotzone(Vector2 screenPoint) => screenPoint.y <= GetDockHotzoneHeight();

        /// <summary>True if this screen position is left to the book: outside both the edge zones and the dock hotzone.</summary>
        public static bool IsInSafeZone(Vector2 screenPoint) => !IsInEdgeZone(screenPoint) && !IsInDockHotzone(screenPoint);

        /// <summary>
        /// The safe area as a world rectangle on the z = 0 plane, for a camera that looks along +z (the usual 2D setup).
        /// Use it to place things, for example to keep a double-tap target inside it.
        /// </summary>
        public static Rect GetSafeWorldRect(Camera camera)
        {
            var depth = Mathf.Abs(camera.transform.position.z);
            var safe = GetSafeScreenRect();
            var min = camera.ScreenToWorldPoint(new Vector3(safe.xMin, safe.yMin, depth));
            var max = camera.ScreenToWorldPoint(new Vector3(safe.xMax, safe.yMax, depth));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>The shortest swipe, in screen pixels, that the template treats as a page turn.</summary>
        public static float GetSwipeMinDistance(float screenWidth)
        {
            var ratio = DevicePixelRatio;
            return Mathf.Max(SwipeMinDistanceCssPx, screenWidth / ratio * SwipeMinDistanceRatio) * ratio;
        }
    }
}
