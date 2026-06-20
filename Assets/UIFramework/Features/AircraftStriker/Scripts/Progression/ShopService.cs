using VContainer;

namespace AircraftStriker
{
    // Full implementation in Phase 6.
    public class ShopService
    {
        private readonly IProgressionService _progression;
        private readonly ShopCatalog _catalog;

        [Inject]
        public ShopService(IProgressionService progression, ShopCatalog catalog)
        {
            _progression = progression;
            _catalog = catalog;
        }

        // Returns true if purchase succeeded.
        public bool TryBuy(string itemId, int currentCoins)
        {
            foreach (var item in _catalog.Items)
            {
                if (item.Id != itemId) continue;
                if (_progression.IsUnlocked(itemId)) return false;
                if (currentCoins < item.CoinCost) return false;

                _progression.SaveCoins(currentCoins - item.CoinCost);
                _progression.SetUnlocked(itemId);
                return true;
            }
            return false;
        }
    }
}
