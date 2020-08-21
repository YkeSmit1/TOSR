using System.Collections.Generic;

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
        public int nextBidIdForRule { get; set; }
        public int FaseOffset { get; set; }
        public bool EndOfBidding { get; set; }
        Dictionary<Fase, bool> fasesWithOffset;

        public BiddingState(Dictionary<Fase, bool> fasesWithOffset)
        {
            this.fasesWithOffset = fasesWithOffset;
            Init();
        }

        public void Init()
        {
            fase = Fase.Shape;
            lastBidId = 1;
            currentBid = Bid.PassBid;
            relayBidIdLastFase = 0;
            EndOfBidding = false;
            FaseOffset = 0;
            nextBidIdForRule = 0;
        }
        public void UpdateBiddingState(int bidIdFromRule, Fase nextfase, string description)
        {
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
                relayBidIdLastFase = bidId + 1;
                fase = nextfase;
                FaseOffset = 0;
                nextBidIdForRule = 0;
            }
            else if (fasesWithOffset[fase])
            {
                FaseOffset++;
                nextBidIdForRule = bidIdFromRule;
            }
            else
            {
                nextBidIdForRule = bidIdFromRule + 1;
            }
        }
    }
}
