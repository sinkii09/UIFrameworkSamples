using UnityEngine;

namespace AircraftStriker
{
    [CreateAssetMenu(menuName = "AircraftStriker/WaveDatabase")]
    public class WaveDatabase : ScriptableObject
    {
        public WaveConfig[] Waves;
        public bool LoopAfterFinal = true;
    }
}
