using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Xunit;
using Common;
using BiddingLogic;
using Common.Test;
using Common.Tosr;

namespace TosrIntegration.Test
{
    public class TestCaseProviderQueens
    {
        public static IEnumerable<object[]> TestCases()
        {
            // ♣♦♥♠
            yield return ["5NT", "AKQ2,AK2,AKQ,KQ2", "xxxx,Qxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♣6♥7♣Pass"];
            yield return ["Zoom5NT", "AKQ2,AK2,KQ2,AKQ", "xxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♦6♠7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♣6♥7♣Pass"];
            yield return ["6C", "AK32,AKQ,AKQ,KQ2", "Qxxx,xxx,xx,Axxx", "1♣1NT2♥3♥4♣4♥4NT5♥5NT6♥7♠", "1♠2♦3♦3♠4♦4♠5♦5♠6♦6♠Pass"];
            yield return ["Zoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxx,xxx,AQ,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♥7♠", "1♠2♦3♦3♠4♦4♠5♣5♥6♦6♠Pass"];
            // With Singleton
            yield return ["1Singleton5NT", "AKQ2,AK2,AKQ,KQ2", "xxxx,Qxxx,x,Axxx", "1♣1♠2♣2♥2NT3♥4♣4♥4NT5♥7♠", "1♥1NT2♦2♠3♦3♠4♦4♠5♦5NTPass"];
            yield return ["1SingletonZoom5NT", "AKQ2,AK2,KQ2,AKQ", "xxxx,Qxx,A,xxxxx", "1♣1♠2♠3♥4♣4♥4NT5♦5♠6♣6♥7♠", "1♥2♥3♦3♠4♦4♠5♣5♥5NT6♦6NTPass"];
            yield return ["1Singleton6C", "AK32,AKQ,AKQ,KQ2", "Qxxx,xxx,x,Axxxx", "1♣1♠2♠3♥4♣4♥5♣5♥5NT7♠", "1♥2♥3♦3♠4♦4NT5♦5♠6♦Pass"];
            yield return ["1SingletonZoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxx,x,AQxx,xxxx", "1♣2♦2♠3♥4♣4♥5♣7♠", "2♣2♥3♦3♠4♦4NT5♠Pass"];

            // With two singletons
            yield return ["2Singleton5NT", "AKQ2,AK2,AKQ,KQ2", "x,Qxxxxx,x,Axxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5NTPass"];
            yield return ["2SingletonZoom5NT", "AKQ2,AK2,KQ2,AKQ", "x,Qxxxxx,A,xxxxx", "1♣2♣2♥2NT3♥4♣4♥4NT5♦5♠7♥", "1NT2♦2♠3♦3NT4♦4♠5♣5♥6♣Pass"];
            yield return ["2Singleton6C", "AK32,AKQ,AKQ,KQ2", "Qxxxxx,x,x,Axxxx", "1♣1♠2♥2NT3♥4♣4♥4NT5♦7♠", "1♥2♦2♠3♦3NT4♦4♠5♣5NTPass"];
            yield return ["2SingletonZoom6C", "AKJ2,AKQ,K32,AKQ", "Qxxxxx,x,AQxxx,x", "1♣1♠2♦2NT3♥4♣4♥4NT5♦7♠", "1♥2♣2♠3♦3NT4♦4♠5♣6♣Pass"];
        }
    }

    [Collection("Sequential")]
    public class QueensTest(BaseTestFixture fixture) : IClassFixture<BaseTestFixture>
    {
        private readonly Dictionary<Phase, bool> phasesWithOffset = fixture.phasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries = fixture.reverseDictionaries;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Theory]
        [MemberData(nameof(TestCaseProviderQueens.TestCases), MemberType = typeof(TestCaseProviderQueens))]
        public void TestAuctionsQueens(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            // This test can fail because it relies on the sampling and dds.
            ArgumentNullException.ThrowIfNull(testName);
            Logger.Info($"Executing testcase {testName}");

            _ = PInvoke.Setup("Tosr.db3");
            var bidManager = new BidManager(new BidGenerator(), phasesWithOffset, reverseDictionaries, true, false);
            var auction = bidManager.GetAuction(northHand, southHand);
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            Assert.Equal(expectedBidsSouth, actualBidsSouth);

            var actualBidsNorth = auction.GetBidsAsString(Player.North);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);

            var constructedSouthHand = bidManager.BiddingInformation.ConstructSouthHand(northHand);
            Assert.Equal(UtilTosr.HandWithX(southHand), constructedSouthHand.First());

            var queens = bidManager.BiddingInformation.GetQueensFromAuction(auction, bidManager.BiddingState);
            Assert.True(BiddingInformation.CheckQueens(queens, southHand));
        }
    }
}
