using Xunit;
using Tosr;
using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

namespace TosrGui.Test
{
    public class FullBiddingTests
    {
        [Fact()]
        public void ExecuteTest()
        {
            var bidGenerator = new Mock<IBidGenerator>();
            // 1Sp
            bidGenerator.SetupSequence(x => x.GetBid(It.IsAny<BiddingState>(), It.IsAny<string>())).
               // 1Sp
                Returns(() => (4, Fase.Shape, "")).
                // 2Di
                Returns(() => (7, Fase.Shape, "")).
                // 3NT
                Returns(() => (15, Fase.Controls, "")).
                // 4NT
                Returns(() => (5, Fase.Scanning, "")).
                // 5NT
                Returns(() => (5, Fase.Scanning, "")).
                // Pass
                Returns(() => (0, Fase.Scanning, ""));

            var auction = new Auction();
            BidManager.GetAuction("", auction, bidGenerator.Object);

            Assert.Equal("1♣1NT2♥4♣5♦6♥", auction.GetBids(Player.North));
            Assert.Equal("1♠2♦3NT5♣6♦Pass", auction.GetBids(Player.South));
        }
    }
}