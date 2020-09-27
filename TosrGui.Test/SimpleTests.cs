using Tosr;
using Xunit;
using Common;
using System.Collections.Generic;

namespace TosrGui.Test
{
    public class TestCaseProviderMergeTest
    {
        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { "Qxx,Kxxx,Axxxx,x", new[] { "A", "K", "Q", "" }, "3451" };
            yield return new object[] { "Axxxx,Kxxx,Qxx,x", new[] { "A", "K", "Q", "" }, "5431" };
            yield return new object[] { ",Axxx,Kxxxx,xxxx", new[] { "K", "A", "", "" }, "0454" };
            yield return new object[] { "Kxxx,xxx,Qx,Axxx", new[] { "K", "A", "", "Q" }, "4324" };
            yield return new object[] { "Kxxx,Axx,Qx,xxxx", new[] { "K", "", "A", "Q" }, "4324" };
            yield return new object[] { "Kxxx,xxx,Axx,Qxx", new[] { "K", "", "A", "Q" }, "4333" };
            yield return new object[] { "xxx,Kxxx,Axx,Qxx", new[] { "K", "", "A", "Q" }, "3433" };
            yield return new object[] { "xxx,Axx,Kxxx,Qxx", new[] { "K", "", "A", "Q" }, "3343" };
            yield return new object[] { "xxx,Axx,Qxx,Kxxx", new[] { "K", "", "A", "Q" }, "3334" };
        }
    }

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
            Assert.True(Util.IsSameTeam(Player.North, Player.South));
            Assert.True(Util.IsSameTeam(Player.East, Player.West));
            Assert.True(Util.IsSameTeam(Player.UnKnown, Player.East));
            Assert.False(Util.IsSameTeam(Player.North, Player.West));
        }

        [Theory]
        [MemberData(nameof(TestCaseProviderMergeTest.TestCases), MemberType = typeof(TestCaseProviderMergeTest))]
        public void MergeControlAndShapeTest(string expected, string[] controls, string shapeLengthStr)
        {
            Assert.Equal(expected, string.Join(',', BidManager.MergeControlAndShape(controls, shapeLengthStr)));
        }

        [Fact()]
        public void SouthHandWithxTest()
        {
            Assert.Equal("Ax,KQxx,Qxxxx,xx", BidManager.HandWithx("A5,KQ65,QT987,42"));
        }
    }
}
