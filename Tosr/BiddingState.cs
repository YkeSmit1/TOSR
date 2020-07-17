namespace Tosr
{
    public class BiddingState
    {
        public BiddingState()
        {
            Init();
        }
        public Fase fase { get; set; }
        public int lastBidId { get; set; }
        public int bidId { get; set; }
        public Bid currentBid { get; set; }
        public int relayBidIdLastFase { get; set; }

        public void Init()
        {
            fase = Fase.Shape;
            lastBidId = 1;
            bidId = int.MaxValue;
            currentBid = Bid.PassBid;
            relayBidIdLastFase = 0;
        }
    }
}
