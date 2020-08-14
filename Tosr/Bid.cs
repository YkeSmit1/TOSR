using System;

namespace Tosr
{
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
    }
}