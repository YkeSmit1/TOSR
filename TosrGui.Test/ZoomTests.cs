using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;
using Moq;
using Tosr;
using Common;
using ShapeDictionary = System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<string> pattern, bool zoom)>;
using ControlsDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;

namespace TosrGui.Test
{
    public class ZoomTests
    {
        private ShapeDictionary shapeAuctions;
        private ControlsDictionary auctionsControls;
        private Dictionary<Fase, bool> fasesWithOffset;

        public ZoomTests()
        {
            // Setup
            shapeAuctions = new ShapeDictionary
            {
                {$"{new Bid(1, Suit.Spades)}{new Bid(3, Suit.Diamonds)}", (new List<string>{ "3424" }, false) },
                {$"{new Bid(1, Suit.Spades)}{new Bid(2, Suit.Diamonds)}{new Bid(3, Suit.Hearts)}", (new List<string>{ "4234" }, true) },
                {$"{new Bid(1, Suit.Spades)}{new Bid(3, Suit.Hearts)}", (new List<string>{ "4243" }, true) },
                {$"{new Bid(1, Suit.Hearts)}{new Bid(3, Suit.Hearts)}", (new List<string>{ "6331" }, false) }
            };

            auctionsControls = new ControlsDictionary
            {
                {"4♣4♠5♥5NT6♥", new List<string> { "Axxx,AQx,xxx,Kxx", "Axxx,KQx,xxx,Axx", "Kxxx,AQx,xxx,Axx"} }
            };

            fasesWithOffset = new Dictionary<Fase, bool>
            {
                { Fase.Shape, false },
                { Fase.Controls, false},
                { Fase.Scanning, true}
            };
        }

        [Fact()]
        public void GetShapeStrFromAuctionTest()
        {
            // Setup
            var auction = new Auction();
            var newBids = new List<Bid> { new Bid(1, Suit.Hearts), new Bid(3, Suit.Hearts) };
            newBids.ForEach(bid => bid.fase = Fase.Shape);
            auction.SetBids(Player.South, newBids);

            // Act and assert
            Assert.Equal("6331", BidManager.GetShapeStrFromAuction(auction, shapeAuctions).Item1.First());
        }

        [Fact()]
        public void GetShapeStrFromAuctionWithTest()
        {
            // Setup
            var auction = new Auction();
            var newBids = new List<Bid> { new Bid(1, Suit.Spades), new Bid(3, Suit.Spades) };
            newBids.ForEach(bid => bid.fase = Fase.Shape);
            auction.SetBids(Player.South, newBids);

            // Act and assert
            Assert.Equal("4243", BidManager.GetShapeStrFromAuction(auction, shapeAuctions).Item1.First());
        }

        [Fact()]
        public void GetShapeStrFromAuctionTestNotFound()
        {
            // Setup
            var auction = new Auction();
            var newBids = new List<Bid> { new Bid(1, Suit.Hearts), new Bid(3, Suit.Spades) };
            newBids.ForEach(bid => bid.fase = Fase.Shape);
            auction.SetBids(Player.South, newBids);

            // Act and assert
            Assert.Throws<InvalidOperationException>(() => BidManager.GetShapeStrFromAuction(auction, shapeAuctions));
        }

        [Fact()]
        public void FullTest()
        {
            // ♣♦♥♠

            // Simulate Kxxx,Ax,xxx,AQxx
            var bidGenerator = new Mock<IBidGenerator>();
            bidGenerator.SetupSequence(x => x.GetBid(It.IsAny<BiddingState>(), It.IsAny<string>())).
                // 1Sp
                Returns(() => (4, Fase.Shape, "", false)).
                // 2D
                Returns(() => (7, Fase.Shape, "", false)).
                // 3NT
                Returns(() => (15, Fase.Scanning, "", true)).
                // 4H
                Returns(() => (2, Fase.Scanning, "", false)).
                // 5D
                Returns(() => (5, Fase.Scanning, "", false)).
                // 5S
                Returns(() => (6, Fase.Scanning, "", false)).
                // 6D
                Returns(() => (8, Fase.Scanning, "", false));

            BidManager bidManager = new BidManager(bidGenerator.Object, fasesWithOffset, shapeAuctions, auctionsControls);
            var auction = bidManager.GetAuction("", "");

            Assert.Equal("1♠2♦3NT4♥5♦5♠6♦Pass", auction.GetBidsAsString(Player.South));
            Assert.Equal("1♣1NT2♥4♣4♠5♥5NT6♥", auction.GetBidsAsString(Player.North));

            Assert.Equal("1♠2♦3NT", auction.GetBidsAsString(Fase.Shape));
            Assert.Equal("", auction.GetBidsAsString(Fase.Controls));
            Assert.Equal("4♥5♦5♠6♦", auction.GetBidsAsString(Fase.Scanning));

            var southHand = bidManager.ConstructSouthHand("Axxx,Kxx,Kxx,Kxx", auction);
            Assert.Equal("Kxxx,Ax,xxx,AQxx", southHand);
        }
    }
}