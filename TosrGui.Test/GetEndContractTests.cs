using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiddingLogic;
using Common;

namespace TosrGui.Test
{
    public class GetEndContractTests
    {
        public static IEnumerable<object[]> TestCasesOneSuit()
        {
            // Two bids
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 0 }, { new Bid(6, Suit.Spades), 0 } }, new Bid(3, Suit.NoTrump) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 1 } }, new Bid(4, Suit.NoTrump) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(6, Suit.Spades), 2 } }, new Bid(4, Suit.NoTrump) };
            yield return new object[] { new Bid(4, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 0 }, { new Bid(5, Suit.Spades), 0 } }, new Bid(3, Suit.NoTrump) };
            yield return new object[] { new Bid(4, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 } }, new Bid(3, Suit.NoTrump) };
            yield return new object[] { new Bid(4, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 2 }, { new Bid(5, Suit.Spades), 1 } }, new Bid(3, Suit.NoTrump) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(6, Suit.Spades), 2 } }, new Bid(4, Suit.Clubs) };
            yield return new object[] { new Bid(4, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 1 } }, new Bid(4, Suit.Clubs) };
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(6, Suit.Spades), 2 } }, new Bid(4, Suit.Diamonds) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(6, Suit.Spades), 2 } }, new Bid(4, Suit.Hearts) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 1 } }, new Bid(4, Suit.Hearts) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(6, Suit.Spades), 2 } }, new Bid(4, Suit.Spades) };
            yield return new object[] { new Bid(4, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 1 } }, new Bid(4, Suit.Spades) };
            // Three bids
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(3, Suit.NoTrump) };
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(4, Suit.Diamonds) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 4 } }, new Bid(5, Suit.Clubs) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 2 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(5, Suit.Clubs) };
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(4, Suit.NoTrump) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(5, Suit.NoTrump) };
            // NT
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.NoTrump), 0 }, { new Bid(6, Suit.NoTrump), 0 } }, new Bid(3, Suit.NoTrump) };
            yield return new object[] { new Bid(6, Suit.NoTrump), new Dictionary<Bid, int> { { new Bid(4, Suit.NoTrump), 2 }, { new Bid(6, Suit.NoTrump), 1 } }, new Bid(5, Suit.Clubs) };
            yield return new object[] { new Bid(4, Suit.NoTrump), new Dictionary<Bid, int> { { new Bid(4, Suit.NoTrump), 2 }, { new Bid(6, Suit.NoTrump), 1 } }, new Bid(4, Suit.Diamonds) };
        }

        public static IEnumerable<object[]> TestCasesTwoSuits()
        {
            var possibleContracts = new Dictionary<Bid, int> { 
                { new Bid(4, Suit.Spades), 4 }, { new Bid(6, Suit.Spades), 6 }, 
                { new Bid(5, Suit.Diamonds), 6 }, { new Bid(6, Suit.Diamonds), 4 },
                { new Bid(3, Suit.NoTrump), 3 }, { new Bid(4, Suit.NoTrump), 4 }, { new Bid(6, Suit.NoTrump), 3 } };
            yield return new object[] { null, possibleContracts, new Bid(2, Suit.NoTrump) };
            yield return new object[] { new Bid(BidType.pass), possibleContracts, new Bid(7, Suit.NoTrump) };
            yield return new object[] { null, possibleContracts, new Bid(4, Suit.Diamonds) };
            yield return new object[] { new Bid(6, Suit.Spades), possibleContracts, new Bid(4, Suit.Spades) };
        }

        [Theory]
        [MemberData(nameof(TestCasesOneSuit))]
        public void GetEndContractOneSuitTest(Bid expectedBid, Dictionary<Bid, int> possibleContracts, Bid currentBid)
        {
            Assert.Equal(expectedBid, BidManager.GetEndContract(possibleContracts, currentBid));
        }

        [Theory]
        [MemberData(nameof(TestCasesTwoSuits))]
        public void GetEndContractTwoSuitsTest(Bid expectedBid, Dictionary<Bid, int> possibleContracts, Bid currentBid)
        {
            Assert.Equal(expectedBid, BidManager.GetEndContract(possibleContracts, currentBid));
        }
    }
}