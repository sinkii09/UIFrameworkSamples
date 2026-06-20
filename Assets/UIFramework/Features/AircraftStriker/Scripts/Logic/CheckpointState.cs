namespace AircraftStriker
{
    // Captured before each boss wave. Stored in memory only — never persisted to PlayerPrefs.
    public class CheckpointState
    {
        public int WaveIndex;
        public int LivesAtCheckpoint;
        public int ShieldAtCheckpoint;
        public int ScoreAtCheckpoint;
        public int CoinsEarned;

        public static CheckpointState From(int waveIndex, PlayerData data) => new CheckpointState
        {
            WaveIndex          = waveIndex,
            LivesAtCheckpoint  = data.Lives,
            ShieldAtCheckpoint = data.ShieldCount,
            ScoreAtCheckpoint  = data.Score,
            CoinsEarned        = data.CoinsEarned,
        };
    }
}
