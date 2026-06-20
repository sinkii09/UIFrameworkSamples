using UnityEngine;

namespace AircraftStriker
{
    [CreateAssetMenu(menuName = "AircraftStriker/ShopItem")]
    public class ShopItemConfig : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public int CoinCost;
        public ShopItemType ItemType;
        public Sprite PreviewSprite;
    }
}
