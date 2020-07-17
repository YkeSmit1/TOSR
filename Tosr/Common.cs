using System;
using System.Collections.Generic;
using System.Linq;

namespace Tosr
{
    public enum Player
    {
        West,
        North,
        East,
        South,
        UnKnown
    };

    public enum BidType
    {
        bid,
        pass,
        dbl,
        rdbl
    }

    /// <summary>
    /// enumeration that represents suit values
    /// </summary>
    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3,
        NoTrump = 4
    }

    /// <summary>
    /// enumeration that represents face values
    /// </summary>
    public enum Face
    {
        Ace,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }

    public class Common
    {
        public static string GetSuitDescription(Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => "\u2663",
                Suit.Diamonds => "\u2666",
                Suit.Hearts => "\u2665",
                Suit.Spades => "\u2660",
                Suit.NoTrump => "NT",
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static char GetFaceDescription(Face face)
        {
            return face switch
            {
                Face.Ace => 'A',
                Face.Two => '2',
                Face.Three => '3',
                Face.Four => '4',
                Face.Five => '5',
                Face.Six => '6',
                Face.Seven => '7',
                Face.Eight => '8',
                Face.Nine => '9',
                Face.Ten => 'T',
                Face.Jack => 'J',
                Face.Queen => 'Q',
                Face.King => 'K',
                _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
            };
        }

        public static bool IsSameTeam(Player player1, Player player2)
        {
            return player1 == Player.North && player2 == Player.South
                   || player1 == Player.West && player2 == Player.East
                || player1 == player2 && player1 == Player.UnKnown;
        }

        public static Bid GetBid(int bidId)
        {
            if (bidId == 0)
                return Bid.PassBid;
            return new Bid((bidId - 1) / 5 + 1, (Suit)((bidId - 1) % 5));
        }

        public static int GetBidId(Bid bid)
        {
            return ((bid.rank - 1) * 5) + (int)bid.suit + 1;
        }

        public static Bid NextBid(Bid bid)
        {
            if (bid == Bid.PassBid)
                return new Bid(1, Suit.Clubs);
            if (bid.suit == Suit.NoTrump)
                return new Bid(bid.rank + 1, Suit.Clubs);
            return new Bid(bid.rank, bid.suit + 1);
        }

        public static string GetDeckAsString(IEnumerable<CardDto> orderedCards)
        {
            var listCards = orderedCards.ToList();
            var suitAsString = SuitAsString(listCards, Suit.Spades) + "," +
                               SuitAsString(listCards, Suit.Hearts) + "," +
                               SuitAsString(listCards, Suit.Diamonds) + "," +
                               SuitAsString(listCards, Suit.Clubs);
            return suitAsString;
        }

        public static string SuitAsString(IEnumerable<CardDto> listCards, Suit suit)
        {
            return listCards.Where(c => c.Suit == suit).Aggregate("", (x, y) => x + Common.GetFaceDescription(y.Face));
        }

    }

    public class Bid : IEquatable<Bid>, IComparable<Bid>
    {
        public static Bid PassBid = new Bid(BidType.pass);
        public static Bid Dbl = new Bid(BidType.dbl);
        public static Bid Rdbl = new Bid(BidType.rdbl);

        public readonly BidType bidType;
        public readonly int rank;
        public readonly Suit suit;
        public string description = string.Empty;

        public Bid(int rank, Suit suit)
        {
            bidType = BidType.bid;
            this.suit = suit;
            this.rank = rank;
        }

        public Bid(BidType bidType)
        {
            this.bidType = bidType;
            suit = default;
            rank = default;
        }


        public override string ToString()
        {
            return bidType switch
            {
                BidType.bid => rank + Common.GetSuitDescription(suit),
                BidType.pass => "Pass",
                BidType.dbl => "Dbl",
                BidType.rdbl => "Rdbl",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public bool Equals(Bid other)
        {
            return suit == other.suit && bidType == other.bidType && rank == other.rank;
        }

        public override bool Equals(object obj)
        {
            return obj is Bid other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(bidType, rank, suit);
        }

        public int CompareTo(Bid other)
        {
            var bidTypeComparison = bidType.CompareTo(other.bidType);
            if (bidTypeComparison != 0) return bidTypeComparison;

            var rankComparison = rank.CompareTo(other.rank);
            if (rankComparison != 0) return rankComparison;

            return suit.CompareTo(other.suit);
        }
        public static bool operator <(Bid a, Bid b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(Bid a, Bid b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator ==(Bid a, Bid b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Bid a, Bid b)
        {
            return !(a == b);
        }

        public static bool operator <=(Bid left, Bid right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Bid left, Bid right)
        {
            return left.CompareTo(right) >= 0;
        }
    }

}