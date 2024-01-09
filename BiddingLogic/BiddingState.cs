using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common;
using Common.Tosr;

namespace BiddingLogic
{
    public class BiddingState
    {
        private static List<Phase> SignOffPhasesFor3NT { get; } = new() { Phase.Pull3NTNoAsk, Phase.Pull3NTOneAskMin, Phase.Pull3NTOneAskMax, Phase.Pull3NTTwoAsks };
        public static List<Phase> SignOffPhasesFor4Di { get; } = new() { Phase.Pull4DiamondsNoAsk, Phase.Pull4DiamondsOneAskMin, Phase.Pull4DiamondsOneAskMax };
        public static List<Phase> SignOffPhases { get; } = SignOffPhasesFor3NT.Concat(SignOffPhasesFor4Di).ToList();
        private static List<Phase> SignOffPhasesWithout3NTNoAsk { get; } = SignOffPhasesFor4Di.Concat(new[] { Phase.Pull3NTOneAskMin, Phase.Pull3NTOneAskMax, Phase.Pull3NTTwoAsks }).ToList();

        public Phase Phase { get; set; }
        public Bid CurrentBid { get; set; }
        public int RelayBidIdLastPhase { get; set; }
        public int NextBidIdForRule { get; private set; }
        private int PhaseOffset { get; set; }

        private readonly Dictionary<Phase, bool> phasesWithOffset;
        public Phase PreviousPhase { get; private set; }
        public List<(Phase phase, Bid bid)> BidsPerPhase { get; } = new();
        public bool IsZoomShape { get; private set; }
        public bool IsZoomControlScanning { get; private set; }

        public BiddingState(Dictionary<Phase, bool> phasesWithOffset)
        {
            this.phasesWithOffset = phasesWithOffset;
            Init();
        }

        private void Init()
        {
            Phase = Phase.Shape;
            CurrentBid = Bid.PassBid;
            RelayBidIdLastPhase = 0;
            PhaseOffset = 0;
            NextBidIdForRule = 0;
            PreviousPhase = Phase.Unknown;
            BidsPerPhase.Clear();
            IsZoomShape = false;
            IsZoomControlScanning = false;
        }

        public int CalculateBid(int bidIdFromRule, string description, bool zoom)
        {
            var bidId = bidIdFromRule + RelayBidIdLastPhase + PhaseOffset;
            if (bidIdFromRule == 0)
            {
                CurrentBid = SignOffPhasesFor4Di.Contains(Phase) ? Bids.FourHeartsBid : Bid.PassBid;
                return bidId;
            }

            CurrentBid = Bid.GetBid(bidId);
            BidsPerPhase.Add((SignOffPhasesWithout3NTNoAsk.Contains(Phase) ? PreviousPhase : Phase, CurrentBid));
            if (SignOffPhasesWithout3NTNoAsk.Contains(Phase))
                BidsPerPhase.Add((Phase, CurrentBid));
            if (zoom)
            {
                if (Phase == Phase.Shape)
                    IsZoomShape = true;
                if (Phase == Phase.ScanningControls)
                    IsZoomControlScanning = true;
            }
            CurrentBid.description = description;
            return bidId;
        }

        public void UpdateBiddingState(int bidIdFromRule, Phase nextPhase, int bidId, int zoomOffset)
        {
            if (SignOffPhases.Contains(Phase))
            {
                if (Phase == Phase.Pull3NTNoAsk)
                {
                    RelayBidIdLastPhase += bidIdFromRule + 1 - NextBidIdForRule;
                    Phase = PreviousPhase;
                    return;
                }
                Phase = PreviousPhase;
            }

            if (nextPhase != Phase)
            {
                // Specific for zoom. TODO Code is ugly, needs improvement
                if (zoomOffset != 0)
                    zoomOffset--;
                RelayBidIdLastPhase = bidId + 1 - zoomOffset;
                Phase = nextPhase;
                PhaseOffset = 0;
                NextBidIdForRule = zoomOffset;
            }
            else if (phasesWithOffset[Phase])
            {
                PhaseOffset++;
                NextBidIdForRule = bidIdFromRule;
            }
            else
            {
                NextBidIdForRule = bidIdFromRule + 1;
            }
        }

        public void UpdateBiddingStateSignOff(int controlBidCount, Bid relayBid)
        {
            PreviousPhase = Phase;
            if (relayBid.Suit == Suit.NoTrump)
            {
                Phase = controlBidCount switch
                {
                    0 => Phase.Pull3NTNoAsk,
                    1 => Phase == Phase.Controls ? Phase.Pull3NTOneAskMin : Phase.Pull3NTOneAskMax,
                    2 => Phase.Pull3NTTwoAsks,
                    _ => throw new ArgumentOutOfRangeException(nameof(controlBidCount)),
                };
            }
            else
            {
                Phase = controlBidCount switch
                {
                    0 => Phase.Pull4DiamondsNoAsk,
                    1 => Phase == Phase.Controls ? Phase.Pull4DiamondsOneAskMin : Phase.Pull4DiamondsOneAskMax,
                    _ => throw new ArgumentOutOfRangeException(nameof(controlBidCount)),
                };
            }
            RelayBidIdLastPhase = Bid.GetBidId(relayBid) - (Phase == Phase.Pull3NTNoAsk ? 0 : NextBidIdForRule) - PhaseOffset + (relayBid == Bids.FourDiamondBid ? 1 : 0);
        }

        public string GetBidsAsString(Phase fase)
        {
            return string.Join("", BidsPerPhase.Where(x => new[] { fase }.Contains(x.phase)).Select(x => x.bid));
        }

        public IEnumerable<Bid> GetBids(params Phase[] fases)
        {
            return BidsPerPhase.Where(x => fases.Contains(x.phase)).Select(x => x.bid);
        }

        public Bid GetPullBid()
        {
            return BidsPerPhase.Where(x => SignOffPhases.Contains(x.phase)).Select(x => x.bid).SingleOrDefault();
        }

        public Phase GetPullPhase()
        {
            return BidsPerPhase.Where(x => SignOffPhases.Contains(x.phase)).Select(x => x.phase).SingleOrDefault();
        }

        public static Bid GetSignOffBid(Phase pullPhase, Bid pullBid)
        {
            return (pullPhase switch
            {
                Phase.Pull3NTNoAsk => pullBid,
                Phase.Pull3NTOneAskMin => Bids.ThreeNTBid + 1,
                Phase.Pull3NTOneAskMax => Bids.ThreeNTBid + 1,
                Phase.Pull3NTTwoAsks => Bids.ThreeNTBid + 1,
                Phase.Pull4DiamondsNoAsk => Bids.FourDiamondBid + 2,
                Phase.Pull4DiamondsOneAskMin => Bids.FourDiamondBid + 2,
                Phase.Pull4DiamondsOneAskMax => Bids.FourDiamondBid + 2,
                _ => throw new InvalidEnumArgumentException(nameof(pullPhase)),
            });
        }
    }
}
