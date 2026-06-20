using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace AircraftStriker
{
    [UIViewKey("AircraftStriker/SkinSelectionView")]
    public class SkinSelectionView : UIView<SkinSelectionViewModel>
    {
        public override UILayer Layer => UILayer.Screen;

        [SerializeField] private Transform _skinListContainer;
        [SerializeField] private SkinItemRow _skinRowPrefab;
        [SerializeField] private Button _backButton;

        protected override void BindViewModel(SkinSelectionViewModel vm)
        {
            vm.Skins.Subscribe(skins =>
            {
                foreach (Transform child in _skinListContainer)
                    Destroy(child.gameObject);

                if (skins == null) return;
                foreach (var data in skins)
                {
                    var row = Instantiate(_skinRowPrefab, _skinListContainer);
                    row.Bind(data, () => vm.OnSkinSelected(data.Id));
                }
            }).AddTo(ref _showDisposables);

            _backButton.BindButton(vm.OnBackPressed, ref _showDisposables);
        }
    }
}
