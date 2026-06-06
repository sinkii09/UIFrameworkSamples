namespace MemoryGame
{
    public struct CardData
    {
        public int Id;          // unique per card (0–15 for a 4×4 grid)
        public int PairId;      // 0–7; cards with equal PairId are a match
        public int FaceIndex;   // sprite index into GameplayView._cardFaceSprites (= PairId)
        public bool IsFlipped;
        public bool IsMatched;
    }
}
