namespace AircraftStriker
{
    // Full implementation in Phase 6 (PlayerPrefsProgressionService).
    public interface IProgressionService
    {
        int LoadHighScore();
        void SaveHighScore(int score);
        int LoadCoins();
        void SaveCoins(int coins);
        bool IsUnlocked(string itemId);
        void SetUnlocked(string itemId);
        string LoadSelectedSkin();
        void SaveSelectedSkin(string skinId);
        WeaponLevel LoadStartingWeapon();
        void SaveStartingWeapon(WeaponLevel level);
        int LoadBonusLives();
    }
}
