using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFrameworkSamples
{
    public class HUDView : UIView<HUDViewModel>
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private Image _healthBar;

        // Test-only buttons — wire these in the sample prefab to drive the ViewModel directly.
        [SerializeField] private Button _addScoreButton;
        [SerializeField] private Button _damageButton;

        public override UILayer Layer => UILayer.HUD;

        protected override void BindViewModel(HUDViewModel vm)
        {
            vm.Score.BindToText(_scoreText, score => $"Score: {score}").AddTo(ref _showDisposables);
            vm.HealthPercent.BindToFillAmount(_healthBar).AddTo(ref _showDisposables);

            _addScoreButton.onClick.AddListener(() => vm.AddScore(10));
            _damageButton.onClick.AddListener(() => vm.TakeDamage(0.1f));
        }
    }
}
