namespace BiddingLogic
{
    public interface IBidGenerator
    {
        (int, Phase, string, int) GetBid(BiddingState biddingState, string handsString);
    }
}