using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;
using System.Collections.Generic;
using VContainer;

namespace AircraftStriker
{
    public class SkinItemData
    {
        public string Id;
        public string DisplayName;
        public bool IsUnlocked;
        public bool IsSelected;
    }

    public class SkinSelectionViewModel : ViewModelBase
    {
        private readonly IUINavigator _navigator;
        private readonly IProgressionService _progression;

        public ReactiveProperty<List<SkinItemData>> Skins { get; } = new(new List<SkinItemData>());
        public ReactiveProperty<string> SelectedSkinId { get; } = new(string.Empty);

        // Skin IDs — populated from designer data in Phase 6
        private static readonly string[] SkinIds = { "skin_default", "skin_red", "skin_blue" };
        private static readonly string[] SkinNames = { "Default", "Red Hawk", "Blue Storm" };

        [Inject]
        public SkinSelectionViewModel(IUINavigator navigator, IProgressionService progression)
        {
            _navigator = navigator;
            _progression = progression;
        }

        public override void OnShow() => RefreshSkins();

        public void OnSkinSelected(string skinId)
        {
            if (!_progression.IsUnlocked(skinId)) return;
            _progression.SaveSelectedSkin(skinId);
            SelectedSkinId.Value = skinId;
            RefreshSkins();
        }

        public void OnBackPressed() => _navigator.PopAsync().Forget();

        private void RefreshSkins()
        {
            SelectedSkinId.Value = _progression.LoadSelectedSkin();
            var list = new List<SkinItemData>();
            for (int i = 0; i < SkinIds.Length; i++)
            {
                list.Add(new SkinItemData
                {
                    Id = SkinIds[i],
                    DisplayName = SkinNames[i],
                    IsUnlocked = SkinIds[i] == "skin_default" || _progression.IsUnlocked(SkinIds[i]),
                    IsSelected = SkinIds[i] == SelectedSkinId.Value,
                });
            }
            Skins.Value = list;
        }
    }
}
