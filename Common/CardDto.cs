using System.Collections.Generic;

namespace Tosr
{
    public class CardDto
    {
        public Face Face;
        public Suit Suit;
    }

    public class GuiSuitComparer : IComparer<Suit>
    {
        public int Compare(Suit x, Suit y)
        {
            int tempx = x == Suit.Diamonds ? -1 : (int)x;
            int tempy = y == Suit.Diamonds ? -1 : (int)y;
            return tempx.CompareTo(tempy);
        }
    }

    public class FaceComparer : IComparer<Face>
    {
        public int Compare(Face x, Face y)
        {
            int tempx = x == Face.Ace ? 13 : (int)x;
            int tempy = y == Face.Ace ? 13 : (int)y;
            return tempx.CompareTo(tempy);
        }
    }

}