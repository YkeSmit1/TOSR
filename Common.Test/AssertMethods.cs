using BiddingLogic;
using System.Linq;
using Common.Tosr;
using Xunit;

namespace Common.Test
{
    public static class AssertMethods
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
            Assert.Equal(UtilTosr.HandWithX(southHand), constructedSouthHand.First());

            var queens = biddingInformation.GetQueensFromAuction(auction, biddingState);
            Assert.True(BiddingInformation.CheckQueens(queens, southHand));
        }
    }
}
