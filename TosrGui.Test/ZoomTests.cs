using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using Common;
using BiddingLogic;

namespace TosrGui.Test
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsOnlyDictionary = Dictionary<string, List<int>>;
    using ControlScanningDictionary = Dictionary<string, (List<string> controlsScanning, bool zoom)>;

    public class ZoomTests
    {
        private readonly ShapeDictionary shapeAuctions;
        private readonly ControlsOnlyDictionary auctionsControlsOnly;
        private readonly ControlScanningDictionary auctionsControlsScanning;
        private readonly Dictionary<Phase, bool> phasesWithOffset;

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

            auctionsControlsOnly = new ControlsOnlyDictionary
            {
                {"4♣", new List<int>{ 5 } }
            };

            phasesWithOffset = new Dictionary<Phase, bool>
            {
                { Phase.Shape, false },
                { Phase.Controls, false},
                { Phase.ScanningControls, true},
                { Phase.ScanningOther, true}
            };
            auctionsControlsScanning = new ControlScanningDictionary
            {
                {"4♣4♠5♥5NT6♥", (new List<string>{ "Kxxx,Ax,xxx,Axxx" }, false) }
            };
        }

        [Fact()]
        public void GetShapeStrFromAuctionTest()
        {
            // Setup
            var auction = new Auction();
            var newBids = new List<Bid> { new(1, Suit.Hearts), new(3, Suit.Hearts) };
            var biddingState = new BiddingState(phasesWithOffset);
            foreach (var bid in newBids)
                biddingState.BidsPerPhase.Add((Phase.Shape, bid));
            auction.SetBids(Player.South, newBids);

            // Act and assert
            Assert.Equal("6331", BiddingInformation.GetInformationFromBids(shapeAuctions, biddingState.GetBids(Phase.Shape)).information.First());
        }

        [Fact()]
        public void GetShapeStrFromAuctionWithTest()
        {
            // Setup
            var auction = new Auction();
            var newBids = new List<Bid> { new(1, Suit.Spades), new(3, Suit.Spades) };
            var biddingState = new BiddingState(phasesWithOffset);
            foreach (var bid in newBids)
                biddingState.BidsPerPhase.Add((Phase.Shape, bid));
            auction.SetBids(Player.South, newBids);

            // Act and assert
            Assert.Equal("4243", BiddingInformation.GetInformationFromBids(shapeAuctions, biddingState.GetBids(Phase.Shape)).information.First());
        }

        [Fact()]
        public void GetShapeStrFromAuctionTestNotFound()
        {
            // Setup
            var auction = new Auction();
            var newBids = new List<Bid> { new(1, Suit.Hearts), new(3, Suit.Spades) };
            var biddingState = new BiddingState(phasesWithOffset);
            foreach (var bid in newBids)
                biddingState.BidsPerPhase.Add((Phase.Shape, bid));
            auction.SetBids(Player.South, newBids);

            // Act and assert
            Assert.Throws<InvalidOperationException>(() => BiddingInformation.GetInformationFromBids(shapeAuctions, biddingState.GetBids(Phase.Shape)));
        }

        [Fact()]
        public void FullTest()
        {
            // ♣♦♥♠

            // Simulate Kxxx,Ax,xxx,AQxx
            var bidGenerator = new Mock<IBidGenerator>();
            bidGenerator.SetupSequence(x => x.GetBid(It.IsAny<BiddingState>(), It.IsAny<string>())).
                // 1Sp
                Returns(() => (4, Phase.Shape, "", 0)).
                // 2D
                Returns(() => (7, Phase.Shape, "", 0)).
                // 3NT
                Returns(() => (15, Phase.ScanningControls, "", 1)).
                // 4H
                Returns(() => (2, Phase.ScanningControls, "", 0)).
                // 5D
                Returns(() => (5, Phase.ScanningControls, "", 0)).
                // 5S
                Returns(() => (6, Phase.ScanningControls, "", 0)).
                // 6D
                Returns(() => (8, Phase.ScanningControls, "", 0));

            var reverseDictionaries = new ReverseDictionaries(shapeAuctions, auctionsControlsOnly, auctionsControlsScanning, null);
            var bidManager = new BidManager(bidGenerator.Object, phasesWithOffset, reverseDictionaries, false);
            var auction = bidManager.GetAuction("", "");

            Assert.Equal("1♠2♦3NT4♥5♦5♠6♦Pass", auction.GetBidsAsString(Player.South));
            Assert.Equal("1♣1NT2♥4♣4♠5♥5NT6♥", auction.GetBidsAsString(Player.North));

            Assert.Equal("1♠2♦3NT", bidManager.BiddingState.GetBidsAsString(Phase.Shape));
            Assert.Equal("", bidManager.BiddingState.GetBidsAsString(Phase.Controls));
            Assert.Equal("4♥5♦5♠6♦", bidManager.BiddingState.GetBidsAsString(Phase.ScanningControls));

            var southHand = bidManager.biddingInformation.ConstructSouthHand("Axxx,Kxx,Kxx,Kxx");
            Assert.Equal("Kxxx,Ax,xxx,Axxx", southHand.First());
        }
    }
}