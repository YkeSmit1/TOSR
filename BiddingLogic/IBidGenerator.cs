using Common;

namespace BiddingLogic
{
    public interface IBidGenerator
    {
        (int, Fase, string, int) GetBid(BiddingState biddingState, string handsString);
    }
}