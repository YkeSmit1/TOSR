using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tosr;
using Xunit;

namespace Common.Test
{
    public class AssertMethods
    {
        public static void AssertAuction(string expectedBidsNorth, string expectedBidsSouth, Auction auction)
        {
            var actualBidsSouth = auction.GetBidsAsString(Player.South);
            var actualBidsNorth = auction.GetBidsAsString(Player.North);

            Assert.Equal(expectedBidsSouth, actualBidsSouth);
            Assert.Equal(expectedBidsNorth, actualBidsNorth);
        }

        public static void AssertHand(BiddingInformation bidManager, Auction auction, string northHand, string southHand, ReverseDictionaries reverseDictionaries)
        {
            var constructedSouthHand = bidManager.ConstructSouthHand(northHand);
            Assert.Equal(Util.HandWithx(southHand), constructedSouthHand.First());

            var queens = bidManager.GetQueensFromAuction(auction, reverseDictionaries);
            Assert.True(BiddingInformation.CheckQueens(queens, southHand));
        }
    }
}
