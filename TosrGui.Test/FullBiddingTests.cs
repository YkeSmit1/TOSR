using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Moq;
using Xunit;
using BiddingLogic;
using Common;

namespace TosrGui.Test
{
    public class FullBiddingTests
    {
        [Fact()]
        public void ExecuteTest()
        {
            var bidGenerator = new Mock<IBidGenerator>();
            var phasesWithOffset = JsonSerializer.Deserialize<Dictionary<Phase, bool>>(File.ReadAllText("phasesWithOffset.json"));

            // 1Sp
            bidGenerator.SetupSequence(x => x.GetBid(It.IsAny<BiddingState>(), It.IsAny<string>())).
                // 1Sp
                Returns(() => (4, Phase.Shape, "", 0)).
                // 2Di
                Returns(() => (7, Phase.Shape, "", 0)).
                // 3NT
                Returns(() => (15, Phase.Controls, "", 0)).
                // 4NT
                Returns(() => (5, Phase.ScanningControls, "", 0)).
                // 5NT
                Returns(() => (5, Phase.ScanningControls, "", 0)).
                // Pass
                Returns(() => (0, Phase.ScanningControls, "", 0));

            var bidManager = new BidManager(bidGenerator.Object, phasesWithOffset);
            var auction = bidManager.GetAuction("", "");

            Assert.Equal("1♣1NT2♥4♣5♦6♥", auction.GetBidsAsString(Player.North));
            Assert.Equal("1♠2♦3NT5♣6♦Pass", auction.GetBidsAsString(Player.South));
            Assert.Equal("1♠2♦3NT", bidManager.BiddingState.GetBidsAsString(Phase.Shape));
            Assert.Equal("5♣", bidManager.BiddingState.GetBidsAsString(Phase.Controls));
            Assert.Equal("6♦", bidManager.BiddingState.GetBidsAsString(Phase.ScanningControls));
        }
    }
}