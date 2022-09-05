namespace Common.Tosr
{
    public static class Bids
    {
        public static readonly Bid OneClub = new(1, Suit.Clubs);
        public static readonly Bid TwoNTBid = new(2, Suit.NoTrump);
        public static readonly Bid ThreeDiamondBid = new(3, Suit.Diamonds);
        public static readonly Bid ThreeSpadeBid = new(3, Suit.Spades);
        public static readonly Bid ThreeNTBid = new(3, Suit.NoTrump);
        public static readonly Bid FourClubBid = new(4, Suit.Clubs);
        public static readonly Bid FourDiamondBid = new(4, Suit.Diamonds);
        public static readonly Bid FourHeartsBid = new(4, Suit.Hearts);
        public static readonly Bid FourNTBid = new(4, Suit.NoTrump);
        public static readonly Bid FiveClubBid = new(5, Suit.Clubs);
        public static readonly Bid FiveDiamondBid = new(5, Suit.Diamonds);
        public static readonly Bid FiveHeartsBid = new(5, Suit.Hearts);
        public static readonly Bid SixSpadeBid = new(6, Suit.Spades);
    }
}