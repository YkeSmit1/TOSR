﻿using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace Tosr
{
    public class BidGenerator : IBidGenerator
    {
        public (int, Fase, string, bool) GetBid(BiddingState biddingState, string handsString)
        {
            var bidFromRule = Pinvoke.GetBidFromRule(biddingState.Fase, handsString, biddingState.NextBidIdForRule, out var nextfase, out var zoom);
            return (bidFromRule, nextfase, string.Empty, zoom);

        }
    }
    public class BidGeneratorDescription : IBidGenerator
    {
        public (int, Fase, string, bool) GetBid(BiddingState biddingState, string handsString)
        {
            var description = new StringBuilder(128);
            var bidFromRule = Pinvoke.GetBidFromRuleEx(biddingState.Fase, handsString, biddingState.NextBidIdForRule, out var nextfase, description);
            
            return (bidFromRule, nextfase, description.ToString(), false);

        }
    }

}
