using Sinkii09.UIFramework;

namespace AircraftStriker
{
    public class GameOverArgs : IViewArgs
    {
        public int FinalScore;
        public int WavesReached;
        public int CoinsEarned;
        public int GrazeCount;
        public int MaxCombo;
        public bool IsNewHighScore;
        public bool HasCheckpoint;
    }
}
