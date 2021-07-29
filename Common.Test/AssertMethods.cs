﻿using BiddingLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Common.Test
{
    public class AssertMethods
    {
        public static void AssertAuction(string expectedBidsNorth, string expectedBidsSouth, Auction auction)
        {
            auction.CheckConsistency();
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            var actualBidsNorth = auction.GetBidsAsString(Player.North);

            Assert.Equal(expectedBidsSouth, actualBidsSouth);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);
        }

        public static void AssertHand(BiddingInformation biddingInformation, Auction auction, string northHand, string southHand, ReverseDictionaries reverseDictionaries, BiddingState biddingState)
        {
            var constructedSouthHand = biddingInformation.ConstructSouthHand(northHand);
            Assert.Equal(Util.HandWithx(southHand), constructedSouthHand.First());

            var queens = biddingInformation.GetQueensFromAuction(auction, reverseDictionaries, biddingState);
            Assert.True(BiddingInformation.CheckQueens(queens, southHand));
        }
    }
}
