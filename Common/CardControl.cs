using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Forms;

namespace Common
{
    public static class CardControl
    {
        private static ResourceManager resourceManager = new ResourceManager("Common.Images", Assembly.GetExecutingAssembly());

        public static Bitmap GetFaceImageForCard(Suit suit, Face face)
        {
            var topy = suit switch
            {
                Suit.Clubs => 0,
                Suit.Diamonds => 294,
                Suit.Hearts => 196,
                Suit.Spades => 98,
                _ => throw new ArgumentException(nameof(suit)),
            };
            var topx = 73 * Convert.ToInt32(face);
            var rect = new Rectangle(topx, topy, 73, 97);
            var cardImages = (Bitmap)resourceManager.GetObject("cardfaces");
            var cropped = cardImages.Clone(rect, cardImages.PixelFormat);

            return cropped;
        }

        public static Bitmap GetBackImage()
        {
            var cardBack = (Bitmap)resourceManager.GetObject("card-back");

            return cardBack;
        }

    }
}
