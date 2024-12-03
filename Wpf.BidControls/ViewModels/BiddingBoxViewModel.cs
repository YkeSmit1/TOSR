using System;
using System.Collections.ObjectModel;
using System.Linq;
using Common;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoreLinq;

namespace Wpf.BidControls.ViewModels
{
    public class BiddingBoxViewModel : ObservableObject
    {
        public ObservableGroupedCollection<int, Bid> SuitBids { get; }
        public ObservableCollection<Bid> NonSuitBids { get; }

        public RelayCommand<Bid> DoBid { get; set; }

        public BiddingBoxViewModel()
        {
            var collection = Enumerable.Range(1, 7)
                .Cartesian(Enum.GetValues<Suit>(), (level, suit) => new Bid(level, suit)).GroupBy(x => x.Rank, x => x);
            SuitBids = new ObservableGroupedCollection<int, Bid>(collection);
            NonSuitBids = [Bid.PassBid, Bid.Dbl, Bid.Rdbl];
        }
    }
}
