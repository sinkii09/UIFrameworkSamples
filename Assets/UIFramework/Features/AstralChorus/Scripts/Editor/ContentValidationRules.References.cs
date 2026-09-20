using System;
using System.Collections.Generic;

namespace AstralChorus.Editor
{
    // Rule 4 lives in its own file because it is the only rule that reasons about asset PATHS rather
    // than about content, and because the class was over the 200-line limit with it inline.
    public static partial class ContentValidationRules
    {
        // The rule that fails silently in production if it is missing: an asset OUTSIDE a pack that
        // references an asset INSIDE it drags that pack into the build no matter what the profile
        // says. Cross-pack edges (A -> B) count too, which is why ownership is compared on both ends
        // rather than just asking "is the target inside any pack".
        public static void CheckReferences(
            IReadOnlyList<KeyValuePair<string, string>> edges,   // asset path -> dependency path
            IReadOnlyList<string> packRoots,
            List<string> findings)
        {
            foreach (var edge in edges)
            {
                if (edge.Key == edge.Value) continue;

                string target = OwnerOf(edge.Value, packRoots);
                if (target == null) continue;                    // dependency is not inside a pack

                string source = OwnerOf(edge.Key, packRoots);
                if (source == target) continue;                  // same pack: fine

                findings.Add($"[rule 4] '{edge.Key}' references '{edge.Value}', which belongs to pack " +
                             $"root '{target}'. That reference pulls the pack into the build even when " +
                             "the profile disables it.");
            }
        }

        private static string OwnerOf(string assetPath, IReadOnlyList<string> packRoots)
        {
            foreach (string root in packRoots)
            {
                if (assetPath.StartsWith(root + "/", StringComparison.Ordinal)) return root;
            }
            return null;
        }
    }
}
