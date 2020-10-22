using Common;

namespace Tosr
{
    public interface IBidGenerator
    {
        (int, Fase, string, int) GetBid(BiddingState biddingState, string handsString);
    }
}