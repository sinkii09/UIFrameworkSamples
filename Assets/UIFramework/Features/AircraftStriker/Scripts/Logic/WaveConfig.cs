using UnityEngine;

namespace AircraftStriker
{
    [CreateAssetMenu(menuName = "AircraftStriker/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        public SpawnEntry[] Enemies;
        public float PreWaveDelay = 1f;
        public bool IsBossWave;
        public EnemyConfig BossConfig;

        // Boss fires different pattern sets per HP phase (75% / 50% / 25% thresholds).
        // Executor handles empty/null arrays gracefully.
        public BulletPatternConfig[] BossPhase1Patterns;
        public BulletPatternConfig[] BossPhase2Patterns;
        public BulletPatternConfig[] BossPhase3Patterns;
    }
}
