using System.Windows.Forms;
using Common;

namespace Tosr
{
    public class BiddingBoxButton : Button
    {
        public Bid bid;

        public BiddingBoxButton(Bid bid)
        {
            this.bid = bid;
        }
    }
}