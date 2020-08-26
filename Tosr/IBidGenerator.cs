namespace Tosr
{
    public interface IBidGenerator
    {
        (int, Fase, string) GetBid(BiddingState biddingState, string handsString, IPinvoke pinvoke);
    }
}