using TMPro;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>
    /// One tab of the showcase. Initialised lazily, the first time its tab is opened: activating the
    /// panel runs every RecyclerView's Awake synchronously, and only after that may the view be fed a
    /// provider or a count.
    /// </summary>
    public abstract class ShowcasePanel : MonoBehaviour
    {
        [SerializeField] private ShowcaseToolbar _toolbar;
        [SerializeField] private TMP_Text _hint;

        private bool _initialized;

        public abstract string Title { get; }

        /// <summary>What to look at in this tab — shown on screen so it can be checked without docs.</summary>
        protected abstract string Hint { get; }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _hint.text = Hint;
            OnInitialize(_toolbar);
        }

        protected abstract void OnInitialize(ShowcaseToolbar toolbar);
    }
}
