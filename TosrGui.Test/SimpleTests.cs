using BiddingLogic;
using Xunit;
using Common;
using System.Collections.Generic;
using Common.Tosr;

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
            Assert.Equal(expected, string.Join(',', BiddingInformation.MergeControlAndShape(controls, shapeLengthStr)));
        }

        [Fact()]
        public void SouthHandWithxTest()
        {
            Assert.Equal("Ax,Kxxx,xxxxx,xx", UtilTosr.HandWithX("A5,KQ65,QT987,42"));
        }

        [Fact()]
        public void GetTrumpSuitTest()
        {
            Assert.Equal(Suit.Hearts, Util.GetTrumpSuit("xxx,xxxx,xxx,xxx", "xxx,xxxx,xxx,xxx"));
            Assert.Equal(Suit.Spades, Util.GetTrumpSuit("xxxxx,xx,xxx,xxx", "xxxx,xxx,xxx,xxx"));
            Assert.Equal(Suit.NoTrump, Util.GetTrumpSuit("xxx,xxxx,xxx,xxx", "xxxx,xxx,xxx,xxx"));
            Assert.Equal(Suit.NoTrump, Util.GetTrumpSuit("xxx,xxx,xxxx,xxx", "xxx,xxx,xxxx,xxx"));
            Assert.Equal(Suit.NoTrump, Util.GetTrumpSuit("xxx,xxx,xxxxx,xx", "xxx,xxx,xxxx,xxx"));
            Assert.Equal(Suit.NoTrump, Util.GetTrumpSuit("xxx,xxx,xxxx,xxx", "xxxx,xxxx,xxxx,x"));
            Assert.Equal(Suit.Diamonds, Util.GetTrumpSuit("xxx,xxx,xxxxx,xx", "xxxx,xxxx,xxxx,x"));
        }

        [Fact()]
        public void GetGameContractTest()
        {
            Assert.Equal(new Bid(4, Suit.Hearts), Bid.GetGameContract(Suit.Hearts, new Bid(3, Suit.Hearts), false));
            Assert.Equal(Bid.PassBid, Bid.GetGameContract(Suit.Hearts, new Bid(4, Suit.Hearts), false));
            Assert.Equal(Bid.PassBid, Bid.GetGameContract(Suit.Hearts, new Bid(5, Suit.Hearts), false));
            Assert.Equal(new Bid(4, Suit.Hearts), Bid.GetGameContract(Suit.Hearts, new Bid(4, Suit.Clubs), false));
            Assert.Equal(new Bid(4, Suit.Hearts), Bid.GetGameContract(Suit.Hearts, new Bid(4, Suit.Diamonds), true));
            Assert.Equal(new Bid(5, Suit.Hearts), Bid.GetGameContract(Suit.Hearts, new Bid(4, Suit.Diamonds), false));
            Assert.Equal(new Bid(4, Suit.Hearts), Bid.GetGameContract(Suit.Hearts, new Bid(3, Suit.Spades), false));
            Assert.Equal(new Bid(5, Suit.Hearts), Bid.GetGameContract(Suit.Hearts, new Bid(4, Suit.Spades), false));
            Assert.Equal(new Bid(5, Suit.Hearts), Bid.GetGameContract(Suit.Hearts, new Bid(4, Suit.Spades), false));
            Assert.Equal(new Bid(5, Suit.NoTrump), Bid.GetGameContract(Suit.NoTrump, new Bid(4, Suit.Spades), false));

            Assert.Equal(Bid.InvalidBid, Bid.GetGameContract(Suit.Hearts, new Bid(5, Suit.Diamonds), false));
            Assert.Equal(Bid.InvalidBid, Bid.GetGameContract(Suit.Hearts, new Bid(6, Suit.Spades), false));
            Assert.Equal(Bid.InvalidBid, Bid.GetGameContract(Suit.Hearts, new Bid(6, Suit.Clubs), false));
        }

        [Fact()]
        public void GetSuitsWithFitTest()
        {
            // No fit
            Assert.Equal(new List<Suit>(), Util.GetSuitsWithFit("xxx,xxx,xxx,xxxx", "xxx,xxx,xxxx,xxx"));

            // Fit in spades (8)
            Assert.Equal(new List<Suit> { Suit.Spades }, Util.GetSuitsWithFit("xxxx,xxxx,xxx,xx", "xxxx,xx,xxx,xxxx"));

            // Fit in hearts (9) and clubs (8)
            Assert.Equal(new List<Suit> { Suit.Hearts, Suit.Clubs }, Util.GetSuitsWithFit("xx,xxxxx,xx,xxxx", "xxx,xxxx,xx,xxxx"));
        }
    }
}
