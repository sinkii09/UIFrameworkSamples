using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AircraftStriker
{
    public class BulletPatternExecutor
    {
        public async UniTaskVoid Fire(Vector3 origin, BulletPatternConfig config,
            Func<Vector3> getPlayerPos, AircraftPoolManager pool, GameplayController gameplay,
            CancellationToken ct = default)
        {
            for (int b = 0; b < config.BurstCount; b++)
            {
                if (ct.IsCancellationRequested) return;
                FireBurst(origin, config, getPlayerPos(), pool, gameplay);
                if (b < config.BurstCount - 1)
                {
                    bool cancelled = await UniTask
                        .Delay(TimeSpan.FromSeconds(config.BurstDelay), cancellationToken: ct)
                        .SuppressCancellationThrow();
                    if (cancelled) return;
                }
            }
        }

        public void FirePlayer(Vector3 origin, WeaponLevel weapon,
            AircraftPoolManager pool, GameplayController gameplay)
        {
            switch (weapon)
            {
                case WeaponLevel.Double:
                    Spawn(pool, BulletType.PlayerBasic, origin + Vector3.left  * 0.25f, Vector2.up, BulletOwner.Player, gameplay);
                    Spawn(pool, BulletType.PlayerBasic, origin + Vector3.right * 0.25f, Vector2.up, BulletOwner.Player, gameplay);
                    break;
                case WeaponLevel.Spread:
                    Spawn(pool, BulletType.PlayerBasic, origin,                        Vector2.up,           BulletOwner.Player, gameplay);
                    Spawn(pool, BulletType.PlayerBasic, origin, Rotate(Vector2.up, -20f), BulletOwner.Player, gameplay);
                    Spawn(pool, BulletType.PlayerBasic, origin, Rotate(Vector2.up,  20f), BulletOwner.Player, gameplay);
                    break;
                default: // Single
                    Spawn(pool, BulletType.PlayerBasic, origin, Vector2.up, BulletOwner.Player, gameplay);
                    break;
            }
        }

        private void FireBurst(Vector3 origin, BulletPatternConfig config, Vector3 playerPos,
            AircraftPoolManager pool, GameplayController gameplay)
        {
            switch (config.PatternType)
            {
                case BulletPatternType.Ring:
                    FireArc(origin, config, config.StartAngle, config.SpreadAngle, pool, gameplay);
                    break;
                case BulletPatternType.SpiralCW:
                    FireArc(origin, config, Time.time * config.SpiralStepDegrees, config.SpreadAngle, pool, gameplay);
                    break;
                case BulletPatternType.SpiralCCW:
                    FireArc(origin, config, -(Time.time * config.SpiralStepDegrees), config.SpreadAngle, pool, gameplay);
                    break;
                case BulletPatternType.AimedFan:
                case BulletPatternType.BurstFan:
                    FireFan(origin, config, playerPos, pool, gameplay);
                    break;
                case BulletPatternType.Wall:
                    FireWall(origin, config, pool, gameplay);
                    break;
                case BulletPatternType.DualSpiral:
                    float dual = Time.time * config.SpiralStepDegrees;
                    FireArc(origin, config, dual,         config.SpreadAngle, pool, gameplay);
                    FireArc(origin, config, dual + 180f,  config.SpreadAngle, pool, gameplay);
                    break;
            }
        }

        private void FireArc(Vector3 origin, BulletPatternConfig config,
            float baseAngle, float arc, AircraftPoolManager pool, GameplayController gameplay)
        {
            float step = config.Count > 1 ? arc / config.Count : 0f;
            for (int i = 0; i < config.Count; i++)
                Spawn(pool, config.BulletConfig.BulletType, origin,
                      Rotate(Vector2.up, baseAngle + step * i), BulletOwner.Enemy, gameplay);
        }

        private void FireFan(Vector3 origin, BulletPatternConfig config, Vector3 playerPos,
            AircraftPoolManager pool, GameplayController gameplay)
        {
            Vector2 toPlayer = ((Vector2)(playerPos - origin)).normalized;
            float   baseAngle  = Vector2.SignedAngle(Vector2.up, toPlayer) + config.StartAngle;
            float   halfArc    = config.SpreadAngle * 0.5f;
            float   step       = config.Count > 1 ? config.SpreadAngle / (config.Count - 1) : 0f;
            for (int i = 0; i < config.Count; i++)
                Spawn(pool, config.BulletConfig.BulletType, origin,
                      Rotate(Vector2.up, baseAngle - halfArc + step * i), BulletOwner.Enemy, gameplay);
        }

        private void FireWall(Vector3 origin, BulletPatternConfig config,
            AircraftPoolManager pool, GameplayController gameplay)
        {
            float step  = config.Count > 1 ? config.SpreadAngle / (config.Count - 1) : 0f;
            float start = config.StartAngle - config.SpreadAngle * 0.5f;
            for (int i = 0; i < config.Count; i++)
                Spawn(pool, config.BulletConfig.BulletType, origin,
                      Rotate(Vector2.down, start + step * i), BulletOwner.Enemy, gameplay);
        }

        private static void Spawn(AircraftPoolManager pool, BulletType type, Vector3 origin,
            Vector2 dir, BulletOwner owner, GameplayController gameplay)
        {
            var bullet = pool.GetBullet(type);
            if (bullet == null) return;
            bullet.transform.position = origin;
            bullet.Setup(owner, dir, gameplay);
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }
    }
}
