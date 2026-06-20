namespace AircraftStriker
{
    public class PlayerData
    {
        public int MaxLives { get; private set; }
        public int Lives { get; private set; }
        public int ShieldCount { get; private set; }
        public WeaponLevel CurrentWeapon { get; private set; } // fixed at run start
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int CoinsEarned { get; private set; }
        public int GrazeCount { get; private set; }
        public float GrazeMeter { get; private set; }          // 0–100
        public bool IsAlive => Lives > 0;

        public const float GrazeMeterFillPerHit = 2f;
        public const float GrazeMeterFull = 100f;

        public void Reset(int maxLives, WeaponLevel startWeapon)
        {
            MaxLives = maxLives;
            Lives = maxLives;
            ShieldCount = 0;
            CurrentWeapon = startWeapon;
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            CoinsEarned = 0;
            GrazeCount = 0;
            GrazeMeter = 0f;
        }

        // Any hit costs exactly 1 life regardless of bullet damage — lives system.
        public void TakeDamage()
        {
            if (ShieldCount > 0) { ShieldCount--; Combo = 0; return; }
            Lives = System.Math.Max(0, Lives - 1);
            Combo = 0;
        }

        public void RestoreLife(int amount) =>
            Lives = System.Math.Min(MaxLives, Lives + amount);

        public void AddShield() => ShieldCount++;

        public void AddScore(int baseScore)
        {
            Combo++;
            if (Combo > MaxCombo) MaxCombo = Combo;
            int multiplier = System.Math.Min(Combo, 10);
            Score += baseScore * multiplier;
        }

        public void ResetCombo() => Combo = 0;

        public void AddGraze()
        {
            GrazeCount++;
            Score += 5;
            GrazeMeter = System.Math.Min(GrazeMeterFull, GrazeMeter + GrazeMeterFillPerHit);
        }

        // Returns true when the meter just filled; caller should trigger +500 bonus and reset.
        public bool TryConsumeFullGrazeMeter()
        {
            if (GrazeMeter < GrazeMeterFull) return false;
            GrazeMeter = 0f;
            return true;
        }

        public void AddCoins(int coins) => CoinsEarned += coins;

        public void RestoreFrom(CheckpointState s)
        {
            Lives       = s.LivesAtCheckpoint;
            ShieldCount = s.ShieldAtCheckpoint;
            Score       = s.ScoreAtCheckpoint;
            CoinsEarned = s.CoinsEarned;
            Combo       = 0;
            GrazeCount  = 0;
            GrazeMeter  = 0f;
        }
    }
}
