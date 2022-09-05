using System;
using System.Collections.ObjectModel;
using System.Linq;
using Common;
using MvvmHelpers;
using MvvmHelpers.Commands;

namespace Wpf.BidControls.ViewModels
{
    public class BiddingBoxViewModel : ObservableObject
    {
        public ObservableCollection<Grouping<int, Bid>> SuitBids { get; }
        public ObservableCollection<Bid> NonSuitBids { get; }

        public Command DoBid { get; set; }

        public BiddingBoxViewModel()
        {
            SuitBids = new ObservableCollection<Grouping<int, Bid>>(Enumerable.Range(1, 7)
                .Select(level => new Grouping<int, Bid>(level, Enum.GetValues(typeof(Suit)).Cast<Suit>()
                .Select(suit => new Bid(level, suit)))));
            NonSuitBids = new ObservableCollection<Bid> { Bid.PassBid, Bid.Dbl, Bid.Rdbl };
        }
    }
}
