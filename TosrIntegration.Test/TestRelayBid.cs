using Common;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tosr;
using Xunit;
using Xunit.Abstractions;

namespace TosrIntegration.Test
{
    using RelayBidKindFunc = Func<Auction, string, string, IEnumerable<Bid>, Suit, BidManager.RelayBidKind>;

    public class TestCaseProviderRelayBid
    {
        public static IEnumerable<object[]> TestCases3NT()
        {
            // ♣♦♥♠
            // 3(4)NT tests no pull
            yield return new object[] { "Test3NT18HCP", "AK32,AK2,A32,432", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3NT", "1♠2♣2♥2NT3♦Pass"};
            yield return new object[] { "Test3NT21HCP", "AK32,AK2,A32,K32", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥3NT", "1♠2♣2♥2NT3♦3♠Pass" };
            yield return new object[] { "Test3NT23HCP", "AK32,AK2,A32,KQ2", "x,xxxx,Kxxxx,Axx", "1♣1NT2♦2♠3♣3♥4♣4NT", "1♠2♣2♥2NT3♦3♠4♥Pass" };

            // 3(4)NT tests with pull
            yield return new object[] { "Test3NTPull18HCP", "AK32,AK2,A32,432", "x,Qxxx,KQxxx,AQx", "1♣1NT2♦2♠3♣3NT4♦4♠5♦5NT6♥Pass", "1♠2♣2♥2NT3♦4♣4♥5♣5♠6♦7♦" };
            yield return new object[] { "Test3NTPull21HCPMin", "AK32,AK2,A32,K32", "Q,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥3NT4NT5♥6♣6♠7♣Pass", "1♠2♣2♥2NT3♦3♠4♠5♦5NT6♥6NT7♥" };
            yield return new object[] { "Test3NTPull21HCPMax", "AQJ2,AQJ,A32,K32", "K,Kxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥4NT6♥7♣7♥Pass", "1♠2♣2♥2NT3♦4♣6♦6NT7♦7♠" };
            yield return new object[] { "Test3NTPull23HCP", "AK32,AK2,A32,KQ2", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥4♣4NT5♥6♣6♠7♦7♠Pass", "1♠2♣2♥2NT3♦3♠4♥5♦5NT6♥7♣7♥7NT" };
        }

        public static IEnumerable<object[]> TestCases4Diamond()
        {
            // ♣♦♥♠
            // Test with major fit no pull
            yield return new object[] { "TestFit17HCP", "AK32,A432,A32,Q2", "xx,xxxx,KQxxx,Kx", "1♣1NT2♦2♠4♦Pass", "1♠2♣2♥3♣4♥Pass" };
            yield return new object[] { "TestFit20HCPMin", "AQ32,A432,A2,AQ2", "xx,xxxx,KQxxx,Kx", "1♣1NT2♦2♠3♦4♦Pass", "1♠2♣2♥3♣3♥4♥Pass" };
            yield return new object[] { "TestFit18HCPMax", "Q432,Q432,AJ,AKQ", "Kx,AJxx,Kxxxx,Jx", "1♣1NT2♦2♠3♦4♦Pass", "1♠2♣2♥3♣3♠4♥Pass" };

            // Test with major fit with pull
            yield return new object[] { "TestFitPull17HCP", "AK32,A432,A32,Q2", "xx,Kxxx,KQxxx,AK", "1♣1NT2♦2♠4♦5♦6♣6♥7♣7♥Pass", "1♠2♣2♥3♣5♣5NT6♦6NT7♦7♠" };
            yield return new object[] { "TestFitPull18HCPMax", "QJ32,QJ32,AJ,AK2", "Kx,Axxx,KQxxx,xx", "1♣1NT2♦2♠3♦4♦5♥6♣6♥Pass", "1♠2♣2♥3♣3♠5♦5NT6♦6♠" };
            yield return new object[] { "TestFitPull20HCPMin", "AQ32,A432,A2,A32", "xx,Kxxx,KQxxx,Kx", "1♣1NT2♦2♠3♦4♦5♥6♦7♣7♠8♣Pass", "1♠2♣2♥3♣3♥5♦6♣6NT7♥7NT8♦" };

            // Test with minor fit. TODO
        }
    }

    [Collection("Sequential")]
    public class TestRelayBid
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries;
        private readonly ITestOutputHelper output;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public TestRelayBid(ITestOutputHelper output)
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            reverseDictionaries = new ReverseDictionaries(fasesWithOffset);
            this.output = output;
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases3NT), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions3NT(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest(testName);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, false);
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases4Diamond), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions4Diamond(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest(testName);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, (auction, northHand, southHandShape, controls, trumpSuit) => { return BidManager.RelayBidKind.fourDiamondEndSignal; });
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
        }

        private static void SetupTest(string testName)
        {
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            logger.Info($"Executing test-case {testName}");
            Pinvoke.Setup("Tosr.db3");
        }

        private static void AssertAuction(string expectedBidsNorth, string expectedBidsSouth, Auction auction)
        {
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            var actualBidsNorth = auction.GetBidsAsString(Player.North);

            Assert.Equal(expectedBidsSouth, actualBidsSouth);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);

            //var constructedSouthHand = bidManager.ConstructSouthHand(northHand, auction);
            //Assert.Equal(southHand, constructedSouthHand);
        }
    }
}
