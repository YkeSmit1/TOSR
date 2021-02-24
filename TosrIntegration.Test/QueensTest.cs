using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using Xunit;
using Newtonsoft.Json;
using Common;
using Tosr;
using Common.Test;

namespace TosrIntegration.Test
{
    public class TestCaseProviderQueens
    {
        public static IEnumerable<object[]> TestCases()
        {
            // ♣♦♥♠
            yield return new object[] { "5NT", "AKQ32,AK2,AK2,K2", "xxxx,Qxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♣6♥7♣" };
            yield return new object[] { "Zoom5NT", "AKQ32,AK2,K32,AK", "xxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♣6♥7♣" };
            yield return new object[] { "6C", "AK432,AKQ,AK2,K2", "Qxxx,xxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♥7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♦6♠" };
            yield return new object[] { "Zoom6C", "AKJ2,AK2,K32,AKQ", "Qxxx,xxx,AQ,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♥7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♦6♠" };
            // With Singleton
            yield return new object[] { "1Singleton5NT", "AKQ2,AK2,AK32,K2", "xxxx,Qxxx,x,Axxx", "1♣1♠2♣2♥2NT3♥4♣4♥4NT5♥7♠", "1♥1NT2♦2♠3♦3♠4♦4♠5♦5NT" };
            yield return new object[] { "1SingletonZoom5NT", "AKQ2,AK2,KQ32,AK", "xxxx,Qxx,A,xxxxx", "1♣1♠2♠3♥4♣4♥4NT5♦5♠6♣6♥7♠", "1♥2♥3♦3♠4♦4♠5♣5♥5NT6♦6NT" };
            yield return new object[] { "1Singleton6C", "AK32,AKQ,AK32,K2", "Qxxx,xxx,x,Axxxx", "1♣1♠2♠3♥4♣4♥5♣5♥5NT7♠", "1♥2♥3♦3♠4♦4NT5♦5♠6♦" };
            yield return new object[] { "1SingletonZoom6C", "AKJ2,A32,K32,AKQ", "Qxxx,x,AQxx,xxxx", "1♣2♦2♠3♥4♣4♥5♣7♠", "2♣2♥3♦3♠4♦4NT5♠" };

            // With two singletons
            yield return new object[] { "2Singleton5NT", "AK32,AK32,AK2,K2", "x,Qxxxxx,x,Axxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5NT" };
            yield return new object[] { "2SingletonZoom5NT", "AK32,AK32,K32,AK", "x,Qxxxxx,A,xxxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦5♠7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5♥6♣" };
            yield return new object[] { "2Singleton6C", "AK32,AK2,AK32,K2", "Qxxxxx,x,x,Axxxx", "1♣1♠2♥2NT3♥4♣4♥4NT5♦7♠", "1♥2♦2♠3♦3NT4♦4♠5♣5NT" };
            yield return new object[] { "2SingletonZoom6C", "AKJ2,AK32,K3,A32", "Qxxxxx,x,AQxxx,x", "1♣1♠2♦2NT3♥4♣4♥4NT5♦7♠", "1♥2♣2♠3♦3NT4♦4♠5♣6♣" };
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

            Pinvoke.Setup("Tosr.db3");
            BidManager bidManager = new BidManager(new BidGenerator(), fasesWithOffset, reverseDictionaries, true);
            var auction = bidManager.GetAuction(northHand, southHand);
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            Assert.Equal(expectedBidsSouth, actualBidsSouth);

            var actualBidsNorth = auction.GetBidsAsString(Player.North);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);

            var constructedSouthHand = bidManager.ConstructSouthHand(northHand);
            Assert.Equal(Util.HandWithx(southHand), constructedSouthHand.First());

            var queens = bidManager.GetQueensFromAuction(auction, reverseDictionaries);
            Assert.True(BidManager.CheckQueens(queens, southHand));
        }
    }
}
