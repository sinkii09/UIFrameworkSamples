using System;
using ColorStackSort.Logic;
using NUnit.Framework;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Golden-value tests. A level's identity IS its seed — no board data is stored anywhere — so
    /// any change to this generator silently rewrites every level in the game. These assertions
    /// exist to make that change impossible to do by accident.
    /// </summary>
    [TestFixture]
    public class DeterministicRandomTests
    {
        [Test]
        public void NextUInt_Seed12345_MatchesTheRecordedSequence()
        {
            var random = new DeterministicRandom(12345);
            uint[] expected =
            {
                659017344, 4106910481, 1455411646, 4031707714,
                1126463182, 2585732372, 346233885, 490865175
            };

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], random.NextUInt(),
                    $"Draw {i} changed — every generated level in the game just changed with it.");
            }
        }

        [Test]
        public void NextUInt_SeedZero_MatchesTheRecordedSequence()
        {
            var random = new DeterministicRandom(0);
            uint[] expected = { 3469696627, 1581262666, 1615719374, 3412491734 };

            foreach (var value in expected) Assert.AreEqual(value, random.NextUInt());
        }

        [Test]
        public void NextUInt_NegativeSeed_MatchesTheRecordedSequence()
        {
            // Negative seeds must not sign-extend into the state; this pins that conversion.
            var random = new DeterministicRandom(-1);
            uint[] expected = { 549842577, 891870385, 3908540334, 2341628983 };

            foreach (var value in expected) Assert.AreEqual(value, random.NextUInt());
        }

        [Test]
        public void Next_StaysWithinBounds()
        {
            var random = new DeterministicRandom(7);

            for (var i = 0; i < 10000; i++)
            {
                var value = random.Next(5);
                Assert.GreaterOrEqual(value, 0);
                Assert.Less(value, 5);
            }
        }

        [Test]
        public void Next_IsUniformAcrossAnAwkwardBound()
        {
            // 7 does not divide 2^32, so a naive modulo would bias the low buckets. Rejection
            // sampling should keep every bucket within ~1% of the 100000 expectation.
            var random = new DeterministicRandom(99);
            var buckets = new int[7];

            for (var i = 0; i < 700000; i++) buckets[random.Next(7)]++;

            foreach (var count in buckets)
            {
                Assert.Greater(count, 98000, "Bucket badly under-represented — modulo bias?");
                Assert.Less(count, 102000, "Bucket badly over-represented — modulo bias?");
            }
        }

        [Test]
        public void Next_BoundOfOne_AlwaysReturnsZero()
        {
            var random = new DeterministicRandom(3);

            for (var i = 0; i < 100; i++) Assert.AreEqual(0, random.Next(1));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Next_NonPositiveBound_Throws(int bound)
        {
            var random = new DeterministicRandom(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(bound));
        }

        [Test]
        public void SameSeed_ProducesTheSameSequence()
        {
            var first = new DeterministicRandom(4242);
            var second = new DeterministicRandom(4242);

            for (var i = 0; i < 100; i++) Assert.AreEqual(first.NextUInt(), second.NextUInt());
        }
    }
}
