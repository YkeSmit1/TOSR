using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Xunit;
using Common;
using BiddingLogic;
using Common.Test;

namespace TosrIntegration.Test
{
    public class TestCaseProviderQueens
    {
        public static IEnumerable<object[]> TestCases()
        {
            // ♣♦♥♠
            yield return new object[] { "5NT", "AKQ2,AK2,AKQ,KQ2", "xxxx,Qxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♣6♥7♣Pass" };
            yield return new object[] { "Zoom5NT", "AKQ2,AK2,KQ2,AKQ", "xxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♣6♥7♣Pass" };
            yield return new object[] { "6C", "AK32,AKQ,AKQ,KQ2", "Qxxx,xxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♥7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♦6♠Pass" };
            yield return new object[] { "Zoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxx,xxx,AQ,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♥7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♦6♠Pass" };
            // With Singleton
            yield return new object[] { "1Singleton5NT", "AKQ2,AK2,AKQ,KQ2", "xxxx,Qxxx,x,Axxx", "1♣1♠2♣2♥2NT3♥4♣4♥4NT5♥7♠", "1♥1NT2♦2♠3♦3♠4♦4♠5♦5NTPass" };
            yield return new object[] { "1SingletonZoom5NT", "AKQ2,AK2,KQ2,AKQ", "xxxx,Qxx,A,xxxxx", "1♣1♠2♠3♥4♣4♥4NT5♦5♠6♣6♥7♠", "1♥2♥3♦3♠4♦4♠5♣5♥5NT6♦6NTPass" };
            yield return new object[] { "1Singleton6C", "AK32,AKQ,AKQ,KQ2", "Qxxx,xxx,x,Axxxx", "1♣1♠2♠3♥4♣4♥5♣5♥5NT7♠", "1♥2♥3♦3♠4♦4NT5♦5♠6♦Pass" };
            yield return new object[] { "1SingletonZoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxx,x,AQxx,xxxx", "1♣2♦2♠3♥4♣4♥5♣7♠", "2♣2♥3♦3♠4♦4NT5♠Pass" };

            // With two singletons
            yield return new object[] { "2Singleton5NT", "AKQ2,AK2,AKQ,KQ2", "x,Qxxxxx,x,Axxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5NTPass" };
            yield return new object[] { "2SingletonZoom5NT", "AKQ2,AK2,KQ2,AKQ", "x,Qxxxxx,A,xxxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦5♠7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5♥6♣Pass" };
            yield return new object[] { "2Singleton6C", "AK32,AKQ,AKQ,KQ2", "Qxxxxx,x,x,Axxxx", "1♣1♠2♥2NT3♥4♣4♥4NT5♦7♠", "1♥2♦2♠3♦3NT4♦4♠5♣5NTPass" };
            yield return new object[] { "2SingletonZoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxxxx,x,AQxxx,x", "1♣1♠2♦2NT3♥4♣4♥4NT5♦7♠", "1♥2♣2♠3♦3NT4♦4♠5♣6♣Pass" };
        }
    }

    [Collection("Sequential")]
    public class QueensTest : IClassFixture<BaseTestFixture>
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public QueensTest(BaseTestFixture fixture)
        {
            fasesWithOffset = fixture.fasesWithOffset;
            reverseDictionaries = fixture.reverseDictionaries;
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderQueens.TestCases), MemberType = typeof(TestCaseProviderQueens))]
        public void TestAuctionsQueens(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            // This test can fail because it relies on the sampling and dds.
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            logger.Info($"Executing testcase {testName}");

            _ = Pinvoke.Setup("Tosr.db3");
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset, reverseDictionaries, true, false) { SuitSelection = SuitSelection.LongestSuit};
            var auction = bidManager.GetAuction(northHand, southHand);
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            Assert.Equal(expectedBidsSouth, actualBidsSouth);

            var actualBidsNorth = auction.GetBidsAsString(Player.North);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);

            var constructedSouthHand = bidManager.biddingInformation.ConstructSouthHand(northHand);
            Assert.Equal(Util.HandWithx(southHand), constructedSouthHand.First());

            var queens = bidManager.biddingInformation.GetQueensFromAuction(auction, reverseDictionaries);
            Assert.True(BiddingInformation.CheckQueens(queens, southHand));
        }
    }
}
