using Tosr;
using Xunit;
using Common;

namespace TosrGui.Test
{
    public class SimpleTests
    {
        [Fact]
        public void BidManagerTest()
        {
            Assert.Equal(new Bid(1, Suit.Hearts), Bid.GetBid(3));
            Assert.Equal(new Bid(1, Suit.NoTrump), Bid.GetBid(5));
            Assert.Equal(new Bid(2, Suit.NoTrump), Bid.GetBid(10));

            Assert.Equal(3, Bid.GetBidId(new Bid(1, Suit.Hearts)));
            Assert.Equal(10, Bid.GetBidId(new Bid(2, Suit.NoTrump)));

            Assert.Equal(new Bid(1, Suit.Spades), Bid.NextBid(new Bid(1, Suit.Hearts)));
            Assert.Equal(new Bid(2, Suit.Clubs), Bid.NextBid(new Bid(1, Suit.NoTrump)));

        }

        [Fact()]
        public void IsSameTeamTest()
        {
            Assert.True(Common.Common.IsSameTeam(Player.North, Player.South));
            Assert.True(Common.Common.IsSameTeam(Player.East, Player.West));
            Assert.True(Common.Common.IsSameTeam(Player.UnKnown, Player.East));
            Assert.False(Common.Common.IsSameTeam(Player.North, Player.West));
        }

        [Fact()]
        public void MergeControlAndShapeTest()
        {
            Assert.Equal("Qxx,Kxxx,Axxxx,x", string.Join(',', BidManager.MergeControlAndShape("Axxx,Kxx,Qxx,xxx", "3451")));
            Assert.Equal("Axxxx,Kxxx,Qxx,x", string.Join(',', BidManager.MergeControlAndShape("Axxx,Kxx,Qxx,xxx", "5431")));
            Assert.Equal(",Axxx,Kxxxx,xxxx", string.Join(',', BidManager.MergeControlAndShape("Kxxx,Axx,xxx,xxx", "0454")));
        }

        [Fact()]
        public void SouthHandWithxTest()
        {
            Assert.Equal("Ax,KQxx,Qxxxx,xx", BatchBidding.HandWithx("A5,KQ65,QT987,42"));
        }
    }
}
