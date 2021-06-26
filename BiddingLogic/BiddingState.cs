using System;
using System.Collections.Generic;
using Common;

namespace BiddingLogic
{
    public class BiddingState
    {
        public Fase Fase { get; set; }
        public Bid CurrentBid { get; set; }
        public int RelayBidIdLastFase { get; set; }
        public int NextBidIdForRule { get; private set; }
        private int FaseOffset { get; set; }
        public bool EndOfBidding { get; set; }

        private readonly Dictionary<Fase, bool> fasesWithOffset;
        public Fase PreviousFase { get; private set; }

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
            PreviousFase = Fase.Unknown;
        }
        public int CalculateBid(int bidIdFromRule, string description, bool zoom)
        {
            var bidId = bidIdFromRule + RelayBidIdLastFase + FaseOffset;
            if (bidIdFromRule == 0)
            {
                if (Util.signOffFasesFor4Di.Contains(Fase))
                {
                    CurrentBid = Bid.fourHeartsBid;
                }
                else
                {
                    CurrentBid = Bid.PassBid;
                    EndOfBidding = true;
                }
                return bidId;
            }

            CurrentBid = Bid.GetBid(bidId);
            CurrentBid.fase = Util.signOffFasesWithout3NTNoAsk.Contains(Fase) ? PreviousFase : Fase;
            CurrentBid.pullFase = Util.signOffFases.Contains(Fase) ? Fase : Fase.Unknown;
            CurrentBid.description = description;
            CurrentBid.zoom = zoom;
            return bidId;
        }

        public void UpdateBiddingState(int bidIdFromRule, Fase nextfase, int bidId, int zoomOffset)
        {
            if (Util.signOffFases.Contains(Fase))
            {
                if (Fase == Fase.Pull3NTNoAsk)
                {
                    RelayBidIdLastFase += bidIdFromRule + 1 - NextBidIdForRule;
                    Fase = PreviousFase;
                    return;
                }
                Fase = PreviousFase;
            }

            if (nextfase != Fase)
            {
                // Specific for zoom. TODO Code is ugly, needs improvement
                RelayBidIdLastFase = (bidId + 1) - zoomOffset;
                Fase = nextfase;
                FaseOffset = 0;
                NextBidIdForRule = zoomOffset;
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

        public Bid UpdateBiddingStateSignOff(int controlBidCount, Bid relayBid)
        {
            PreviousFase = Fase;
            if (relayBid.suit == Suit.NoTrump)
            {
                Fase = controlBidCount switch
                {
                    0 => Fase.Pull3NTNoAsk,
                    1 => Fase == Fase.Controls ? Fase.Pull3NTOneAskMin : Fase.Pull3NTOneAskMax,
                    2 => Fase.Pull3NTTwoAsks,
                    _ => throw new ArgumentOutOfRangeException(nameof(controlBidCount)),
                };
            }
            else
            {
                Fase = controlBidCount switch
                {
                    0 => Fase.Pull4DiamondsNoAsk,
                    1 => Fase == Fase.Controls ? Fase.Pull4DiamondsOneAskMin : Fase.Pull4DiamondsOneAskMax,
                    _ => throw new ArgumentOutOfRangeException(nameof(controlBidCount)),
                };
            }
            RelayBidIdLastFase = Bid.GetBidId(relayBid) - (Fase == Fase.Pull3NTNoAsk ? 0 : NextBidIdForRule) - FaseOffset + (relayBid == Bid.fourDiamondBid ? 1 : 0);
            return relayBid;
        }
    }
}
