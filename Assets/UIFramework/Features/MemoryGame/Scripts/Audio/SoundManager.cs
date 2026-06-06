using UnityEngine;

namespace MemoryGame
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour, ISoundService
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        private bool _sfxEnabled;
        private bool _musicEnabled;
        // Tracks the last clip requested via PlayMusic so SetMusicEnabled(true) can resume it.
        private AudioClip _currentMusicClip;

        private void Awake()
        {
            _sfxEnabled = PlayerPrefs.GetInt("SfxEnabled", 1) == 1;
            _musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        }

        public void PlaySFX(AudioClip clip)
        {
            if (_sfxEnabled && clip != null)
                _sfxSource.PlayOneShot(clip);
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            _currentMusicClip = clip;
            if (!_musicEnabled) return;
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _currentMusicClip = null;
            _musicSource.Stop();
        }

        public void SetSFXEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            if (enabled)
            {
                if (_currentMusicClip != null)
                {
                    _musicSource.clip = _currentMusicClip;
                    _musicSource.loop = true;
                    _musicSource.Play();
                }
            }
            else
            {
                _musicSource.Pause();
            }
        }
    }
}
