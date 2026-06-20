using UnityEngine;

namespace AircraftStriker
{
    // Registered via RegisterInstance<IAircraftSoundService> in AircraftLifetimeScope.
    // _sfxClips array index must match SFXType enum int values.
    // _musicSource AudioSource holds the background music clip — set loop + clip in Inspector.
    public class AircraftSoundManager : MonoBehaviour, IAircraftSoundService
    {
        [Header("SFX Clips (index = SFXType int value)")]
        [SerializeField] private AudioClip[]  _sfxClips;
        [SerializeField] private AudioSource[] _sfxPool;   // round-robin for polyphony

        [Header("Music")]
        [SerializeField] private AudioSource _musicSource;

        private int _sfxRoundRobin;

        public void PlaySFX(SFXType sfx)
        {
            int index = (int)sfx;
            if (index < 0 || index >= _sfxClips.Length || _sfxClips[index] == null) return;
            if (_sfxPool == null || _sfxPool.Length == 0) return;

            AudioSource src = _sfxPool[_sfxRoundRobin % _sfxPool.Length];
            _sfxRoundRobin++;
            src.PlayOneShot(_sfxClips[index]);
        }

        public void PlayMusic(bool loop = true)
        {
            if (_musicSource == null) return;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        public void SetMasterVolume(float volume) =>
            AudioListener.volume = Mathf.Clamp01(volume);
    }
}
