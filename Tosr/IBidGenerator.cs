using Common;

namespace Tosr
{
    public interface IBidGenerator
    {
        (int, Fase, string, bool) GetBid(BiddingState biddingState, string handsString);
    }
}