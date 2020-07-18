using System;
using System.Collections.Generic;
using System.Text;

namespace Tosr
{
    class BidGenerator : IBidGenerator
    {
        public (int, Fase, string) GetBid(BiddingState biddingState, string handsString, bool requestDescription)
        {
            var description = new StringBuilder(128);
            var bidFromRule = requestDescription ?
                    Pinvoke.GetBidFromRuleEx(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out var nextfase, description) :
                    Pinvoke.GetBidFromRule(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out nextfase);
            return (bidFromRule, nextfase, description.ToString());

        }
    }
}
