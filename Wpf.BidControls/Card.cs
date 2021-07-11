using Common;

namespace Wpf.BidControls
{
    public class Card
    {
        public Suit Suit { get; set; }
        public Face Face { get; set; }
        public int Index { get; set; }
        public CardImageSettings CardImageSettings { get; set; }
    }
}
