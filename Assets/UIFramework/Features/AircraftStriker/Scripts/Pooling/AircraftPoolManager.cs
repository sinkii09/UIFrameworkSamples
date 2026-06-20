using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace AircraftStriker
{
    // Registered in AircraftLifetimeScope via RegisterInstance.
    // _bulletPrefabs array must match BulletType enum integer order exactly:
    // [0]=PlayerBasic [1]=EnemyRed [2]=EnemyBlue [3]=EnemyOrb [4]=EnemyLaser [5]=BossGold
    // _bulletCapacities in same order: [30, 80, 80, 50, 20, 60]
    public class AircraftPoolManager : MonoBehaviour
    {
        [Header("Bullet Prefabs (index must match BulletType enum int value)")]
        [SerializeField] private BulletController[] _bulletPrefabs;
        [SerializeField] private int[] _bulletCapacities;

        [Header("Enemy Prefabs")]
        [SerializeField] private EnemyController _basicEnemyPrefab;
        [SerializeField] private EnemyController _speedEnemyPrefab;
        [SerializeField] private EnemyController _heavyEnemyPrefab;
        [SerializeField] private BossController _bossPrefab;

        [Header("Pickup Prefabs")]
        [SerializeField] private PickupController _healthPickupPrefab;
        [SerializeField] private PickupController _shieldPickupPrefab;
        [SerializeField] private PickupController _scoreBoostPickupPrefab;

        [Header("VFX Prefabs")]
        [SerializeField] private ParticleSystem _smallExplosionPrefab;
        [SerializeField] private ParticleSystem _largeExplosionPrefab;

        [Header("Capacities")]
        [SerializeField] private int _enemyCapacity = 20;
        [SerializeField] private int _pickupCapacity = 10;
        [SerializeField] private int _vfxCapacity = 20;

        private static readonly BulletType[] _bulletTypeValues = (BulletType[])Enum.GetValues(typeof(BulletType));
        private Dictionary<BulletType, ObjectPool<BulletController>> _bulletPools;
        private ObjectPool<EnemyController> _basicEnemyPool;
        private ObjectPool<EnemyController> _speedEnemyPool;
        private ObjectPool<EnemyController> _heavyEnemyPool;
        private ObjectPool<BossController> _bossPool;
        private ObjectPool<PickupController> _healthPool;
        private ObjectPool<PickupController> _shieldPool;
        private ObjectPool<PickupController> _scoreBoostPool;
        private ObjectPool<ParticleSystem> _smallVFXPool;
        private ObjectPool<ParticleSystem> _largeVFXPool;

        private void Awake()
        {
            try
            {
                InitPools();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e, this);
                Debug.LogError("[PoolManager] Awake() threw — some pools may be null. Check above exception.", this);
            }
        }

        private void InitPools()
        {
            // Bullet pools — guarded: if _bulletPrefabs is null Awake would otherwise crash before
            // enemy pools are created, leaving _basicEnemyPool / _speedEnemyPool / etc. null.
            _bulletPools = new Dictionary<BulletType, ObjectPool<BulletController>>();
            if (_bulletPrefabs != null)
            {
                var bulletTypes = _bulletTypeValues;
                for (int i = 0; i < bulletTypes.Length; i++)
                {
                    if (i >= _bulletPrefabs.Length || _bulletPrefabs[i] == null)
                    {
                        Debug.LogWarning($"[PoolManager] Missing prefab for BulletType {bulletTypes[i]} — pool skipped", this);
                        continue;
                    }
                    // Clamp to >=1: ObjectPool throws ArgumentException if maxSize < 1.
                    int cap = Mathf.Max(1, (_bulletCapacities != null && i < _bulletCapacities.Length) ? _bulletCapacities[i] : 50);
                    BulletType type = bulletTypes[i];
                    BulletController prefab = _bulletPrefabs[i];
                    ObjectPool<BulletController> bulletPool = null;
                    bulletPool = new ObjectPool<BulletController>(
                        createFunc: () =>
                        {
                            var obj = Instantiate(prefab, transform);
                            obj.OnReturn += p => { if (p.gameObject.activeSelf) bulletPool?.Release((BulletController)p); };
                            return obj;
                        },
                        actionOnGet:     obj => { obj.gameObject.SetActive(true);  obj.OnGetFromPool(); },
                        actionOnRelease: obj => { obj.gameObject.SetActive(false); obj.OnReturnToPool(); },
                        actionOnDestroy: obj => Destroy(obj.gameObject),
                        collectionCheck: false,
                        defaultCapacity: cap,
                        maxSize:         cap * 2
                    );
                    _bulletPools[type] = bulletPool;
                }
            }
            else Debug.LogError("[PoolManager] _bulletPrefabs not assigned — no bullet pools created.", this);

            // Enemy pools
            if (_basicEnemyPrefab   != null) _basicEnemyPool = CreatePool(_basicEnemyPrefab, _enemyCapacity);
            else Debug.LogError("[PoolManager] _basicEnemyPrefab not assigned.", this);

            if (_speedEnemyPrefab   != null) _speedEnemyPool = CreatePool(_speedEnemyPrefab, _enemyCapacity);
            else Debug.LogError("[PoolManager] _speedEnemyPrefab not assigned.", this);

            if (_heavyEnemyPrefab   != null) _heavyEnemyPool = CreatePool(_heavyEnemyPrefab, _enemyCapacity);
            else Debug.LogError("[PoolManager] _heavyEnemyPrefab not assigned.", this);

            if (_bossPrefab         != null) _bossPool       = CreatePool(_bossPrefab, 2);
            else Debug.LogError("[PoolManager] _bossPrefab not assigned.", this);

            // Pickup pools
            if (_healthPickupPrefab     != null) _healthPool     = CreatePool(_healthPickupPrefab, _pickupCapacity);
            else Debug.LogError("[PoolManager] _healthPickupPrefab not assigned.", this);

            if (_shieldPickupPrefab     != null) _shieldPool     = CreatePool(_shieldPickupPrefab, _pickupCapacity);
            else Debug.LogError("[PoolManager] _shieldPickupPrefab not assigned.", this);

            if (_scoreBoostPickupPrefab != null) _scoreBoostPool = CreatePool(_scoreBoostPickupPrefab, _pickupCapacity);
            else Debug.LogError("[PoolManager] _scoreBoostPickupPrefab not assigned.", this);

            // VFX pools
            if (_smallExplosionPrefab   != null) _smallVFXPool   = CreateVFXPool(_smallExplosionPrefab, _vfxCapacity);
            else Debug.LogError("[PoolManager] _smallExplosionPrefab not assigned.", this);

            if (_largeExplosionPrefab   != null) _largeVFXPool   = CreateVFXPool(_largeExplosionPrefab, _vfxCapacity / 2);
            else Debug.LogError("[PoolManager] _largeExplosionPrefab not assigned.", this);
        }

        // === Bullet API ===

        public BulletController GetBullet(BulletType type)
        {
            if (_bulletPools.TryGetValue(type, out var pool)) return pool.Get();
            Debug.LogError($"[PoolManager] No pool for BulletType {type}");
            return null;
        }

        public void ReleaseBullet(BulletController bullet, BulletType type)
        {
            if (_bulletPools.TryGetValue(type, out var pool)) pool.Release(bullet);
            else Debug.LogWarning($"[PoolManager] No pool for BulletType {type} — bullet leaked.", this);
        }

        // === Enemy API ===

        public EnemyController GetEnemy(EnemyType type)
        {
            var pool = type switch
            {
                EnemyType.Speed => _speedEnemyPool,
                EnemyType.Heavy => _heavyEnemyPool,
                _               => _basicEnemyPool
            };
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool for EnemyType.{type} is null — check Inspector prefab assignments.", this);
                return null;
            }
            return pool.Get();
        }

        public void ReleaseEnemy(EnemyController e, EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Speed: _speedEnemyPool?.Release(e); break;
                case EnemyType.Heavy: _heavyEnemyPool?.Release(e); break;
                default:              _basicEnemyPool?.Release(e); break;
            }
        }

        public BossController GetBoss()
        {
            if (_bossPool == null)
            {
                Debug.LogError("[PoolManager] _bossPool is null — check Inspector prefab assignments.", this);
                return null;
            }
            return _bossPool.Get();
        }
        public void ReleaseBoss(BossController b) => _bossPool?.Release(b);

        // === Pickup API ===

        public PickupController GetPickup(PickupType type)
        {
            var pool = type switch
            {
                PickupType.Shield     => _shieldPool,
                PickupType.ScoreBoost => _scoreBoostPool,
                _                     => _healthPool
            };
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool for PickupType.{type} is null — check Inspector prefab assignments.", this);
                return null;
            }
            return pool.Get();
        }

        public void ReleasePickup(PickupController p, PickupType type)
        {
            switch (type)
            {
                case PickupType.Shield:     _shieldPool?.Release(p);     break;
                case PickupType.ScoreBoost: _scoreBoostPool?.Release(p); break;
                default:                    _healthPool?.Release(p);      break;
            }
        }

        // === VFX API ===

        public ParticleSystem GetSmallVFX()
        {
            if (_smallVFXPool == null) { Debug.LogError("[PoolManager] _smallVFXPool is null — check Inspector prefab assignments.", this); return null; }
            return _smallVFXPool.Get();
        }
        public void ReleaseSmallVFX(ParticleSystem ps) => _smallVFXPool?.Release(ps);

        public ParticleSystem GetLargeVFX()
        {
            if (_largeVFXPool == null) { Debug.LogError("[PoolManager] _largeVFXPool is null — check Inspector prefab assignments.", this); return null; }
            return _largeVFXPool.Get();
        }
        public void ReleaseLargeVFX(ParticleSystem ps) => _largeVFXPool?.Release(ps);

        // === Bulk Release ===

        // Returns all currently-active pooled objects to their pools. Called by GameplayController.StopGame()
        // so the screen is clean when transitioning back to the main menu.
        // GetComponentsInChildren(false) snapshots active children; the pool's OnReturn handler guards
        // against double-release with the `if (p.gameObject.activeSelf)` check.
        public void ReturnAll()
        {
            var active = GetComponentsInChildren<PooledObject>(false);
            foreach (var obj in active)
                obj.ReturnToPool();

            var vfx = GetComponentsInChildren<ParticleSystem>(false);
            foreach (var ps in vfx)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        // === Factories ===

        private ObjectPool<T> CreatePool<T>(T prefab, int capacity) where T : PooledObject
        {
            ObjectPool<T> pool = null;
            pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    var obj = Instantiate(prefab, transform);
                    obj.OnReturn += p => { if (p.gameObject.activeSelf) pool?.Release((T)p); };
                    return obj;
                },
                actionOnGet:     obj => { obj.gameObject.SetActive(true);  obj.OnGetFromPool(); },
                actionOnRelease: obj => { obj.gameObject.SetActive(false); obj.OnReturnToPool(); },
                actionOnDestroy: obj => Destroy(obj.gameObject),
                collectionCheck: false,
                defaultCapacity: capacity,
                maxSize:         capacity * 2
            );
            return pool;
        }

        private ObjectPool<ParticleSystem> CreateVFXPool(ParticleSystem prefab, int capacity)
        {
            return new ObjectPool<ParticleSystem>(
                createFunc:      () => Instantiate(prefab, transform),
                actionOnGet:     ps => { ps.gameObject.SetActive(true); ps.Play(); },
                actionOnRelease: ps => { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); },
                actionOnDestroy: ps => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: capacity
            );
        }
    }
}
