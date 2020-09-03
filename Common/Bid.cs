using System;

namespace Tosr
{
    public enum BidType
    {
        bid,
        pass,
        dbl,
        rdbl
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
        public Fase fase = Fase.Unknown;

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

        public static Bid GetBid(int bidId)
        {
            return bidId == 0 ? Bid.PassBid : new Bid((bidId - 1) / 5 + 1, (Suit)((bidId - 1) % 5));
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

        public bool Equals(Bid other) => suit == other.suit && bidType == other.bidType && rank == other.rank;
        public override bool Equals(object obj) => obj is Bid other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(bidType, rank, suit);

        public int CompareTo(Bid other)
        {
            var bidTypeComparison = bidType.CompareTo(other.bidType);
            if (bidTypeComparison != 0) return bidTypeComparison;

            var rankComparison = rank.CompareTo(other.rank);
            if (rankComparison != 0) return rankComparison;

            return suit.CompareTo(other.suit);
        }
        public static bool operator ==(Bid a, Bid b) => a.Equals(b);
        public static bool operator !=(Bid a, Bid b) => !a.Equals(b);
        public static bool operator <(Bid a, Bid b) => a.CompareTo(b) < 0;
        public static bool operator >(Bid a, Bid b) => a.CompareTo(b) > 0;
        public static bool operator <=(Bid a, Bid b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Bid a, Bid b) => a.CompareTo(b) >= 0;
        public static int operator -(Bid a, Bid b) => GetBidId(a) - GetBidId(b);
        public static Bid operator -(Bid a, int i) => a.bidType == BidType.bid ? GetBid(GetBidId(a) - i) : a;
    }
}