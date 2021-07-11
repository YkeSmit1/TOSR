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
        public static IEnumerable<object[]> TestCases()
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
            yield return new object[] { new Bid(5, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 2 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(5, Suit.Clubs) };
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(4, Suit.NoTrump) };
            yield return new object[] { new Bid(6, Suit.Spades), new Dictionary<Bid, int> { { new Bid(4, Suit.Spades), 1 }, { new Bid(5, Suit.Spades), 2 }, { new Bid(6, Suit.Spades), 3 } }, new Bid(5, Suit.NoTrump) };
            // NT
            yield return new object[] { null, new Dictionary<Bid, int> { { new Bid(4, Suit.NoTrump), 0 }, { new Bid(6, Suit.NoTrump), 0 } }, new Bid(3, Suit.NoTrump) };
            yield return new object[] { new Bid(6, Suit.NoTrump), new Dictionary<Bid, int> { { new Bid(4, Suit.NoTrump), 2 }, { new Bid(6, Suit.NoTrump), 1 } }, new Bid(5, Suit.Clubs) };
            yield return new object[] { new Bid(4, Suit.NoTrump), new Dictionary<Bid, int> { { new Bid(4, Suit.NoTrump), 2 }, { new Bid(6, Suit.NoTrump), 1 } }, new Bid(4, Suit.Diamonds) };
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void GetEndContractTest(Bid expectedBid, Dictionary<Bid, int> possibleContracts, Bid currentBid)
        {
            Assert.Equal(expectedBid, BidManager.GetEndContract(possibleContracts, currentBid));
        }
    }
}