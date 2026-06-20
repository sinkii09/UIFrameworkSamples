namespace AircraftStriker
{
    public static class GameScore
    {
        public static int ComboMultiplier(int combo) => System.Math.Min(combo, 10);
        public static int ScoreToCoins(int score) => score / 50;
    }
}
