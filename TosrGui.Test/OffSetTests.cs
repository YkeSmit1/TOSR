﻿using Xunit;
using BiddingLogic;
using System;
using System.Collections.Generic;
using System.Text;
using Common;
using System.Linq;

namespace TosrGui.Test
{
    public class TestCaseProvider
    {
        public static IEnumerable<object[]> TestCasesNoPull()
        {
            // ♣♦♥♠
            yield return new object[] { "Simple", 0, new[]{
                "1♣", "3♦",
                "3♥", "3NT",
                "4♣", "4♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3NT4♦" };


            yield return new object[] { "Use4ClAsRelay", 0, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "4♣", "4♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣" };

            yield return new object[] { "OffSet", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3♠", "3NT" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣" };

            yield return new object[] { "OffSetUse4ClAsRelay", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♠",
                "4♣", "4♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3NT4♦" };

            // Zoom test
            yield return new object[] { "SimpelZoom", 2, new[]{
                "1♣", "3♦",
                "3♥", "3NT",
                "4♣", "4♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♦4♠" };

            yield return new object[] { "Use4ClAsRelayZoom", 2, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "4♣", "4♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣4♥" };

            // ♣♦♥♠
            yield return new object[] { "OffSetZoom", 2, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3♠", "3NT" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣4♥" };
            // ♣♦♥♠
            yield return new object[] { "OffSetUse4ClAsRelayZoom", 2, new[]{
                "1♣", "3♣",
                "3♦", "3♠",
                "4♣", "4♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♦4♠" };

            // Pull tests
        }
        public static IEnumerable<object[]> TestCasesPull3NT()
        {
            // ♣♦♥♠
            yield return new object[] { "Simple0", 0, new[]{
                "1♣", "3♦",
                "3NT", "4♦",
                "4♥", "4♠",
                "4NT", "5♣"},
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls},
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣" };

            yield return new object[] { "Simple0_", 0, new[]{
                "1♣", "3♦",
                "3NT", "4NT",
                "5♣", "5♦",
                "5♥", "5♠"},
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣" };

            yield return new object[] { "Simple1", 0, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "3NT", "4♣" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.Controls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTOneAskMin },
                "3♠4♣" };

            // ♣♦♥♠
            yield return new object[] { "OffSet0", 0, new[]{
                "1♣", "3♣",
                "3NT", "4♣",
                "4♦", "4NT",
                "5♣", "5NT"},
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "4♣5♣" };

            yield return new object[] { "OffSet1", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3NT", "4♣" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.Controls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTOneAskMin },
                "3♠4♣" };

            // Zoom test
            yield return new object[] { "SimpelZoom0", 2, new[]{
                "1♣", "3♦",
                "3NT", "4♦",
                "4♥", "4♠",
                "4NT", "5♣" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣4♥" };

            yield return new object[] { "SimpelZoom1", 2, new[]{
                "1♣", "3♦",
                "3♥", "3♠",
                "3NT", "4♣" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.Controls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTOneAskMin },
                "3♠4♣4♥" };

            // ♣♦♥♠
            yield return new object[] { "OffSetZoom0", 2, new[]{
                "1♣", "3♣",
                "3NT", "4♣",
                "4♦", "4♥",
                "4♠", "5♦"},
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTNoAsk,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣4NT" };

            yield return new object[] { "OffSetZoom1", 2, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "3NT", "4♣" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.Controls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull3NTOneAskMin },
                "3♠4♣4♥" };

            // Pull tests
        }
        public static IEnumerable<object[]> TestCasesPull4Di()
        {
            // ♣♦♥♠
            yield return new object[] { "Simple0", 0, new[]{
                "1♣", "3♦",
                "4♦", "4NT",
                "5♣", "5♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull4DiamondsNoAsk,
                Fase.Unknown, Fase.Unknown },
                "3NT4♦" };

            yield return new object[] { "Simple1", 0, new[]{
                "1♣", "3♦",
                "3♥", "3NT",
                "4♦", "4♠" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull4DiamondsOneAskMax },
                "3NT4♦" };

            yield return new object[] { "OffSet0", 0, new[]{
                "1♣", "3♣",
                "4♦", "4♠",
                "4NT", "5♣" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull4DiamondsNoAsk,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣" };

            yield return new object[] { "OffSet1", 0, new[]{
                "1♣", "3♣",
                "3♦", "3♥",
                "4♦", "4♠" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.Controls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull4DiamondsOneAskMin },
                "3♠4♣" };

            // Zoom test
            yield return new object[] { "SimpelZoom", 2, new[]{
                "1♣", "3♦",
                "4♦", "4NT",
                "5♣", "5♦" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull4DiamondsOneAskMax,
                Fase.Unknown, Fase.Unknown },
                "3♠4♦4♠" };

            yield return new object[] { "OffSetZoom", 2, new[]{
                "1♣", "3♣",
                "4♦", "4♠",
                "4NT", "5♣" },
            new[]{
                Fase.Unknown, Fase.Shape,
                Fase.Unknown, Fase.Controls,
                Fase.Unknown, Fase.ScanningControls },
            new[]{
                Fase.Unknown, Fase.Unknown,
                Fase.Unknown, Fase.Pull4DiamondsOneAskMin,
                Fase.Unknown, Fase.Unknown },
                "3♠4♣4♥" };
        }
    }

    [Collection("Sequential")]
    public class OffSetTests
    {
        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCasesNoPull), MemberType = typeof(TestCaseProvider))]
        public void GetAuctionForFaseWithOffsetNoPullTest(string testName, int zoomOffset, string[] bids, Fase[] fases, Fase[] pullFases, string expected)
        {
            TestFaseWithOffset(testName, zoomOffset, bids, fases, pullFases, expected);
        }

        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCasesPull3NT), MemberType = typeof(TestCaseProvider))]
        public void GetAuctionForFaseWithOffsetPull3NTTest(string testName, int zoomOffset, string[] bids, Fase[] fases, Fase[] pullFases, string expected)
        {
            TestFaseWithOffset(testName, zoomOffset, bids, fases, pullFases, expected);
        }

        [Theory]
        [MemberData(nameof(TestCaseProvider.TestCasesPull4Di), MemberType = typeof(TestCaseProvider))]
        public void GetAuctionForFaseWithOffsetPull4DiTest(string testName, int zoomOffset, string[] bids, Fase[] fases, Fase[] pullFases, string expected)
        {
            TestFaseWithOffset(testName, zoomOffset, bids, fases, pullFases, expected);
        }


        private static void TestFaseWithOffset(string testName, int zoomOffset, string[] bids, Fase[] fases, Fase[] pullFases, string expected)
        {
            if (testName is null)
            {
                throw new ArgumentNullException(nameof(testName));
            }
            var auction = new Auction();
            Assert.Equal(bids.Length, fases.Length);
            var enumerable = fases.Zip(pullFases, (x, y) => (x, y)).Zip(bids, (x, z) => (z, x.x, x.y));
            SetBids(auction, enumerable);
            var actual = string.Join("", BiddingInformation.GetAuctionForFaseWithOffset(auction, zoomOffset, Fase.Controls, Fase.ScanningControls));
            Assert.Equal(expected, actual);
        }

        private static void SetBids(Auction auction, IEnumerable<(string, Fase, Fase)> bidsStr)
        {
            auction.bids.Clear();
            var biddingRound = 1;
            var player = Player.North;
            var lbids = bidsStr.Select(bidstr => new Bid(int.Parse(bidstr.Item1.Substring(0, 1)), Util.GetSuit(bidstr.Item1[1..])) { fase = bidstr.Item2, pullFase = bidstr.Item3 });
            foreach (var bid in lbids)
            {
                if (player == Player.North)
                {
                    auction.bids[biddingRound] = new Dictionary<Player, Bid>(new List<KeyValuePair<Player, Bid>> { new KeyValuePair<Player, Bid>(player, bid) });
                    player = Player.South;
                }
                else
                {
                    auction.bids[biddingRound].Add(player, bid);
                    biddingRound++;
                    player = Player.North;
                }
            }
        }
    }
}