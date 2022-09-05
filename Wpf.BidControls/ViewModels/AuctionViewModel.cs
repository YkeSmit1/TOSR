using Common;
using MvvmHelpers;

namespace Wpf.BidControls.ViewModels
{
    public class AuctionViewModel : ObservableObject
    {
        public Auction Auction { get; set; } = new();
        public AuctionViewModel()
        {
            Auction.AddBid(Bid.PassBid);
            Auction.AddBid(new Bid(1, Suit.Diamonds));
        }

        public void UpdateAuction(Auction auction)
        {
            Auction = ObjectCloner.ObjectCloner.DeepClone(auction);
            OnPropertyChanged(nameof(Auction));
        }
    }
}
