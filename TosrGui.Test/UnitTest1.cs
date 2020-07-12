using System;
using Tosr;
using Xunit;

namespace TosrGui.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Assert.Equal(new Bid(1, Suit.Hearts), Common.GetBid(3));
            Assert.Equal(new Bid(1, Suit.NoTrump), Common.GetBid(5));
            Assert.Equal(new Bid(2, Suit.NoTrump), Common.GetBid(10));

            Assert.Equal(3, Common.GetBidId(new Bid(1, Suit.Hearts)));
            Assert.Equal(10, Common.GetBidId(new Bid(2, Suit.NoTrump)));

            Assert.Equal(new Bid(1, Suit.Spades), Common.NextBid(new Bid(1, Suit.Hearts)));
            Assert.Equal(new Bid(2, Suit.Clubs), Common.NextBid(new Bid(1, Suit.NoTrump)));

        }
    }
}
