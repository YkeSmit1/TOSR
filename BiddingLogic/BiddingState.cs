using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common;

namespace BiddingLogic
{
    public class BiddingState
    {
        private static List<Fase> SignOffFasesFor3NT { get; } = new List<Fase> { Fase.Pull3NTNoAsk, Fase.Pull3NTOneAskMin, Fase.Pull3NTOneAskMax, Fase.Pull3NTTwoAsks };
        private static List<Fase> SignOffFasesFor4Di { get; } = new List<Fase> { Fase.Pull4DiamondsNoAsk, Fase.Pull4DiamondsOneAskMin, Fase.Pull4DiamondsOneAskMax };
        public static List<Fase> SignOffFases { get; } = SignOffFasesFor3NT.Concat(SignOffFasesFor4Di).ToList();
        private static List<Fase> SignOffFasesWithout3NTNoAsk { get; } = SignOffFasesFor4Di.Concat(new[] { Fase.Pull3NTOneAskMin, Fase.Pull3NTOneAskMax, Fase.Pull3NTTwoAsks }).ToList();

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
                CurrentBid = SignOffFasesFor4Di.Contains(Fase) ? Bid.fourHeartsBid : Bid.PassBid;
                return bidId;
            }

            CurrentBid = Bid.GetBid(bidId);
            BidsPerFase.Add((SignOffFasesWithout3NTNoAsk.Contains(Fase) ? PreviousFase : Fase, CurrentBid));
            if (SignOffFasesWithout3NTNoAsk.Contains(Fase))
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
            if (SignOffFases.Contains(Fase))
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
                RelayBidIdLastFase = bidId + 1 - zoomOffset;
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
            return string.Join("", BidsPerFase.Where(x => new Fase[] { fase }.Contains(x.fase)).Select(x => x.bid));
        }

        public IEnumerable<Bid> GetBids(params Fase[] fases)
        {
            return BidsPerFase.Where(x => fases.Contains(x.fase)).Select(x => x.bid);
        }

        public Bid GetPullBid()
        {
            return BidsPerFase.Where(x => SignOffFases.Contains(x.fase)).Select(x => x.bid).SingleOrDefault();
        }

        public Fase GetPullFase()
        {
            return BidsPerFase.Where(x => SignOffFases.Contains(x.fase)).Select(x => x.fase).SingleOrDefault();
        }

        public static Bid GetSignOffBid(Fase pullFase, Bid pullBid)
        {
            return (pullFase switch
            {
                Fase.Pull3NTNoAsk => pullBid,
                Fase.Pull3NTOneAskMin => Bid.threeNTBid + 1,
                Fase.Pull3NTOneAskMax => Bid.threeNTBid + 1,
                Fase.Pull3NTTwoAsks => Bid.threeNTBid + 1,
                Fase.Pull4DiamondsNoAsk => Bid.fourDiamondBid + 2,
                Fase.Pull4DiamondsOneAskMin => Bid.fourDiamondBid + 2,
                Fase.Pull4DiamondsOneAskMax => Bid.fourDiamondBid + 2,
                _ => throw new InvalidEnumArgumentException(nameof(pullFase)),
            });
        }
    }
}
