using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerShowcase
{
    /// <summary>
    /// Scene composition root: a tab bar over the showcase panels. Panels are initialised on first
    /// open, after activation has run their RecyclerViews' Awake.
    /// </summary>
    public class RecyclerShowcaseController : MonoBehaviour
    {
        private static readonly Color ActiveTab = new(0.33f, 0.55f, 0.95f);
        private static readonly Color IdleTab = new(0.22f, 0.24f, 0.32f);

        [SerializeField] private ShowcasePanel[] _panels;
        [SerializeField] private Button[] _tabs;

        private int _current = -1;

        private void Start()
        {
            if (_panels.Length != _tabs.Length)
            {
                Debug.LogError($"[RecyclerShowcase] {_panels.Length} panels but {_tabs.Length} tabs — rebuild the scene.", this);
                return;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                int tab = i;
                _tabs[i].onClick.AddListener(() => Select(tab));
                _tabs[i].GetComponentInChildren<TMP_Text>().text = _panels[i].Title;
            }

            Select(0);
        }

        public void Select(int tab)
        {
            if (tab == _current) return;
            _current = tab;

            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].gameObject.SetActive(i == tab);
                _tabs[i].image.color = i == tab ? ActiveTab : IdleTab;
            }

            // Activation just ran the panel's RecyclerView Awakes. Force layout so viewport rects are
            // real (CanvasScaler applied): SetCellProvider/SetItemCount lay cells out immediately and
            // size the first window from the viewport rect.
            Canvas.ForceUpdateCanvases();
            _panels[tab].Initialize();
        }
    }
}
