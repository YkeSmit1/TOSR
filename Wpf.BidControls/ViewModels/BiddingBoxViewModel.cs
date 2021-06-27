using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Common;
using MvvmHelpers;
using MvvmHelpers.Commands;

namespace Wpf.BidControls.ViewModels
{
    public class BiddingBoxViewModel : ObservableObject
    {
        public ObservableCollection<Grouping<int, Bid>> SuitBids { get; set; } = new();
        public ObservableCollection<Bid> NonSuitBids { get; set; } = new();

        public Command DoBid { get; set; }
        public bool IsEnabled { get; set; }

        public BiddingBoxViewModel()
        {
            SuitBids = new ObservableCollection<Grouping<int, Bid>>(Enumerable.Range(1, 7)
                .Select(level => new Grouping<int, Bid>(level, Enum.GetValues(typeof(Suit)).Cast<Suit>()
                .Select(suit => new Bid(level, suit)))));
            NonSuitBids = new ObservableCollection<Bid> { Bid.PassBid, Bid.Dbl, Bid.Rdbl };
        }
    }
}
