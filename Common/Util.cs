using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Common
{
    public enum Player
    {
        West,
        North,
        East,
        South,
        UnKnown
    };

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
        Unknown,
        Shape,
        Controls,
        ScanningControls,
        ScanningOther,
        End,
        Pull3NTNoAsk,
        Pull3NTOneAsk,
        Pull3NTTwoAsks,
        Pull4DiamondsNoAsk,
        Pull4DiamondsOneAsk,
        BidGame,
    };


    public static class Util
    {
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

        public static Face GetFaceFromDescription(char c)
        {
            return c switch
            {
                'A' => Face.Ace,
                '2' => Face.Two,
                '3' => Face.Three,
                '4' => Face.Four,
                '5' => Face.Five,
                '6' => Face.Six,
                '7' => Face.Seven,
                '8' => Face.Eight,
                '9' => Face.Nine,
                'T' => Face.Ten,
                'J' => Face.Jack,
                'Q' => Face.Queen,
                'K' => Face.King,
                _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
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

        public static string SuitAsString(IEnumerable<CardDto> cards, Suit suit)
        {
            return cards.Where(c => c.Suit == suit).Aggregate("", (x, y) => x + GetFaceDescription(y.Face));
        }

        public static (Suit, int) GetLongestSuit(string northHand, string southHand)
        {
            var suitLengthNorth = northHand.Split(',').Select(x => x.Length);
            var suitLengthSouth = southHand.Split(',').Select(x => x.Length);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);
            var maxSuitLength = suitLengthNS.Max();
            var longestSuit = suitLengthNS.ToList().IndexOf(maxSuitLength);
            return ((Suit)(3 - longestSuit), maxSuitLength);
        }

        public static bool IsFreakHand(string handLength)
        {
            var handPattern = string.Concat(handLength.OrderByDescending(y => y));
            return int.Parse(handPattern[0].ToString()) >= 8 ||
                int.Parse(handPattern[0].ToString()) + int.Parse(handPattern[1].ToString()) >= 12;
        }
        public static Dictionary<string, T> LoadAuctions<T>(string fileName, Func<Dictionary<string, T>> generateAuctions)
        {
            var logger = LogManager.GetCurrentClassLogger();

            Dictionary<string, T> auctions;
            // Generate only if file does not exist or is older then one day
            if (File.Exists(fileName) && File.GetLastWriteTime(fileName) > DateTime.Now - TimeSpan.FromDays(1))
            {
                auctions = JsonConvert.DeserializeObject<Dictionary<string, T>>(File.ReadAllText(fileName));
            }
            else
            {
                logger.Info($"File {fileName} is too old or does not exist. File will be generated");
                auctions = generateAuctions();
                var sortedAuctions = auctions.ToImmutableSortedDictionary();
                var path = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrWhiteSpace(path))
                    Directory.CreateDirectory(path);
                File.WriteAllText(fileName, JsonConvert.SerializeObject(sortedAuctions, Formatting.Indented));
            }
            return auctions;
        }

        public static int GetHcpCount(string hand)
        {
            return hand.Count(x => x == 'J') + hand.Count(x => x == 'Q') * 2 + hand.Count(x => x == 'K') * 3 + hand.Count(x => x == 'A') * 4;
        }

        public static int GetControlCount(string hand)
        {
            return hand.Count(x => x == 'K') + hand.Count(x => x == 'A') * 2;
        }

        public static Suit GetTrumpSuit(string northHand, string southHand)
        {
            Debug.Assert(northHand.Length == 16);
            Debug.Assert(southHand.Length == 16);
            // TODO Use single dummy analyses to find out the best trump suit
            var (longestSuit, suitLength) = Util.GetLongestSuit(northHand, southHand);
            // If we have a major fit return the major
            if (new List<Suit> { Suit.Spades, Suit.Hearts }.Contains(longestSuit))
                return (suitLength < 8) ? Suit.NoTrump : longestSuit;
            // Only wants to play a minor if we have a singleton and more than 9 trumps
            if (suitLength > 8 && (northHand.Split(',').Select(x => x.Length).Min() <= 1 || southHand.Split(',').Select(x => x.Length).Min() <= 1))
                return longestSuit;
            return Suit.NoTrump;
        }

    }
}