using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Dictionary<Fase, bool> fasesWithOffset;
        public Fase PreviousFase { get; private set; }
        public List<(Fase fase, Bid bid)> BidsPerFase { get; set; } = new();
        public bool IsZoomShape { get; set; }
        public bool IsZoomControlScanning { get; set; }

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
            FaseOffset = 0;
            NextBidIdForRule = 0;
            PreviousFase = Fase.Unknown;
            BidsPerFase.Clear();
            IsZoomShape = false;
            IsZoomControlScanning = false;
        }
        public int CalculateBid(int bidIdFromRule, string description, bool zoom)
        {
            var bidId = bidIdFromRule + RelayBidIdLastFase + FaseOffset;
            if (bidIdFromRule == 0)
            {
                CurrentBid = Util.signOffFasesFor4Di.Contains(Fase) ? Bid.fourHeartsBid : Bid.PassBid;
                return bidId;
            }

            CurrentBid = Bid.GetBid(bidId);
            BidsPerFase.Add((Util.signOffFasesWithout3NTNoAsk.Contains(Fase) ? PreviousFase : Fase, CurrentBid));
            if (Util.signOffFasesWithout3NTNoAsk.Contains(Fase))
                BidsPerFase.Add((Fase, CurrentBid));
            if (zoom)
            {
                if (Fase == Fase.Shape)
                    IsZoomShape = true;
                if (Fase == Fase.ScanningControls)
                    IsZoomControlScanning = true;
            }
            CurrentBid.description = description;
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

        public string GetBidsAsString(Fase fase)
        {
            return GetBidsAsString(new Fase[] { fase });
        }

        public string GetBidsAsString(Fase[] fases)
        {
            return string.Join("", BidsPerFase.Where(x => fases.Contains(x.fase)).Select(x => x.bid));
        }

        public IEnumerable<Bid> GetBids(Player player, Fase fase)
        {
            return GetBids(new Fase[] { fase });
        }

        public IEnumerable<Bid> GetBids(Fase[] fases)
        {
            return BidsPerFase.Where(x => fases.Contains(x.fase)).Select(x => x.bid);
        }

        public IEnumerable<Bid> GetPullBids(Player player)
        {
            return BidsPerFase.Where(x => Util.signOffFases.Contains(x.fase)).Select(x => x.bid);
        }

        public Fase GetPullFase()
        {
            return BidsPerFase.Where(x => Util.signOffFases.Contains(x.fase)).Select(x => x.fase).SingleOrDefault();
        }


    }
}
