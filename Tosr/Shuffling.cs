using System;
using System.Linq;

namespace Tosr
{
	class Shuffling
    {
        public static CardDto[] RandomizeDeck(int length)
        {
	        CardDto[] cards = new CardDto[length]; 
	        int[] deck = Enumerable.Range(0, 51).ToArray();
	        FisherYates.Shuffle(deck);

	        for (int n = 0; n < length; ++n)
	        {
		        var face = deck[n] % 13;
		        var suit = deck[n] / 13;
		        cards[n] = new CardDto() {Face = (Face) face, Suit = (Suit) suit};
	        }

	        return cards;
        }

        public static class FisherYates
        {
	        static readonly Random random = new Random();
	        public static void Shuffle(int[] deck)
	        {
		        for (int n = deck.Length - 1; n > 0; --n)
		        {
			        int k = random.Next(n+1);
			        int temp = deck[n];
			        deck[n] = deck[k];
			        deck[k] = temp;
		        }
	        }
        }
    }

	internal class CardDto
	{
		public Face Face;
		public Suit Suit;
	}
}