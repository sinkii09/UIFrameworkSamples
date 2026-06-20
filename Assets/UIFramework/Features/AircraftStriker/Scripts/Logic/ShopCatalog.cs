using UnityEngine;

namespace AircraftStriker
{
    [CreateAssetMenu(menuName = "AircraftStriker/ShopCatalog")]
    public class ShopCatalog : ScriptableObject
    {
        public ShopItemConfig[] Items;
    }
}
