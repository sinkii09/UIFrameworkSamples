using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase
{
    /// <summary>
    /// A row of buttons a panel fills at runtime. Each panel declares its own actions in code, so the
    /// scene builder only has to create an empty row and point it at one inactive button template.
    /// </summary>
    public class ShowcaseToolbar : MonoBehaviour
    {
        [SerializeField] private Button _buttonTemplate;

        public void AddButton(string caption, Action onClick)
        {
            var button = Instantiate(_buttonTemplate, transform);
            button.gameObject.SetActive(true);
            button.name = caption;
            button.GetComponentInChildren<TMP_Text>().text = caption;
            button.onClick.AddListener(() => onClick());
        }
    }
}
