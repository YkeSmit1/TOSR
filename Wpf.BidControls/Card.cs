using Common;

namespace Wpf.BidControls
{
    public class Card
    {
        public Suit Suit { get; init; }
        public Face Face { get; init; }
        public int Index { get; set; }
        public CardImageSettings CardImageSettings { get; init; }
    }
}
