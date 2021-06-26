using System.Windows.Forms;

namespace Common.Controls
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