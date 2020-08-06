using Tosr;
using Xunit;

namespace TosrGui.Test
{
    public class SimpleTests
    {
        [Fact]
        public void BidManagerTest()
        {
            Assert.Equal(new Bid(1, Suit.Hearts), BidManager.GetBid(3));
            Assert.Equal(new Bid(1, Suit.NoTrump), BidManager.GetBid(5));
            Assert.Equal(new Bid(2, Suit.NoTrump), BidManager.GetBid(10));

            Assert.Equal(3, BidManager.GetBidId(new Bid(1, Suit.Hearts)));
            Assert.Equal(10, BidManager.GetBidId(new Bid(2, Suit.NoTrump)));

            Assert.Equal(new Bid(1, Suit.Spades), BidManager.NextBid(new Bid(1, Suit.Hearts)));
            Assert.Equal(new Bid(2, Suit.Clubs), BidManager.NextBid(new Bid(1, Suit.NoTrump)));

        }

        [Fact()]
        public void IsSameTeamTest()
        {
            Assert.True(Common.IsSameTeam(Player.North, Player.South));
            Assert.True(Common.IsSameTeam(Player.East, Player.West));
            Assert.True(Common.IsSameTeam(Player.UnKnown, Player.East));
            Assert.False(Common.IsSameTeam(Player.North, Player.West));
        }

    }
}
