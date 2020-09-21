﻿using System;
using System.Collections.Generic;
using Common;

namespace Tosr
{
    public class BiddingState
    {
        public Fase Fase { get; set; }
        public Bid CurrentBid { get; set; }
        public int RelayBidIdLastFase { get; set; }
        public int NextBidIdForRule { get; set; }
        public int FaseOffset { get; set; }
        public bool EndOfBidding { get; set; }

        private readonly Dictionary<Fase, bool> fasesWithOffset;

        public BiddingState(Dictionary<Fase, bool> fasesWithOffset)
        {
            this.fasesWithOffset = fasesWithOffset;
            Init();
        }

        public void Init()
        {
            Fase = Fase.Shape;
            CurrentBid = Bid.PassBid;
            RelayBidIdLastFase = 0;
            EndOfBidding = false;
            FaseOffset = 0;
            NextBidIdForRule = 0;
        }
        public void UpdateBiddingState(int bidIdFromRule, Fase nextfase, string description, bool zoom)
        {
            var bidId = bidIdFromRule + RelayBidIdLastFase + FaseOffset;
            if (bidIdFromRule == 0)
            {
                CurrentBid = Bid.PassBid;
                EndOfBidding = true;
                return;
            }

            CurrentBid = Bid.GetBid(bidId);
            CurrentBid.fase = Fase;
            CurrentBid.description = description;
            CurrentBid.zoom = zoom;

            if (nextfase != Fase)
            {
                RelayBidIdLastFase = bidId + 1;
                Fase = nextfase;
                FaseOffset = 0;
                NextBidIdForRule = 0;
            }
            else if (fasesWithOffset[Fase])
            {
                FaseOffset++;
                NextBidIdForRule = bidIdFromRule;
            }
            else
            {
                NextBidIdForRule = bidIdFromRule + 1;
            }
        }
    }
}
