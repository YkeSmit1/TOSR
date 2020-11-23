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
using System.Text.RegularExpressions;

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

        public static Suit GetSuit(string suit)
        {
            return suit switch
            {
                "♣" => Suit.Clubs, // \u2663
                "♦" => Suit.Diamonds, // \u2666
                "♥" => Suit.Hearts, // \u2665
                "♠" => Suit.Spades, // \u2660
                "NT" => Suit.NoTrump,
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

        public static bool IsFreakHand(IEnumerable<int> handLength)
        {
            var handPattern = handLength.OrderByDescending(y => y).ToArray();
            return int.Parse(handPattern[0].ToString()) >= 8 ||
                int.Parse(handPattern[0].ToString()) + int.Parse(handPattern[1].ToString()) >= 12;
        }
        public static Dictionary<T, U> LoadAuctions<T, U>(string fileName, Func<int, Dictionary<T, U>> generateAuctions, int nrOfShortage)
        {
            var logger = LogManager.GetCurrentClassLogger();

            Dictionary<T, U> auctions;
            // Generate only if file does not exist or is older then one day
            if (File.Exists(fileName) && File.GetLastWriteTime(fileName) > DateTime.Now - TimeSpan.FromDays(1))
            {
                auctions = JsonConvert.DeserializeObject<Dictionary<T, U>>(File.ReadAllText(fileName));
            }
            else
            {
                logger.Info($"File {fileName} is too old or does not exist. File will be generated");
                auctions = generateAuctions(nrOfShortage);
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

        public static int GetHcpCount(params string[] suits)
        {
            return suits.Sum(suit => suit.Count(x => x == 'J') + suit.Count(x => x == 'Q') * 2 + suit.Count(x => x == 'K') * 3 + suit.Count(x => x == 'A') * 4);
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
            var (longestSuit, suitLength) = GetLongestSuit(northHand, southHand);
            // If we have a major fit return the major
            if (new List<Suit> { Suit.Spades, Suit.Hearts }.Contains(longestSuit))
                return (suitLength < 8) ? Suit.NoTrump : longestSuit;
            // Only wants to play a minor if we have a singleton and 9 or more trumps
            if (suitLength > 8 && (northHand.Split(',').Select(x => x.Length).Min() <= 1 || southHand.Split(',').Select(x => x.Length).Min() <= 1))
                return longestSuit;
            return Suit.NoTrump;
        }

        public static string GetHandWithOnlyControlsAs4333(string handsString, string honors)
        {
            return new string(string.Join(',', handsString.Split(',').OrderByDescending(x => x.Length).
                Select((x, index) => Regex.Replace(x, $"[^{honors}]", string.Empty).PadRight(index == 0 ? 4 : 3, 'x'))));
        }

        public static bool TryAddQuacksTillHCP(int hcp, ref string[] suits, int[] suitLength)
        {
            if (hcp <= GetHcpCount(suits))
                return true;

            int suitToAdd = 3;
            while (hcp - GetHcpCount(suits) > 1)
            {
                if (suitToAdd == 0)
                    break;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'Q';
                suitToAdd--;
            };

            suitToAdd = 3;
            while (hcp != GetHcpCount(suits))
            {
                if (suitToAdd == 0)
                    return false;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'J';

                suitToAdd--;
            };

            return hcp == GetHcpCount(suits);
        }

        public static bool TryAddJacksTillHCP(int hcp, ref string[] suits, int[] suitLength)
        {
            if (hcp <= GetHcpCount(suits))
                return true;

            int suitToAdd = 3;
            while (hcp != GetHcpCount(suits))
            {
                if (suitToAdd == 0)
                    return false;

                if (suits[suitToAdd].Length < suitLength[suitToAdd])
                    suits[suitToAdd] += 'J';

                suitToAdd--;
            };

            return hcp == GetHcpCount(suits);
        }

        public static int NrOfShortages(string hand)
        {
            return hand.Select(x => int.Parse(x.ToString())).Count(y => y <= 1);
        }

        public static int GetDDSFirst(Player declarer)
        {
            int declarerDDS = 3 - (int)declarer;
            return declarerDDS == 3 ? 0 : declarerDDS + 1;            
        }

        public static int GetDDSSuit(Suit suit)
        {
            return suit == Suit.NoTrump ? (int)suit : 3 - (int)suit;
        }

        public static int GetMostFrequentScore(List<int> scores)
        {
            return scores.GroupBy(x => x).OrderByDescending(y => y.Count()).Take(1).Select(z => z.Key).First();
        }
    }
}