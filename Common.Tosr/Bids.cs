namespace Common.Tosr
{
    public static class Bids
    {
        public static readonly Bid OneClub = new Bid(1, Suit.Clubs);
        public static readonly Bid twoNTBid = new Bid(2, Suit.NoTrump);
        public static readonly Bid threeDiamondBid = new Bid(3, Suit.Diamonds);
        public static readonly Bid threeSpadeBid = new Bid(3, Suit.Spades);
        public static readonly Bid threeNTBid = new Bid(3, Suit.NoTrump);
        public static readonly Bid fourClubBid = new Bid(4, Suit.Clubs);
        public static readonly Bid fourDiamondBid = new Bid(4, Suit.Diamonds);
        public static readonly Bid fourHeartsBid = new Bid(4, Suit.Hearts);
        public static readonly Bid fourNTBid = new Bid(4, Suit.NoTrump);
        public static readonly Bid fiveClubBid = new Bid(5, Suit.Clubs);
        public static readonly Bid fiveDiamondBid = new Bid(5, Suit.Diamonds);
        public static readonly Bid fiveHeartsBid = new Bid(5, Suit.Hearts);
        public static readonly Bid sixSpadeBid = new Bid(6, Suit.Spades);
    }
}