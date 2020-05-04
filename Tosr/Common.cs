using System;
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
            switch (suit)
            {
                case Suit.Clubs:
                    return "\u2663";
                case Suit.Diamonds:
                    return "\u2666";
                case Suit.Hearts:
                    return "\u2665";
                case Suit.Spades:
                    return "\u2660";
                case Suit.NoTrump:
                    return "NT";
                default:
                    throw new ArgumentOutOfRangeException(nameof(suit), suit, null);
            }
        }

        public static char GetFaceDescription(Face face)
        {
            switch (face)
            {
                case Face.Ace:
                    return 'A';
                case Face.Two:
                    return '2';
                case Face.Three:
                    return '3';
                case Face.Four:
                    return '4';
                case Face.Five:
                    return '5';
                case Face.Six:
                    return '6';
                case Face.Seven:
                    return '7';
                case Face.Eight:
                    return '8';
                case Face.Nine:
                    return '9';
                case Face.Ten:
                    return 'T';
                case Face.Jack:
                    return 'J';
                case Face.Queen:
                    return 'Q';
                case Face.King:
                    return 'K';
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
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


    }

    public struct Bid : IEquatable<Bid>, IComparable<Bid>
    {
        public static Bid PassBid = new Bid(BidType.pass);
        public static Bid Dbl = new Bid(BidType.dbl);
        public static Bid Rdbl = new Bid(BidType.rdbl);

        public readonly BidType bidType;
        public readonly int rank;
        public readonly Suit suit;

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
            switch (bidType)
            {
                case BidType.bid:
                    return rank + Common.GetSuitDescription(suit);
                case BidType.pass:
                    return "Pass";
                case BidType.dbl:
                    return "Dbl";
                case BidType.rdbl:
                    return "Rdbl";
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            unchecked
            {
                var hashCode = (int) bidType;
                hashCode = (hashCode * 397) ^ rank;
                hashCode = (hashCode * 397) ^ (int) suit;
                return hashCode;
            }
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