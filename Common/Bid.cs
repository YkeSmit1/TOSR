﻿using System;

namespace Common
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
        public static readonly Bid PassBid = new Bid(BidType.pass);
        public static readonly Bid Dbl = new Bid(BidType.dbl);
        public static readonly Bid Rdbl = new Bid(BidType.rdbl);

        public static readonly Bid twoNTBid = new Bid(2, Suit.NoTrump);
        public static readonly Bid threeDiamondBid = new Bid(3, Suit.Diamonds);
        public static readonly Bid threeSpadeBid = new Bid(3, Suit.Spades);
        public static readonly Bid threeNTBid = new Bid(3, Suit.NoTrump);
        public static readonly Bid fourClubBid = new Bid(4, Suit.Clubs);
        public static readonly Bid fourDiamondBid = new Bid(4, Suit.Diamonds);
        public static readonly Bid fourHeartsBid = new Bid(4, Suit.Hearts);
        public static readonly Bid fourNTBid = new Bid(4, Suit.NoTrump);

        public readonly BidType bidType;
        public readonly int rank;
        public readonly Suit suit;
        public string description = string.Empty;
        public Fase fase = Fase.Unknown;
        public bool zoom;

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
                BidType.bid => rank + Util.GetSuitDescription(suit),
                BidType.pass => "Pass",
                BidType.dbl => "Dbl",
                BidType.rdbl => "Rdbl",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static Bid GetBid(int bidId)
        {
            return bidId == 0 ? PassBid : new Bid((bidId - 1) / 5 + 1, (Suit)((bidId - 1) % 5));
        }

        public static int GetBidId(Bid bid)
        {
            return ((bid.rank - 1) * 5) + (int)bid.suit + 1;
        }

        public static Bid NextBid(Bid bid)
        {
            if (bid == PassBid)
                return new Bid(1, Suit.Clubs);
            if (bid.suit == Suit.NoTrump)
                return new Bid(bid.rank + 1, Suit.Clubs);
            return new Bid(bid.rank, bid.suit + 1);
        }

        public static Bid GetGameContract(Suit trumpSuit, Bid currentBid)
        {
            var bid = trumpSuit switch
            {
                Suit.Spades => new Bid(4, Suit.Spades),
                Suit.Hearts => new Bid(4, Suit.Hearts),
                Suit.Diamonds => new Bid(5, Suit.Diamonds),
                Suit.Clubs => new Bid(5, Suit.Clubs),
                Suit.NoTrump => new Bid(3, Suit.NoTrump),
                _ => throw new ArgumentException(nameof(trumpSuit)),
            };
            return CheapestContract(currentBid, bid);
        }

        private static Bid CheapestContract(Bid currentBid, Bid bid)
        {
            return currentBid.suit == bid.suit ? PassBid : 
                currentBid < bid ? bid : 
                currentBid.suit < bid.suit ? new Bid(currentBid.rank, bid.suit) : 
                new Bid(currentBid.rank + 1, bid.suit);
        }

        // Operators
        public bool Equals(Bid other) => !(other is null) && suit == other.suit && bidType == other.bidType && rank == other.rank;
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
        public static bool operator ==(Bid a, Bid b) => a is null ? b is null : a.Equals(b);
        public static bool operator !=(Bid a, Bid b) => !(a == b);
        public static bool operator <(Bid a, Bid b) => a.CompareTo(b) < 0;
        public static bool operator >(Bid a, Bid b) => a.CompareTo(b) > 0;
        public static bool operator <=(Bid a, Bid b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Bid a, Bid b) => a.CompareTo(b) >= 0;
        public static int operator -(Bid a, Bid b) => GetBidId(a) - GetBidId(b);
        public static Bid operator -(Bid a, int i) => a.bidType == BidType.bid ? DecreaseBid(a, i) : a;
        public static Bid operator --(Bid a) => a.bidType == BidType.bid ? DecreaseBid(a, 1) : a;
        public static Bid operator +(Bid a, int i) => a.bidType == BidType.bid ? IncreaseBid(a, i) : a;
        public static Bid operator ++(Bid a) => a.bidType == BidType.bid ? IncreaseBid(a, 1) : a;

        private static Bid IncreaseBid(Bid a, int i)
        {
            var bid = GetBid(GetBidId(a) + i);
            bid.fase = a.fase;
            bid.description = a.description;
            return bid;
        }

        private static Bid DecreaseBid(Bid a, int i)
        {
            var bid = GetBid(GetBidId(a) - i);
            bid.fase = a.fase;
            bid.description = a.description;
            return bid;
        }
    }
}