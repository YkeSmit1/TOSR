using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using BiddingLogic;
using Common;
using NLog;
using System.Linq;
using Common.Test;
using Common.Tosr;
using JetBrains.Annotations;

namespace TosrIntegration.Test
{
    public class TestCaseProvider
    {
        public static IEnumerable<object[]> TestCases()
        {
            // ♣♦♥♠
            yield return ["FullTestCtlr2", "Axxx,Kxx,Kxx,xxx", "xxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♥4NT5♦5♠6♦6♠Pass", "1♠2♦3♦3♠4♦4♠5♣5♥6♣6♥7♣"];
            yield return ["FullTestCtlr3", "Axxx,Kxx,Kxx,xxx", "Kxxx,Qxx,Ax,xxxx", "1♣1NT2♥3♥4♣4♠5♦5♠6♦6♠Pass", "1♠2♦3♦3♠4♥5♣5♥6♣6♥7♣"];
            yield return ["FullTestCtrl4", "Axxx,Kxx,Kxx,xxx", "Kxxx,xxx,Ax,Kxxx", "1♣1NT2♥3♥4♣4NT5♠6♦6♠7♣Pass", "1♠2♦3♦3♠4♠5♥6♣6♥6NT7♦"];
            yield return ["FullTestCtrl5", "Kxxx,Kxx,Kxx,xxx", "Axxx,xxx,Ax,Kxxx", "1♣1NT2♥3♥4♦5♣5♠6♣6♥Pass", "1♠2♦3♦4♣4NT5♥5NT6♦6♠"];
            yield return ["FullTestZoomCtrl3", "Axxx,Kxx,Kxx,xxx", "Kxxx,Ax,Qxx,xxxx", "1♣1NT2♥3♠4♦4NT5♦5NT6♦Pass", "1♠2♦3♥4♣4♠5♣5♠6♣6♠"];
            yield return ["FullTestZoomCtrl4", "Kxxx,Kxx,Kxx,xxx", "Axxx,Ax,Qxx,xxxx", "1♣1NT2♥3♠4♥5♣5♥6♣6♥Pass", "1♠2♦3♥4♦4NT5♦5NT6♦6NT"];
            yield return ["FullTestZoomCtrl5", "Axxx,Kxx,Kxx,xxx", "Kxxx,Ax,AQx,xxxx", "1♣1NT2♥4♣4♠5♥5NTPass", "1♠2♦3NT4♥5♦5♠6♦"];
            yield return ["FullTestZoomCtrl6", "Kxxx,Kxx,Kxx,xxx", "Axxx,Ax,AQx,xxxx", "1♣1NT2♥4♦4NT5♠6♣Pass", "1♠2♦4♣4♠5♥5NT6♥"];
            yield return ["FullTest_6430Ctrl3", "Kxxx,Axx,Kxx,xxx", "AQxxxx,KQxx,Qxx,", "1♣1♠2♣4♣4♥5♣5NTPass", "1♥1NT3♠4♦4NT5♠6♠"];
            yield return ["FullTest_7330Ctrl3", "Kxxx,Axx,Kxx,xxx", "AQxxxxx,KQx,Qxx,", "1♣1♠3NT4♦4NT5♠Pass", "1♥3♠4♣4♠5♥6♥"];
            yield return ["FullTest_7231Ctrl3", "Kxxx,Axx,Kxx,Qxx", "AQxxxxx,KQ,Qxx,x", "1♣1♠4♣4♥5♣5♠Pass", "1♥3NT4♦4NT5♥6♠"];
            yield return ["FullTest_7321Ctrl3", "Kxxx,Axx,Kxx,Qxx", "AQxxxxx,KQx,Qx,x", "1♣1♠4♣4♥5♣5NTPass", "1♥3NT4♦4NT5♠6♠"];
            yield return ["FullTest_4441Ctrl4<12", "Kxxx,Axx,Axx,AKx", "Axxx,Kxxx,Kxxx,x", "1♣1♠2♣2♥3♦4♣5♣5♥Pass", "1♥1NT2♦3♣3NT4NT5♦5♠"];
            yield return ["FullTest_4441Ctrl4>12", "Kxxx,Axx,Axx,AKQ", "AQxx,KQxx,Kxxx,x", "1♣1♠2♣2♥3♥Pass", "1♥1NT2♦3♦4♠"];
            yield return ["FullTest_6511Ctrl3", "Kxxx,Axx,AQx,AKQ", "AQxxxx,Kxxxx,x,x", "1♣1♠2♣2NT3♥4♣4♥5♣Pass", "1♥1NT2♠3♦3NT4♦4NT5NT"];
            yield return ["FullTest_5611Ctrl5", "Kxxx,AQx,Axx,Axx", "AQxxx,Kxxxxx,K,K", "1♣1♠2♣2NT3♥4♣4NT5♠Pass", "1♥1NT2♠3♦3♠4♠5♥6♣"];
        }
    }

    [Collection("Sequential")]
    public class FullTest(BaseTestFixture fixture, ITestOutputHelper output) : IClassFixture<BaseTestFixture>
    {
        private readonly Dictionary<Phase, bool> phasesWithOffset = fixture.phasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries = fixture.reverseDictionaries;

        [UsedImplicitly] private readonly ITestOutputHelper output = output;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCases), MemberType = typeof(TestCaseProvider))]
        public void TestAuctions(string testName, string northHand, string southHand, string expectedBidsNorth, string expectedBidsSouth)
        {
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));
            Logger.Info($"Executing testcase {testName}");

            _ = PInvoke.Setup("Tosr.db3");
            var bidManager = new BidManager(new BidGenerator(), phasesWithOffset, reverseDictionaries, false);
            var auction = bidManager.GetAuction(string.Empty, southHand);
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            Assert.Equal(expectedBidsSouth, actualBidsSouth);

            var actualBidsNorth = auction.GetBidsAsString(Player.North);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);

            var constructedSouthHand = bidManager.biddingInformation.ConstructSouthHand(northHand);
            Assert.Equal(UtilTosr.HandWithX(southHand), constructedSouthHand.First());
            Assert.True(BiddingInformation.CheckQueens(bidManager.biddingInformation.GetQueensFromAuction(auction, bidManager.BiddingState), southHand));
        }
    }
}
