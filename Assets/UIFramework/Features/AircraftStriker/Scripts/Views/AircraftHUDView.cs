using R3;
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

        // RollingNumberLabel owns _scoreLabel's text — never BindToText the label as well.
        [SerializeField] private RollingNumberLabel _scoreRoller;
        [SerializeField] private TMP_Text  _comboLabel;
        [SerializeField] private TMP_Text  _waveLabel;
        [SerializeField] private Image[]   _healthIcons;
        [SerializeField] private Image[]   _shieldIcons;
        [SerializeField] private Image     _grazeMeterFill;
        [SerializeField] private PunchFeedback _grazeMeterPunch;
        [SerializeField] private TMP_Text  _grazeCountLabel;
        [SerializeField] private Button    _pauseButton;
        [SerializeField] private GameObject _bossWarning;

        // Track previous meter value to detect a full-meter reset (100 → 0).
        private float _prevGrazeMeter;

        protected override void BindViewModel(AircraftHUDViewModel vm)
        {
            vm.Score.Subscribe(v => _scoreRoller.SetValue(v)).AddTo(ref _showDisposables);
            vm.Combo.BindToText(_comboLabel, v => v > 1 ? $"x{v}" : string.Empty).AddTo(ref _showDisposables);
            vm.WaveNumber.BindToText(_waveLabel, v => $"Wave {v}").AddTo(ref _showDisposables);
            vm.IsBossWave.BindToActive(_bossWarning).AddTo(ref _showDisposables);

            vm.GrazeMeter
                .Subscribe(v =>
                {
                    _grazeMeterFill.fillAmount = v / 100f;

                    // Punch on every graze increment; ~3x punch when the meter resets after full.
                    // PunchFeedback restores to its captured base scale — the old bare DOKill could
                    // leave the fill stuck mid-punch when a new graze landed during the previous one.
                    bool fullReset = _prevGrazeMeter >= 95f && v < 5f;
                    _grazeMeterPunch.PunchScale(fullReset ? 3f : 1f);

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
    }
}
