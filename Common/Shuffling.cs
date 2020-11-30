﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Common
{
    public class Shuffling
    {
        static readonly Random random = new Random();
        public static IEnumerable<CardDto> FisherYates(int length)
        {
            var deck = Enumerable.Range(0, 52).ToArray();

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

        public static IEnumerable<CardDto> FisherYates(IEnumerable<CardDto> northHand, IEnumerable<CardDto> southHand)
        {
            var cardsNorthAndSouth = northHand.Concat(southHand).Select(card => (int)card.Suit * 13 + (int)card.Face).ToList();
            var notPickedCards = Enumerable.Range(0, 52).Where(x => !cardsNorthAndSouth.Contains(x)).ToArray();
            Debug.Assert(notPickedCards.Count() + cardsNorthAndSouth.Count == 52);

            for (var i = notPickedCards.Length - 1; i > 0; i--)
            {
                // Swap some random cards
                var k = random.Next(i + 1);
                (notPickedCards[k], notPickedCards[i]) = (notPickedCards[i], notPickedCards[k]);
            }
            var deck = cardsNorthAndSouth.Concat(notPickedCards).ToArray();

            for (int i = 0; i < 52; ++i)
            {
                yield return new CardDto() { Face = (Face)(deck[i] % 13), Suit = (Suit)(deck[i] / 13) };
            }
        }

        public static string FisherYates(ShuffleRestrictions shuffleRestrictions)
        {
            string hand;
            do
            {
                var cards = Shuffling.FisherYates(13).ToList();
                var orderedCards = cards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
                hand = Util.GetDeckAsString(orderedCards);
            }
            while
                (!shuffleRestrictions.Match(hand));
            return hand;
        }

    }
}