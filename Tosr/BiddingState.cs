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

        // The (rel)BidId that is in the database
        public int relLastBidId { get; set; }
        public Bid currentBid { get; set; }
        public int relayBidIdLastFase { get; set; }
        public int FaseOffset { get; set; }
        public bool EndOfBidding { get; set; }

        public void Init()
        {
            fase = Fase.Shape;
            lastBidId = 1;
            currentBid = Bid.PassBid;
            relayBidIdLastFase = 0;
            FaseOffset = 0;
            EndOfBidding = false;
        }
        public void UpdateBiddingState(int bidIdFromRule, Fase nextfase, string description)
        {
            relLastBidId = bidIdFromRule;
            var bidId = bidIdFromRule + relayBidIdLastFase + FaseOffset;
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
                relLastBidId = 0;
                relayBidIdLastFase = bidId + 1;
                fase = nextfase;
                FaseOffset = 0;
            }
            else if (fase.HasOffset())
            {
                FaseOffset++;
            }
        }
    }
}
