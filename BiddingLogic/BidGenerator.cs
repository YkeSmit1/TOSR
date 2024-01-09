using System.Text;

namespace BiddingLogic
{
    public class BidGenerator : IBidGenerator
    {
        public (int, Phase, string, int) GetBid(BiddingState biddingState, string handsString)
        {
            var bidFromRule = PInvoke.GetBidFromRule(biddingState.Phase, biddingState.PreviousPhase, handsString, biddingState.NextBidIdForRule, out var nextPhase, out var zoomOffset);
            return (bidFromRule, nextPhase, string.Empty, zoomOffset);

        }
    }
    public class BidGeneratorDescription : IBidGenerator
    {
        public (int, Phase, string, int) GetBid(BiddingState biddingState, string handsString)
        {
            var description = new StringBuilder(128);
            var bidFromRule = PInvoke.GetBidFromRuleEx(biddingState.Phase, biddingState.PreviousPhase, handsString, biddingState.NextBidIdForRule, out var nextPhase, description);

            return (bidFromRule, nextPhase, description.ToString(), 0);

        }
    }

}
