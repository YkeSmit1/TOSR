using System;
using System.Collections.Generic;
using System.Text;

namespace Tosr
{
    public class BidGenerator : IBidGenerator
    {
        public (int, Fase, string) GetBid(BiddingState biddingState, string handsString, IPinvoke pinvoke)
        {
            var bidFromRule = pinvoke.GetBidFromRule(biddingState.fase, handsString, biddingState.nextBidIdForRule, out var nextfase);
            return (bidFromRule, nextfase, string.Empty);

        }
    }
    public class BidGeneratorDescription : IBidGenerator
    {
        public (int, Fase, string) GetBid(BiddingState biddingState, string handsString, IPinvoke pinvoke)
        {
            var description = new StringBuilder(128);
            var bidFromRule = pinvoke.GetBidFromRuleEx(biddingState.fase, handsString, biddingState.nextBidIdForRule, out var nextfase, description);
            
            return (bidFromRule, nextfase, description.ToString());

        }
    }

}
