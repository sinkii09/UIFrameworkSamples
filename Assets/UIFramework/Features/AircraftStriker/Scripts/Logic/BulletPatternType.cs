namespace AircraftStriker
{
    public enum BulletPatternType
    {
        Ring,        // equal-angle spread in full 360°
        SpiralCW,    // rotating clockwise burst
        SpiralCCW,   // rotating counter-clockwise burst
        AimedFan,    // fan centered on direction toward player
        Wall,        // horizontal line of bullets
        BurstFan,    // rapid repeated fans with rotational offset
        DualSpiral,  // two spirals rotating in opposite directions
    }
}
