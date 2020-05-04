using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tosr;

namespace TosrGui.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(Common.GetBid(3), new Bid(1, Suit.Hearts));
            Assert.AreEqual(Common.GetBid(5), new Bid(1, Suit.NoTrump));
            Assert.AreEqual(Common.GetBid(10), new Bid(2, Suit.NoTrump));

            Assert.AreEqual(Common.GetBidId(new Bid(1, Suit.Hearts)), 3);
            Assert.AreEqual(Common.GetBidId(new Bid(2, Suit.NoTrump)), 10);

            Assert.AreEqual(Common.NextBid(new Bid(1, Suit.Hearts)), new Bid(1, Suit.Spades));
            Assert.AreEqual(Common.NextBid(new Bid(1, Suit.NoTrump)), new Bid(2, Suit.Clubs));
        }
    }
}
