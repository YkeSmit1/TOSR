using System.Windows.Forms;

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