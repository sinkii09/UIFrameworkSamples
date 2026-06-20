using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AircraftStriker
{
    public class SkinItemRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private Image _previewImage;
        [SerializeField] private Button _selectButton;
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private GameObject _lockedOverlay;

        public void Bind(SkinItemData data, Action onSelect)
        {
            _nameLabel.text = data.DisplayName;
            _lockedOverlay.SetActive(!data.IsUnlocked);
            _selectedIndicator.SetActive(data.IsSelected);
            _selectButton.interactable = data.IsUnlocked && !data.IsSelected;
            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() => onSelect());
        }
    }
}
