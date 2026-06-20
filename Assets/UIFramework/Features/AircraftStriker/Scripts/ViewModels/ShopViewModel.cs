using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using System.Collections.Generic;
using VContainer;

namespace AircraftStriker
{
    public class ShopItemData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int Cost;
        public bool IsUnlocked;
        public bool CanAfford;
    }

    public class ShopViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;
        private readonly IProgressionService _progression;
        private readonly ShopService _shopService;
        private readonly ShopCatalog _catalog;

        public ReactiveProperty<int> CoinBalance { get; } = new(0);
        public ReactiveProperty<List<ShopItemData>> Items { get; } = new(new List<ShopItemData>());

        [Inject]
        public ShopViewModel(
            IUINavigator navigator,
            IProgressionService progression,
            ShopService shopService,
            ShopCatalog catalog)
        {
            _navigator = navigator;
            _progression = progression;
            _shopService = shopService;
            _catalog = catalog;
        }

        public override void OnShow() => RefreshData();

        public void OnBuyPressed(string itemId)
        {
            if (_shopService.TryBuy(itemId, CoinBalance.Value))
                RefreshData();
        }

        public void OnSkinsPressed() => _navigator.ShowAsync<SkinSelectionView>().Forget();
        public void OnBackPressed() => _navigator.PopAsync().Forget();

        private void RefreshData()
        {
            CoinBalance.Value = _progression.LoadCoins();
            var items = new List<ShopItemData>();
            foreach (var cfg in _catalog.Items)
            {
                items.Add(new ShopItemData
                {
                    Id = cfg.Id,
                    DisplayName = cfg.DisplayName,
                    Description = cfg.Description,
                    Cost = cfg.CoinCost,
                    IsUnlocked = _progression.IsUnlocked(cfg.Id),
                    CanAfford = CoinBalance.Value >= cfg.CoinCost,
                });
            }
            Items.Value = items;
        }
    }
}
