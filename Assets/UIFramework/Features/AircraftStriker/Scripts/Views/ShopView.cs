using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AircraftStriker
{
    [UIViewKey("AircraftStriker/ShopView")]
    public class ShopView : UIView<ShopViewModel>
    {
        public override UILayer Layer => UILayer.Screen;

        [SerializeField] private TMP_Text _coinBalanceLabel;
        [SerializeField] private Transform _itemListContainer;
        [SerializeField] private ShopItemRow _itemRowPrefab;
        [SerializeField] private Button _skinsButton;
        [SerializeField] private Button _backButton;

        protected override void BindViewModel(ShopViewModel vm)
        {
            vm.CoinBalance.BindToText(_coinBalanceLabel, v => $"{v} coins").AddTo(ref _showDisposables);

            vm.Items.Subscribe(items =>
            {
                foreach (Transform child in _itemListContainer)
                    Destroy(child.gameObject);

                if (items == null) return;
                foreach (var data in items)
                {
                    var row = Instantiate(_itemRowPrefab, _itemListContainer);
                    row.Bind(data, () => vm.OnBuyPressed(data.Id));
                }
            }).AddTo(ref _showDisposables);

            _skinsButton.BindButton(vm.OnSkinsPressed, ref _showDisposables);
            _backButton.BindButton(vm.OnBackPressed, ref _showDisposables);
        }
    }
}
