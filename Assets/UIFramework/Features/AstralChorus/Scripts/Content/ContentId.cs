namespace AstralChorus.Content
{
    // A content id is "packId:entityId" -- the key a player's save file stores, so its rules are
    // load-bearing: getting one wrong reassigns somebody's progress to a different entity.
    //
    // The entity half MAY contain further colons. The content architecture doc states the rule as
    // the regex ^[a-z0-9_-]+:[a-z0-9_-]+$ and then, a few sections later, calls "ascent:floor:042"
    // valid (pack "ascent", entity "floor:042"). Both cannot be true. The looser rule is the one
    // kept, because milestone ids and content ids have to share a single validator -- at the price
    // that a typo'd "a:b:c" where "a:b" was meant is NOT caught here.
    //
    // Written as a character loop rather than a Regex on purpose: no allocation, no IL2CPP caveat,
    // and IsValid(null) answers false instead of throwing.
    public static class ContentId
    {
        public static bool IsValid(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            int segments = 1;
            int charsInSegment = 0;

            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];

                if (c == ':')
                {
                    if (charsInSegment == 0) return false;   // ":a" or "a::b"
                    segments++;
                    charsInSegment = 0;
                    continue;
                }

                if (!IsIdChar(c)) return false;
                charsInSegment++;
            }

            // charsInSegment == 0 here means a trailing colon ("a:").
            return segments >= 2 && charsInSegment > 0;
        }

        // Splits at the FIRST colon: everything after it belongs to the pack and may contain more.
        public static bool TrySplit(string id, out string packId, out string entityId)
        {
            packId = null;
            entityId = null;

            if (!IsValid(id)) return false;

            int colon = id.IndexOf(':');
            packId = id.Substring(0, colon);
            entityId = id.Substring(colon + 1);
            return true;
        }

        private static bool IsIdChar(char c)
            => (c >= 'a' && c <= 'z')
            || (c >= '0' && c <= '9')
            || c == '_'
            || c == '-';
    }
}
