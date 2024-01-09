namespace BiddingLogic
{
    public enum Phase
    {
        Unknown,
        Shape,
        Controls,
        ScanningControls,
        ScanningOther,
        End,
        Pull3NTNoAsk,
        Pull3NTOneAskMin,
        Pull3NTOneAskMax,
        Pull3NTTwoAsks,
        Pull4DiamondsNoAsk,
        Pull4DiamondsOneAskMin,
        Pull4DiamondsOneAskMax,
        BidGame,
    };
}
