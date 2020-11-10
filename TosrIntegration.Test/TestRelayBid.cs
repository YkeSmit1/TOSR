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
        // TODO 4 tests has to be fixed
        public static IEnumerable<object[]> TestCases3NT()
        {
            // ♣♦♥♠
            // 3(4)NT tests no pull
            yield return new object[] { "TestNoAsk18HCP", "AK32,AK2,A32,432", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3NT", "1♠2♣2♥2NT3♦Pass"};
            yield return new object[] { "TestOneAsk21HCP", "AK32,AK2,A32,K32", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥3NT", "1♠2♣2♥2NT3♦3♠Pass" };
            yield return new object[] { "TestTwoAsk23HCP", "AK32,AK2,A32,KQ2", "x,xxxx,Kxxxx,Axx", "1♣1NT2♦2♠3♣3♥4♣4NT", "1♠2♣2♥2NT3♦3♠4♥Pass" };
        }

        public static IEnumerable<object[]> TestCases3NTPull()
        {
            // ♣♦♥♠
            // 3(4)NT tests with pull
            //yield return new object[] { "TestNoAskCtrl2", "AK32,AK2,A32,432", "x,QJxx,KQJxx,KQJ", "1♣1NT2♦2♠3♣3NT4♦5♣5♠6♦Pass", "1♠2♣2♥2NT3♦4♣4NT5♥6♣7♣" };
            yield return new object[] { "TestNoAskCtrl3", "AK32,AK2,A32,432", "x,Qxxx,KQxxx,AQx", "1♣1NT2♦2♠3♣3NT4♦4♠5♦5NT6♥Pass", "1♠2♣2♥2NT3♦4♣4♥5♣5♠6♦7♦" };
            //yield return new object[] { "TestNoAskCtrl3_", "AK32,AK2,A32,432", "Q,Qxxx,KQxxx,AQx", "1♣1NT2♦2♠3♣3NT4♦5♦5NT6♥Pass", "1♠2♣2♥2NT3♦4♣5♣5♠6♦7♥" };
            yield return new object[] { "TestNoAskCtrl5", "KJx,KJxxx,Kx,AJx", "AQ,Qx,Axxxx,KQxx", "1♣3NT4♠5♥6♥6NTPass", "3♣4♥5♦6♦6♠7♠" };
            yield return new object[] { "TestOneAsk21HCPMin", "AK32,AK2,A32,K32", "Q,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥3NT4♥5♣5♠6♦6♠Pass", "1♠2♣2♥2NT3♦3♠4♦4NT5♥6♣6♥7♣" };
            // TODO fix double zoom. Should ask for queens once after zoom twice.
            //yield return new object[] { "Test3NTPull21HCPMax_", "AQJ2,AQJ,A32,K32", "K,Kxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥4NT6♥6NT7♦Pass", "1♠2♣2♥2NT3♦4♣6♦6♠7♣7♥" };
            yield return new object[] { "TestOneAsk21HCPMax", "AQJ2,AQJ,AK2,432", "K,Kxxx,Qxxxx,AKx", "1♣1NT2♦2♠3♣3♥4NT5♦5NT6♠7♣Pass", "1♠2♣2♥2NT3♦4♣5♣5♠6♥6NT7♦" };
            yield return new object[] { "TestTwoAsk23HCP", "AK32,AK2,A32,KQ2", "x,xxxx,KQxxx,Axx", "1♣1NT2♦2♠3♣3♥4♣4NT5♥6♣6♠7♣Pass", "1♠2♣2♥2NT3♦3♠4♥5♦5NT6♥6NT7♦" };
        }

        public static IEnumerable<object[]> TestCases4Diamond()
        {
            // ♣♦♥♠
            // Test with major fit no pull
            yield return new object[] { "Test17HCP", "AK32,A432,A32,Q2", "xx,xxxx,KQxxx,Kx", "1♣1NT2♦2♠4♦Pass", "1♠2♣2♥3♣4♥Pass" };
            yield return new object[] { "Test20HCPMin", "AQ32,A432,A2,AQ2", "xx,xxxx,KQxxx,Kx", "1♣1NT2♦2♠3♦4♦Pass", "1♠2♣2♥3♣3♥4♥Pass" };
            yield return new object[] { "Test18HCPMax", "Q432,Q432,AJ,AKQ", "Kx,AJxx,Kxxxx,Jx", "1♣1NT2♦2♠3♦4♦Pass", "1♠2♣2♥3♣3♠4♥Pass" };
        }

        public static IEnumerable<object[]> TestCases4DiamondPull()
        {
            // ♣♦♥♠
            // Test with major fit with pull
            yield return new object[] { "Test17HCP", "AK32,A432,A32,Q2", "xx,Kxxx,KQxxx,AK", "1♣1NT2♦2♠4♦5♦6♣6♥7♣7♥Pass", "1♠2♣2♥3♣5♣5NT6♦6NT7♦7♠" };
            //yield return new object[] { "Test18HCPMax", "QJ32,KQJ2,AJ,A32", "Kx,Axxx,KQxxx,xx", "1♣1NT2♦2♠3♦4♦5♥6♣6♥Pass", "1♠2♣2♥3♣3♠5♦5NT6♦6♠" };
            yield return new object[] { "Test18HCPMin", "AQ32,A432,A2,A32", "xx,Kxxx,KQxxx,Kx", "1♣1NT2♦2♠3♦4♦5♣5NT6♠7♣Pass", "1♠2♣2♥3♣3♥4NT5♠6♥6NT7♦" };
            yield return new object[] { "Test20HCPMin", "AQ32,A432,K2,AK2", "xx,KQxx,AQxxx,xx", "1♣1NT2♦2♠3♦4♦5♣5NT6♦7♣Pass", "1♠2♣2♥3♣3♥4NT5♠6♣6NT7♦" };

            // Test with minor fit. TODO

            // Test with zoom
            yield return new object[] { "TestFitWithZoom", "KJ743,K,A84,AQT4", "Axxx,AQ,Qxx,Kxxx", "1♣1NT2♥4♦5♦5NT6♦Pass", "1♠2♦3NT5♣5♠6♣6NT" };
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
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases3NTPull), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions3NTPull(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest(testName);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, false);
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
            AssertHand(bidManager, auction, northHand, southHand);
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

        [Theory]
        [MemberData(nameof(TestCaseProviderRelayBid.TestCases4DiamondPull), MemberType = typeof(TestCaseProviderRelayBid))]
        public void TestAuctions4DiamondPull(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            SetupTest(testName);
            var bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, (auction, northHand, southHandShape, controls, trumpSuit) => { return BidManager.RelayBidKind.fourDiamondEndSignal; });
            var auction = bidManager.GetAuction(northHand, southHand);
            AssertAuction(expectedBidsNorth, expectedBidsSouth, auction);
            AssertHand(bidManager, auction, northHand, southHand);
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
        }

        private static void AssertHand(BidManager bidManager, Auction auction, string northHand, string southHand)
        {
            var constructedSouthHand = bidManager.ConstructSouthHand(northHand, auction);
            Assert.Equal(southHand, constructedSouthHand);
        }
    }
}
