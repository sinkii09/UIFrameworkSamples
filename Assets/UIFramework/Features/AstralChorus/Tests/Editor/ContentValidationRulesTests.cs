using System.Collections.Generic;
using AstralChorus.Editor;
using NUnit.Framework;

namespace AstralChorus.Tests.Content
{
    // The rules are pure functions over gathered data, so they are tested without creating a single
    // asset on disk. That is the entire reason ContentValidationRules is separate from
    // ContentPackValidator.
    public sealed class ContentValidationRulesTests
    {
        private List<string> _findings;

        [SetUp]
        public void SetUp() => _findings = new List<string>();

        private static ManifestInfo Manifest(string packId, params string[] entryIds)
        {
            var info = new ManifestInfo
            {
                PackId = packId,
                FileName = packId,
                AssetPath = $"Assets/Game/Packs/{packId}/Resources/ContentPacks/{packId}.asset"
            };

            foreach (string id in entryIds)
            {
                info.Entries.Add(new EntryInfo { Id = id, AssetName = id });
            }

            return info;
        }

        private static List<ManifestInfo> Manifests(params ManifestInfo[] manifests)
            => new List<ManifestInfo>(manifests);

        // ---- rules 1 and 2 -----------------------------------------------------------------------

        [Test]
        public void CheckManifests_WellFormed_NoFindings()
        {
            ContentValidationRules.CheckManifests(
                Manifests(Manifest("core", "core:aria"), Manifest("tide", "tide:kaya")), _findings);

            Assert.That(_findings, Is.Empty);
        }

        [Test]
        public void CheckManifests_SameIdTwice_ReportsRule1()
        {
            ContentValidationRules.CheckManifests(
                Manifests(Manifest("core", "core:aria", "core:aria")), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("[rule 1]").And.Contain("core:aria"));
        }

        // Both manifests answer to the same Resources key, so which one the loader gets is decided
        // by import order.
        [Test]
        public void CheckManifests_TwoManifestsDeclaringOnePack_ReportsRule1()
        {
            var first = Manifest("core", "core:aria");
            var second = Manifest("core", "core:kaya");
            second.AssetPath = "Assets/Game/Packs/spare/Resources/ContentPacks/core.asset";

            ContentValidationRules.CheckManifests(Manifests(first, second), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("Two manifests declare pack 'core'"));
        }

        [Test]
        public void CheckManifests_MalformedId_ReportsRule2()
        {
            ContentValidationRules.CheckManifests(Manifests(Manifest("core", "Core:Aria")), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("[rule 2]").And.Contain("not a valid"));
        }

        [Test]
        public void CheckManifests_EntryClaimsAnotherPack_ReportsRule2()
        {
            ContentValidationRules.CheckManifests(Manifests(Manifest("core", "tide:kaya")), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("claims pack 'tide'"));
        }

        // The loader fetches by key, so a mismatch here means the pack is unreachable or answers
        // IsPackAvailable under a name nobody asks for.
        [Test]
        public void CheckManifests_PackIdDiffersFromFileName_ReportsRule2()
        {
            var manifest = Manifest("core", "core:aria");
            manifest.FileName = "main";

            ContentValidationRules.CheckManifests(Manifests(manifest), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("named 'main'"));
        }

        [Test]
        public void CheckManifests_ManifestOutsideResources_ReportsRule2()
        {
            var manifest = Manifest("core", "core:aria");
            manifest.AssetPath = "Assets/Game/Packs/core/core.asset";

            ContentValidationRules.CheckManifests(Manifests(manifest), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("never fetch it by key"));
        }

        // ---- rule 3 ------------------------------------------------------------------------------

        [Test]
        public void CheckProfile_WellFormed_NoFindings()
        {
            ContentValidationRules.CheckProfile(
                "core", new[] { "tide" }, Manifests(Manifest("core"), Manifest("tide")), _findings);

            Assert.That(_findings, Is.Empty);
        }

        [Test]
        public void CheckProfile_ListsTheAlwaysLoadedPack_ReportsRule3()
        {
            ContentValidationRules.CheckProfile(
                "core", new[] { "core" }, Manifests(Manifest("core")), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("must not be listed"));
        }

        [Test]
        public void CheckProfile_UnknownPack_ReportsRule3()
        {
            ContentValidationRules.CheckProfile(
                "core", new[] { "ghost" }, Manifests(Manifest("core")), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("has no manifest"));
        }

        [Test]
        public void CheckProfile_DuplicateEntry_ReportsRule3()
        {
            ContentValidationRules.CheckProfile(
                "core", new[] { "tide", "tide" }, Manifests(Manifest("core"), Manifest("tide")), _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("listed twice"));
        }

        // ---- rule 4 ------------------------------------------------------------------------------

        private static readonly List<string> Roots =
            new List<string> { "Assets/Game/Packs/core", "Assets/Game/Packs/tide" };

        private static List<KeyValuePair<string, string>> Edge(string from, string to)
            => new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(from, to) };

        [Test]
        public void CheckReferences_AssetOutsideAPackPointingIn_ReportsRule4()
        {
            ContentValidationRules.CheckReferences(
                Edge("Assets/Game/UI/MainMenu.prefab", "Assets/Game/Packs/tide/Art/kaya.png"),
                Roots, _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("[rule 4]"));
        }

        [Test]
        public void CheckReferences_WithinTheSamePack_NoFinding()
        {
            ContentValidationRules.CheckReferences(
                Edge("Assets/Game/Packs/tide/Definitions/kaya.asset", "Assets/Game/Packs/tide/Art/kaya.png"),
                Roots, _findings);

            Assert.That(_findings, Is.Empty);
        }

        // W-5: a single shared pack root made this edge invisible, and it is exactly the edge that
        // drags a disabled pack back into the build.
        [Test]
        public void CheckReferences_OnePackIntoAnother_ReportsRule4()
        {
            ContentValidationRules.CheckReferences(
                Edge("Assets/Game/Packs/core/Definitions/aria.asset", "Assets/Game/Packs/tide/Art/kaya.png"),
                Roots, _findings);

            Assert.That(_findings.Count, Is.EqualTo(1));
            Assert.That(_findings[0], Does.Contain("Assets/Game/Packs/tide"));
        }

        [Test]
        public void CheckReferences_AssetDependingOnItself_NoFinding()
        {
            string path = "Assets/Game/Packs/tide/Art/kaya.png";
            ContentValidationRules.CheckReferences(Edge(path, path), Roots, _findings);

            Assert.That(_findings, Is.Empty);
        }

        // ---- pack root derivation ----------------------------------------------------------------

        [Test]
        public void TryGetPackRoot_ManifestInsidePack_ReturnsPackFolder()
        {
            Assert.That(
                ContentValidationRules.TryGetPackRoot(
                    "Assets/Game/Packs/tide/Resources/ContentPacks/tide.asset", out string root),
                Is.True);
            Assert.That(root, Is.EqualTo("Assets/Game/Packs/tide"));
        }

        [Test]
        public void TryGetPackRoot_NoResourcesSegment_False()
        {
            Assert.That(
                ContentValidationRules.TryGetPackRoot("Assets/Game/Packs/tide/tide.asset", out string root),
                Is.False);
            Assert.That(root, Is.Null);
        }
    }
}
