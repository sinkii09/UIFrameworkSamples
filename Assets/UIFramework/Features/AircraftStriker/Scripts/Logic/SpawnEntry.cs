using System;
using UnityEngine;

namespace AircraftStriker
{
    [Serializable]
    public struct SpawnEntry
    {
        public EnemyConfig EnemyConfig;
        public int Count;
        public float SpawnInterval;
        public FormationPatternType Formation;
        // Viewport space (0,0=bottom-left, 1,1=top-right).
        // WaveManager converts to world space via Camera.ViewportToWorldPoint.
        public Vector2 HoldPositionViewport;
    }
}
