using R3;
using DG.Tweening;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AircraftStriker
{
    [UIViewKey("AircraftStriker/AircraftHUDView")]
    public class AircraftHUDView : UIView<AircraftHUDViewModel>
    {
        public override UILayer Layer => UILayer.HUD;

        [SerializeField] private TMP_Text  _scoreLabel;
        [SerializeField] private TMP_Text  _comboLabel;
        [SerializeField] private TMP_Text  _waveLabel;
        [SerializeField] private Image[]   _healthIcons;
        [SerializeField] private Image[]   _shieldIcons;
        [SerializeField] private Image     _grazeMeterFill;
        [SerializeField] private TMP_Text  _grazeCountLabel;
        [SerializeField] private Button    _pauseButton;
        [SerializeField] private GameObject _bossWarning;

        // Track previous meter value to detect a full-meter reset (100 → 0).
        private float _prevGrazeMeter;

        protected override void BindViewModel(AircraftHUDViewModel vm)
        {
            vm.Score.BindToText(_scoreLabel, v => $"{v:N0}").AddTo(ref _showDisposables);
            vm.Combo.BindToText(_comboLabel, v => v > 1 ? $"x{v}" : string.Empty).AddTo(ref _showDisposables);
            vm.WaveNumber.BindToText(_waveLabel, v => $"Wave {v}").AddTo(ref _showDisposables);
            vm.IsBossWave.BindToActive(_bossWarning).AddTo(ref _showDisposables);

            vm.GrazeMeter
                .Subscribe(v =>
                {
                    _grazeMeterFill.fillAmount = v / 100f;

                    // Punch scale on every graze increment; big punch when meter resets after full.
                    bool fullReset = _prevGrazeMeter >= 95f && v < 5f;
                    float punchSize = fullReset ? 0.35f : 0.12f;
                    _grazeMeterFill.transform.DOKill();
                    _grazeMeterFill.transform
                        .DOPunchScale(Vector3.one * punchSize, fullReset ? 0.35f : 0.18f, 1, 0.5f)
                        .SetLink(gameObject);

                    _prevGrazeMeter = v;
                })
                .AddTo(ref _showDisposables);

            vm.GrazeCount.BindToText(_grazeCountLabel, v => $"Graze: {v}").AddTo(ref _showDisposables);

            vm.Lives
                .Subscribe(h => UpdateHealthIcons(h, vm.MaxLives.Value))
                .AddTo(ref _showDisposables);
            vm.ShieldCount
                .Subscribe(UpdateShieldIcons)
                .AddTo(ref _showDisposables);

            _pauseButton.BindButton(vm.OnPausePressed, ref _showDisposables);
        }

        private void UpdateHealthIcons(int current, int max)
        {
            for (int i = 0; i < _healthIcons.Length; i++)
                _healthIcons[i].enabled = i < current;
        }

        private void UpdateShieldIcons(int count)
        {
            for (int i = 0; i < _shieldIcons.Length; i++)
                _shieldIcons[i].enabled = i < count;
        }

        private void OnDisable()
        {
            if (_grazeMeterFill != null) _grazeMeterFill.transform.DOKill();
        }
    }
}
