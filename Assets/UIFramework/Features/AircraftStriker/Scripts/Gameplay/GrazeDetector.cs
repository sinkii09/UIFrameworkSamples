using System.Collections.Generic;
using UnityEngine;

namespace AircraftStriker
{
    // Prevents the same bullet instance from counting as multiple grazes in one pass.
    public class GrazeDetector
    {
        private readonly HashSet<EntityId> _seen = new();

        /// Returns true the first time this instanceId is seen; false on repeat.
        public bool TryGraze(EntityId instanceId) => _seen.Add(instanceId);

        public void Clear() => _seen.Clear();
    }
}
