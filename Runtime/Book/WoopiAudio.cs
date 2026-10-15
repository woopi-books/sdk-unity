using System;
using UnityEngine;

namespace Woopi.Core.Book
{
    /// <summary>
    /// The book's master sound state, controlled by the host page (mute and volume). By default
    /// <see cref="WoopiBookHost"/> applies it to <see cref="AudioListener.volume"/>, which covers
    /// every AudioSource. A book that routes sound through its own system (an AudioMixer, an
    /// audio manager) turns that off on the host and reads <see cref="EffectiveVolume"/> /
    /// listens to <see cref="Changed"/> instead.
    /// </summary>
    public static class WoopiAudio
    {
        public static bool Muted { get; private set; }

        /// <summary>Master volume from 0 to 1, independent of the muted flag.</summary>
        public static float Volume { get; private set; } = 1f;

        /// <summary>What the player should actually hear: 0 while muted, else <see cref="Volume"/>.</summary>
        public static float EffectiveVolume => Muted ? 0f : Volume;

        /// <summary>Raised after mute or volume changed.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Muted = false;
            Volume = 1f;
            Changed = null;
        }

        /// <summary>Changes the state and raises <see cref="Changed"/> if anything differs.</summary>
        public static void Set(bool muted, float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (muted == Muted && Mathf.Approximately(volume, Volume))
                return;

            Muted = muted;
            Volume = volume;
            Changed?.Invoke();
        }

        public static void SetMuted(bool muted) => Set(muted, Volume);

        public static void SetVolume(float volume) => Set(Muted, volume);
    }
}
