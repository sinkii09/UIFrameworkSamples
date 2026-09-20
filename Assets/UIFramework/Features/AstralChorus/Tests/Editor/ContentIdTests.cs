using AstralChorus.Content;
using NUnit.Framework;

namespace AstralChorus.Tests.Content
{
    public sealed class ContentIdTests
    {
        [Test]
        public void IsValid_TwoSegments_True()
            => Assert.That(ContentId.IsValid("core:aria"), Is.True);

        // The architecture doc's own regex rejects this while its prose calls it valid. The looser
        // rule wins so milestone ids and content ids share one validator.
        [Test]
        public void IsValid_ThreeSegments_True()
            => Assert.That(ContentId.IsValid("ascent:floor:042"), Is.True);

        [Test]
        public void IsValid_Uppercase_False()
            => Assert.That(ContentId.IsValid("Core:Aria"), Is.False);

        [Test]
        public void IsValid_NoColon_False()
            => Assert.That(ContentId.IsValid("core"), Is.False);

        [Test]
        public void IsValid_LeadingColon_False()
            => Assert.That(ContentId.IsValid(":aria"), Is.False);

        [Test]
        public void IsValid_TrailingColon_False()
            => Assert.That(ContentId.IsValid("core:"), Is.False);

        [Test]
        public void IsValid_EmptyMiddleSegment_False()
            => Assert.That(ContentId.IsValid("core::aria"), Is.False);

        [Test]
        public void IsValid_IllegalCharacter_False()
            => Assert.That(ContentId.IsValid("core:aria!"), Is.False);

        [Test]
        public void IsValid_UnderscoreAndDash_True()
            => Assert.That(ContentId.IsValid("tide-banner:aria_01"), Is.True);

        // A char loop rather than a Regex specifically so this cannot throw.
        [Test]
        public void IsValid_Null_FalseWithoutThrowing()
            => Assert.That(ContentId.IsValid(null), Is.False);

        [Test]
        public void IsValid_Empty_False()
            => Assert.That(ContentId.IsValid(string.Empty), Is.False);

        [Test]
        public void TrySplit_ThreeSegments_SplitsAtFirstColon()
        {
            Assert.That(ContentId.TrySplit("ascent:floor:042", out string packId, out string entityId), Is.True);
            Assert.That(packId, Is.EqualTo("ascent"));
            Assert.That(entityId, Is.EqualTo("floor:042"));
        }

        [Test]
        public void TrySplit_Invalid_FalseAndNullOutputs()
        {
            Assert.That(ContentId.TrySplit("nope", out string packId, out string entityId), Is.False);
            Assert.That(packId, Is.Null);
            Assert.That(entityId, Is.Null);
        }
    }
}
