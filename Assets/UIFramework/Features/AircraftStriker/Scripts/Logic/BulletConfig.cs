using UnityEngine;

namespace AircraftStriker
{
    [CreateAssetMenu(menuName = "AircraftStriker/BulletConfig")]
    public class BulletConfig : ScriptableObject
    {
        public BulletType BulletType;
        [Header("Movement")]
        public float BaseSpeed = 5f;
        public float MaxSpeed = 15f;
        public float Acceleration = 0f;     // units/s² — positive speeds up, negative slows
        public float AngularVelocity = 0f;  // degrees/s — rotates velocity vector for curves
        public float Lifetime = 8f;
        [Header("Combat")]
        public int Damage = 1;
    }
}
