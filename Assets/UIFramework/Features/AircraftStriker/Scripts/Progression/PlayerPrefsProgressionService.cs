using UnityEngine;

namespace AircraftStriker
{
    public class PlayerPrefsProgressionService : IProgressionService
    {
        private const string KeyHighScore  = "as_hs";
        private const string KeyCoins      = "as_coins";
        private const string KeySkin       = "as_skin";
        private const string KeyWeapon     = "as_weapon";
        private const string PrefixUnlock  = "as_ul_";
        // "shop_bonus_hp" item ID must match ShopItemConfig.Id in ShopCatalog SO.
        private const string KeyBonusHp    = "shop_bonus_hp";

        public int  LoadHighScore()                => PlayerPrefs.GetInt(KeyHighScore, 0);
        public void SaveHighScore(int score)       => PlayerPrefs.SetInt(KeyHighScore, score);

        public int  LoadCoins()                    => PlayerPrefs.GetInt(KeyCoins, 0);
        public void SaveCoins(int coins)           => PlayerPrefs.SetInt(KeyCoins, coins);

        public bool IsUnlocked(string itemId)      => PlayerPrefs.GetInt(PrefixUnlock + itemId, 0) == 1;
        public void SetUnlocked(string itemId)     => PlayerPrefs.SetInt(PrefixUnlock + itemId, 1);

        public string LoadSelectedSkin()           => PlayerPrefs.GetString(KeySkin, "skin_default");
        public void   SaveSelectedSkin(string id)  => PlayerPrefs.SetString(KeySkin, id);

        public WeaponLevel LoadStartingWeapon()    => (WeaponLevel)PlayerPrefs.GetInt(KeyWeapon, (int)WeaponLevel.Single);
        public void SaveStartingWeapon(WeaponLevel l) => PlayerPrefs.SetInt(KeyWeapon, (int)l);

        public int LoadBonusLives() => IsUnlocked(KeyBonusHp) ? 1 : 0;
    }
}
