namespace AircraftStriker
{
    public enum BulletPatternType
    {
        Ring,             // equal-angle spread in full 360°
        SpiralCW,         // rotating clockwise burst
        SpiralCCW,        // rotating counter-clockwise burst
        AimedFan,         // fan centered on direction toward player
        Wall,             // horizontal line of bullets
        BurstFan,         // rapid repeated fans with rotational offset
        DualSpiral,       // two spirals rotating in opposite directions
        TripleSpiral,     // three arms at 0°/120°/240°, rotating — dense field, hard to find gaps
        CrossSpiral,      // four arms at 0°/90°/180°/270°, rotating — bullet-hell coverage
        Pincer,           // two aimed sub-fans bracketing player left+right — forces gap-finding
        OscillatingWall,  // wall that sweeps left/right via sin wave — requires pattern-reading
    }
}
