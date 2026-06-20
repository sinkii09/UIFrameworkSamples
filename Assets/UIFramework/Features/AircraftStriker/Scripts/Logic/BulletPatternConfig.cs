using UnityEngine;

namespace AircraftStriker
{
    [CreateAssetMenu(menuName = "AircraftStriker/BulletPatternConfig")]
    public class BulletPatternConfig : ScriptableObject
    {
        public BulletPatternType PatternType;
        public BulletConfig BulletConfig;   // which bullet type this pattern fires
        [Header("Spread")]
        public int Count = 8;               // bullets per burst
        public float StartAngle = 0f;       // offset from base direction (degrees)
        public float SpreadAngle = 360f;    // total arc (Ring=360, Fan=90, Wall=180)
        [Header("Timing")]
        public int BurstCount = 1;          // how many times to repeat the burst
        public float BurstDelay = 0.15f;    // seconds between bursts
        public float PatternInterval = 2f;  // seconds until pattern fires again
        [Header("Options")]
        public bool AimAtPlayer = false;
        public float SpiralStepDegrees = 15f; // only for Spiral / DualSpiral
    }
}
