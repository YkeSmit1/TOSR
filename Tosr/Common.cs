using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Tosr.Properties;

namespace Tosr
{
    public enum Player
    {
        West,
        North,
        East,
        South,
        UnKnown
    };

    public enum BidType
    {
        bid,
        pass,
        dbl,
        rdbl
    }

    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3,
        NoTrump = 4
    }

    public enum Face
    {
        Ace,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }

    public enum Fase
    {
        Shape,
        Controls,
        Scanning
    };


    public class Common
    {
        private readonly static Dictionary<Fase, bool> isFaseRelative = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(Resources.IsFaseRelative);
        public static bool IsFaseRelative(Fase fase)
        {
            isFaseRelative.TryGetValue(fase, out bool result);
            return result;
        }
        public static string GetSuitDescription(Suit suit)
        {
            return suit switch
            {
                Suit.Clubs => "♣", // \u2663
                Suit.Diamonds => "♦", // \u2666
                Suit.Hearts => "♥", // \u2665
                Suit.Spades => "♠", // \u2660
                Suit.NoTrump => "NT",
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null),
            };
        }

        public static char GetFaceDescription(Face face)
        {
            return face switch
            {
                Face.Ace => 'A',
                Face.Two => '2',
                Face.Three => '3',
                Face.Four => '4',
                Face.Five => '5',
                Face.Six => '6',
                Face.Seven => '7',
                Face.Eight => '8',
                Face.Nine => '9',
                Face.Ten => 'T',
                Face.Jack => 'J',
                Face.Queen => 'Q',
                Face.King => 'K',
                _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
            };
        }

        public static bool IsSameTeam(Player player1, Player player2)
        {
            return Math.Abs(player1 - player2) == 2 || 
                (player1 == player2) || 
                (player2 == Player.UnKnown || player1 == Player.UnKnown);
        }

        public static string GetDeckAsString(IEnumerable<CardDto> orderedCards)
        {
            var listCards = orderedCards.ToList();
            var suitAsString = SuitAsString(listCards, Suit.Spades) + "," +
                               SuitAsString(listCards, Suit.Hearts) + "," +
                               SuitAsString(listCards, Suit.Diamonds) + "," +
                               SuitAsString(listCards, Suit.Clubs);
            return suitAsString;
        }

        public static string SuitAsString(IEnumerable<CardDto> listCards, Suit suit)
        {
            return listCards.Where(c => c.Suit == suit).Aggregate("", (x, y) => x + GetFaceDescription(y.Face));
        }
    }
}