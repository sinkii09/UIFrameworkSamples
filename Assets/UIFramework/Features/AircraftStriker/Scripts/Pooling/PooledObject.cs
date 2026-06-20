using System;
using UnityEngine;

namespace AircraftStriker
{
    public abstract class PooledObject : MonoBehaviour
    {
        public event Action<PooledObject> OnReturn;

        public void ReturnToPool() => OnReturn?.Invoke(this);

        public virtual void OnGetFromPool() { }
        public virtual void OnReturnToPool() { }
    }
}
