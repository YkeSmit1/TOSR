﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;
using Moq;
using BiddingLogic;
using Common;

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
        private readonly Dictionary<Fase, bool> fasesWithOffset;

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

            fasesWithOffset = new Dictionary<Fase, bool>
            {
                { Fase.Shape, false },
                { Fase.Controls, false},
                { Fase.ScanningControls, true},
                { Fase.ScanningOther, true}
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
            var newBids = new List<Bid> { new Bid(1, Suit.Hearts), new Bid(3, Suit.Hearts) };
            newBids.ForEach(bid => bid.fase = Fase.Shape);
            auction.SetBids(Player.South, newBids);

            // Act and assert
            Assert.Equal("6331", BiddingInformation.GetShapeStrFromAuction(auction, shapeAuctions).shapes.First());
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
            Assert.Equal("4243", BiddingInformation.GetShapeStrFromAuction(auction, shapeAuctions).shapes.First());
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
            Assert.Throws<InvalidOperationException>(() => BiddingInformation.GetShapeStrFromAuction(auction, shapeAuctions));
        }

        [Fact()]
        public void FullTest()
        {
            // ♣♦♥♠

            // Simulate Kxxx,Ax,xxx,AQxx
            var bidGenerator = new Mock<IBidGenerator>();
            bidGenerator.SetupSequence(x => x.GetBid(It.IsAny<BiddingState>(), It.IsAny<string>())).
                // 1Sp
                Returns(() => (4, Fase.Shape, "", 0)).
                // 2D
                Returns(() => (7, Fase.Shape, "", 0)).
                // 3NT
                Returns(() => (15, Fase.ScanningControls, "", 1)).
                // 4H
                Returns(() => (2, Fase.ScanningControls, "", 0)).
                // 5D
                Returns(() => (5, Fase.ScanningControls, "", 0)).
                // 5S
                Returns(() => (6, Fase.ScanningControls, "", 0)).
                // 6D
                Returns(() => (8, Fase.ScanningControls, "", 0));

            var reverseDictionaries = new ReverseDictionaries(shapeAuctions, auctionsControlsOnly, auctionsControlsScanning, null);
            var bidManager = new BidManager(bidGenerator.Object, fasesWithOffset, reverseDictionaries, false);
            var auction = bidManager.GetAuction("", "");

            Assert.Equal("1♠2♦3NT4♥5♦5♠6♦Pass", auction.GetBidsAsString(Player.South));
            Assert.Equal("1♣1NT2♥4♣4♠5♥5NT6♥", auction.GetBidsAsString(Player.North));

            Assert.Equal("1♠2♦3NT", auction.GetBidsAsString(Fase.Shape));
            Assert.Equal("", auction.GetBidsAsString(Fase.Controls));
            Assert.Equal("4♥5♦5♠6♦", auction.GetBidsAsString(Fase.ScanningControls));

            var southHand = bidManager.biddingInformation.ConstructSouthHand("Axxx,Kxx,Kxx,Kxx");
            Assert.Equal("Kxxx,Ax,xxx,Axxx", southHand.First());
        }
    }
}