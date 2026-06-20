using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AircraftStriker
{
    public class ShopItemRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descLabel;
        [SerializeField] private TMP_Text _costLabel;
        [SerializeField] private Button _buyButton;
        [SerializeField] private GameObject _unlockedBadge;

        public void Bind(ShopItemData data, Action onBuy)
        {
            _nameLabel.text = data.DisplayName;
            _descLabel.text = data.Description;
            _costLabel.text = data.IsUnlocked ? "Owned" : $"{data.Cost} coins";
            _unlockedBadge.SetActive(data.IsUnlocked);
            _buyButton.gameObject.SetActive(!data.IsUnlocked);
            _buyButton.interactable = data.CanAfford;
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(() => onBuy());
        }
    }
}
