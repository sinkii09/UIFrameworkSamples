using UnityEngine;

namespace MemoryGame
{
    public interface ISoundService
    {
        void PlaySFX(AudioClip clip);
        void PlayMusic(AudioClip clip, bool loop = true);
        void StopMusic();
        void SetSFXEnabled(bool enabled);
        void SetMusicEnabled(bool enabled);
    }
}
