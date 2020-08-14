using System;
using System.Collections.Generic;
using System.Linq;

namespace Tosr
{
    class Shuffling
    {
        static readonly Random random = new Random();
        public static IEnumerable<CardDto> FisherYates(int length)
        {
            var deck = Enumerable.Range(0, 51).ToArray();

            for (var n = deck.Length - 1; n > 0; --n)
            {
                // Swap some random cards
                var k = random.Next(n + 1);
                (deck[k], deck[n]) = (deck[n], deck[k]);
            }

            // Yield top n
            for (int n = 0; n < length; ++n)
            {
                yield return new CardDto() { Face = (Face)(deck[n] % 13), Suit = (Suit)(deck[n] / 13) };
            }
        }
    }
}