namespace AircraftStriker
{
    // Full implementation in Phase 6 (AircraftSoundManager).
    public interface IAircraftSoundService
    {
        void PlaySFX(SFXType sfx);
        void PlayMusic(bool loop = true);
        void StopMusic();
        void SetMasterVolume(float volume);
    }
}
