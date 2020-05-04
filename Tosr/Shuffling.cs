using System;
using System.Drawing;

namespace Tosr
{
	/// <summary>
	/// enumeration that represents back bitmaps available in cards.dll of Windows 2K or XP 
	/// </summary>
	public enum Back
	{
		Crosshatch,
		Sky,
		Mineral,
		Fish,
		Frog,
		MoonFlower,
		Island,
		Squares,
		Magenta, 
		Moonland,
		Space,
		Lines,
		Toycars,
		X=14,
		O,
	}

	class Shuffling
    {
        #region Deck Functions					 

        /// <summary>
        /// Randonizes n cards deck for a card game. 
        /// </summary>
        /// <param name="n">number of cards needed</param>
        /// <param name="back">Bitmap of the back of all cards</param>
        /// <param name="showback">Sets if faces are hidden(true) or shown(false)</param>
        /// <param name="size">A new supplied size</param>
        /// <param name="isdefaultsize">Sets whether to ignore given size and set all to default size</param>
        /// <param name="highlighted">Sets if all cards are highlighted or not</param>
        /// <returns>an array of card games</returns>
        public static Card[] RandomizeDeck(int n, Back back, bool showback, Size size, bool isdefaultsize,
            bool highlighted)
        {
            // order 1,2,3,...(Clubs), 1,2,3,...(Diamonds), 1,2,3,...(Hearts), 1,2,3,...(Spades).
            int counter = 0;
            Card[] cards = new Card[n]; // the returned array of cards.
            bool[] used = new bool[52]; // an array of flags to tell which card is used.
            while (counter < n)
            {
                var rand1 = new Random((int) DateTime.Now.Ticks); // a random number generator. 
                var rand2 = new Random(rand1.Next(int.MaxValue)); // a random number generator. 
                var aNumber = rand2.Next(52);
                if (!used[aNumber]) // if the selected card was not used
                {
                    used[aNumber] = true; // set it assosiated flag to true;
                    var face = aNumber % 13;
                    var suit = aNumber / 13;
                    cards[counter] = new Card((Face) face, (Suit) suit, back, highlighted, showback);
                    if (!isdefaultsize)
                    {
                        cards[counter].Size = new Size(size.Width, size.Height);
                    }

                    counter++;
                }
            }

            return cards;
        }

        /// <summary>
        /// Randomizes a full deck of 52 cards with default size, fish backgrounds with their faces hidden
        /// </summary>
        /// <returns>A full deck of cards</returns>
        public static Card[] RandomizeDeck()
        {
            return RandomizeDeck(52, Back.Fish, true, new Size(0, 0), true, false);
        }

        /// <summary>
        /// Randomizes a deck of n cards with a selected back bitmaps.
        /// </summary>
        /// <param name="n">Number of cards</param>
        /// <param name="back">Card back value</param>
        /// <returns></returns>
        public static Card[] RandomizeDeck(int n, Back back)
        {
            return RandomizeDeck(n, back, true);
        }

        /// <summary>
        /// Randomizes a deck of n cards with a selected back bitmaps, with its faces hidden or shown.
        /// </summary>
        /// <param name="n">Number of cards</param>
        /// <param name="back">Card back value</param>
        /// <param name="showback">Set whether to hide or show face</param>
        /// <returns></returns>
        public static Card[] RandomizeDeck(int n, Back back, bool showback)
        {
            return RandomizeDeck(n, back, showback, new Size(0, 0), true, false);
        }

        #endregion
    }
}