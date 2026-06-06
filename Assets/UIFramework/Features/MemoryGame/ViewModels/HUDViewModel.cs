using R3;
using Sinkii09.UIFramework;
using UnityEngine;

namespace MemoryGame
{
    public class HUDViewModel : ViewModelBase
    {
        public ReactiveProperty<int> Score { get; } = new(0);
        public ReactiveProperty<float> HealthPercent { get; } = new(1f);

        public void AddScore(int points)
        {
            Score.Value += points;
            Debug.Log($"[Sample] Score: {Score.Value}");
        }

        public void TakeDamage(float amount)
        {
            HealthPercent.Value = Mathf.Clamp01(HealthPercent.Value - amount);
            Debug.Log($"[Sample] Health: {HealthPercent.Value:P0}");
        }
    }
}
