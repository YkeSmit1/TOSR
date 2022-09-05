using System.Text;

namespace BiddingLogic
{
    public class BidGenerator : IBidGenerator
    {
        public (int, Fase, string, int) GetBid(BiddingState biddingState, string handsString)
        {
            var bidFromRule = PInvoke.GetBidFromRule(biddingState.Fase, biddingState.PreviousFase, handsString, biddingState.NextBidIdForRule, out var nextFase, out var zoomOffset);
            return (bidFromRule, nextFase, string.Empty, zoomOffset);

        }
    }
    public class BidGeneratorDescription : IBidGenerator
    {
        public (int, Fase, string, int) GetBid(BiddingState biddingState, string handsString)
        {
            var description = new StringBuilder(128);
            var bidFromRule = PInvoke.GetBidFromRuleEx(biddingState.Fase, biddingState.PreviousFase, handsString, biddingState.NextBidIdForRule, out var nextFase, description);

            return (bidFromRule, nextFase, description.ToString(), 0);

        }
    }

}
