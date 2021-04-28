using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace Tosr
{
    public class BidGenerator : IBidGenerator
    {
        public (int, Fase, string, int) GetBid(BiddingState biddingState, string handsString)
        {
            var bidFromRule = Pinvoke.GetBidFromRule(biddingState.Fase, biddingState.PreviousFase, handsString, biddingState.NextBidIdForRule, out var nextfase, out var zoomOffset);
            return (bidFromRule, nextfase, string.Empty, zoomOffset);

        }
    }
    public class BidGeneratorDescription : IBidGenerator
    {
        public (int, Fase, string, int) GetBid(BiddingState biddingState, string handsString)
        {
            var description = new StringBuilder(128);
            var bidFromRule = Pinvoke.GetBidFromRuleEx(biddingState.Fase, biddingState.PreviousFase, handsString, biddingState.NextBidIdForRule, out var nextfase, description);

            return (bidFromRule, nextfase, description.ToString(), 0);

        }
    }

}
