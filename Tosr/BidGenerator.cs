using System.Text;

namespace Tosr
{
    public class BidGenerator : IBidGenerator
    {
        public (int, Fase, string) GetBid(BiddingState biddingState, string handsString)
        {
            var bidFromRule = Pinvoke.GetBidFromRule(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out var nextfase);
            return (bidFromRule, nextfase, string.Empty);

        }
    }
    public class BidGeneratorDescription : IBidGenerator
    {
        public (int, Fase, string) GetBid(BiddingState biddingState, string handsString)
        {
            var description = new StringBuilder(128);
            var bidFromRule = Pinvoke.GetBidFromRuleEx(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out var nextfase, description);
            return (bidFromRule, nextfase, description.ToString());

        }
    }

}
