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
        public Bid currentBid { get; set; }
        public int relayBidIdLastFase { get; set; }

        public bool EndOfBidding { get; set; }

        public void Init()
        {
            fase = Fase.Shape;
            lastBidId = 1;
            currentBid = Bid.PassBid;
            relayBidIdLastFase = 0;
            EndOfBidding = false;
        }
        public void UpdateBiddingState(int bidIdFromRule, Fase nextfase, string description)
        {
            var bidId = bidIdFromRule + relayBidIdLastFase;
            if (bidIdFromRule == 0)
            {
                currentBid = Bid.PassBid;
                EndOfBidding = true;
                return;
            }

            currentBid = BidManager.GetBid(bidId);
            currentBid.fase = fase;
            currentBid.description = description;

            if (nextfase != fase)
            {
                relayBidIdLastFase = bidId + 1;
                fase = nextfase;
            }
        }
    }
}
