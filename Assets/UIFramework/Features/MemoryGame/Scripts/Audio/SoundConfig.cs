using UnityEngine;

namespace MemoryGame
{
    [CreateAssetMenu(menuName = "MemoryGame/SoundConfig", fileName = "SoundConfig")]
    public class SoundConfig : ScriptableObject
    {
        public AudioClip CardFlipClip;
        public AudioClip MatchClip;
        public AudioClip MismatchClip;
        public AudioClip WinClip;
        public AudioClip BackgroundMusicClip;
        public AudioClip MainMenuMusicClip;
        public AudioClip ButtonClickClip;
    }
}
