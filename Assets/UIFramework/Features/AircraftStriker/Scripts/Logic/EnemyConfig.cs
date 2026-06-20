using UnityEngine;

namespace AircraftStriker
{
    [CreateAssetMenu(menuName = "AircraftStriker/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        public EnemyType Type;
        public int MaxHealth = 1;
        public float MoveSpeed = 2f;
        public int ScoreValue = 10;
        public float PickupDropChance = 0.15f;  // 0–1
        public BulletPatternConfig FirePattern; // null = does not fire
    }
}
