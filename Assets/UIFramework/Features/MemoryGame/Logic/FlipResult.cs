namespace MemoryGame
{
    public enum FlipResult
    {
        NeedSecond,     // first card flipped, waiting for second
        Match,          // pair matched
        Mismatch,       // pair didn't match — caller must await delay then ResolveMismatch()
        AlreadyFlipped, // card is face-up (already selected as first card)
        AlreadyMatched, // card already permanently matched
        Locked          // mismatch reveal window active — ignore click
    }
}
