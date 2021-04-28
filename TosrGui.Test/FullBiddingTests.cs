using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Tosr;
using Common;

namespace TosrGui.Test
{
    public class FullBiddingTests
    {
        [Fact()]
        public void ExecuteTest()
        {
            var bidGenerator = new Mock<IBidGenerator>();
            var fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));

            // 1Sp
            bidGenerator.SetupSequence(x => x.GetBid(It.IsAny<BiddingState>(), It.IsAny<string>())).
                // 1Sp
                Returns(() => (4, Fase.Shape, "", 0)).
                // 2Di
                Returns(() => (7, Fase.Shape, "", 0)).
                // 3NT
                Returns(() => (15, Fase.Controls, "", 0)).
                // 4NT
                Returns(() => (5, Fase.ScanningControls, "", 0)).
                // 5NT
                Returns(() => (5, Fase.ScanningControls, "", 0)).
                // Pass
                Returns(() => (0, Fase.ScanningControls, "", 0));

            var auction = new BidManager(bidGenerator.Object, fasesWithOffset).GetAuction("", "");

            Assert.Equal("1♣1NT2♥4♣5♦6♥", auction.GetBidsAsString(Player.North));
            Assert.Equal("1♠2♦3NT5♣6♦Pass", auction.GetBidsAsString(Player.South));
            Assert.Equal("1♠2♦3NT", auction.GetBidsAsString(Fase.Shape));
            Assert.Equal("5♣", auction.GetBidsAsString(Fase.Controls));
            Assert.Equal("6♦", auction.GetBidsAsString(Fase.ScanningControls));
        }
    }
}