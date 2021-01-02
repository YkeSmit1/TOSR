﻿using Xunit;
using Solver;
using System;
using System.Text;
using Common;
using System.Linq;

namespace TosrGui.Test
{
    public class ShufflingDealTests
    {
        [Fact()]
        public void ExecuteShuffleTest()
        {
            var shufflingDeal = new ShufflingDeal { NrOfHands = 10};
            shufflingDeal.North = new North { Hcp = new MinMax { Min = 16, Max = 37 } };
            shufflingDeal.South = new South { Hcp = new MinMax { Min = 7, Max = 37 }, Controls = new MinMax { Min = 2, Max = 12 } };

            var boards = shufflingDeal.Execute();
            Assert.Equal(10, boards.Length);
            Assert.All(boards, (board) => 
            {
                var hands = board.Split(' ');
                Assert.All(hands, (hand) => Assert.Equal(16, hand.Length));
                // Check north
                var handNorth = hands[(int)Player.North - 1];
                Assert.InRange(Util.GetHcpCount(handNorth), 16, 37);
                // Check south
                var handSouth = hands[(int)Player.South - 1];
                Assert.InRange(Util.GetHcpCount(handSouth), 7, 37);
                Assert.InRange(Util.GetControlCount(handSouth), 2, 12);
            });
        }

        [Fact]
        public void ShapeControlTest()
        {
            var expectedNorthHand = "K432.AQJ2.A32.K2";
            const string shape = "5413";
            const int controls = 3;

            var shufflingDeal = new ShufflingDeal() { NrOfHands = 10 };
            shufflingDeal.North = new North { Hand = expectedNorthHand.Split(".") };
            shufflingDeal.South = new South { Hcp = new MinMax { Min = 7, Max = 37 }, Controls = new MinMax { Min = controls, Max = controls }, Shape = shape };

            var boards = shufflingDeal.Execute();
            Assert.Equal(10, boards.Length);
            Assert.All(boards, (board) => CheckBoard(board, shufflingDeal));
        }

        [Fact]
        public void ShapeSpecificControlTest()
        {
            const string expectedNorthHand = "QJ32.AQJ2.A32.K2";
            var specificControls = new string[] { "AK", "", "K", "" };
            const string shape = "5413";
            const int controls = 4;

            var shufflingDeal = new ShufflingDeal()
            {
                NrOfHands = 10,
                North = new North { Hand = expectedNorthHand.Split(".") },
                South = new South { Controls = new MinMax { Min = controls, Max = controls }, Shape = shape, SpecificControls = specificControls }
            };

            var boards = shufflingDeal.Execute();
            Assert.Equal(10, boards.Length);
            Assert.All(boards, (board) => CheckBoard(board, shufflingDeal));
        }

        [Fact]
        public void ShapeSpecificControlWithQueensTest()
        {
            const string expectedNorthHand = "J432.AQJ2.AQ2.K2";
            const string queens = "YNNX";
            const string shape = "5413";
            const int controls = 4;
            var specificControls = new string[] { "AK", "", "K", "" };

            var shufflingDeal = new ShufflingDeal()
            {
                NrOfHands = 10,
                North = new North { Hand = expectedNorthHand.Split(".") },
                South = new South
                {
                    Controls = new MinMax { Min = controls, Max = controls },
                    Shape = shape,
                    SpecificControls = specificControls,
                    Queens = queens
                }
            };

            var boards = shufflingDeal.Execute();
            Assert.Equal(10, boards.Length);
            Assert.All(boards, (board) => CheckBoard(board, shufflingDeal));
        }

        [Fact]
        public void ShapeSpecificControlWithQueensWithHcpTest()
        {
            const string expectedNorthHand = "J432.AQJ2.AQ2.K2";
            const string queens = "YNNX";
            const string shape = "5413";
            const int controls = 4;
            var specificControls = new string[] { "AK", "", "K", "" };
            const int minHcp = 14;
            const int maxHcp = 15;

            var shufflingDeal = new ShufflingDeal()
            {
                NrOfHands = 10,
                North = new North { Hand = expectedNorthHand.Split(".") },
                South = new South
                {
                    Hcp = new MinMax { Min = minHcp, Max = maxHcp },
                    Controls = new MinMax { Min = controls, Max = controls },
                    Shape = shape,
                    SpecificControls = specificControls,
                    Queens = queens,
                }
            };

            var boards = shufflingDeal.Execute();
            Assert.Equal(10, boards.Length);
            Assert.All(boards, (board) => CheckBoard(board, shufflingDeal));
        }

        private static void CheckBoard(string board, ShufflingDeal shufflingDeal)
        {
            var hands = board.Split(' ');
            Assert.All(hands, (hand) => Assert.Equal(16, hand.Length));
            // Check north
            var actualNandNorth = hands[(int)Player.North - 1];
            Assert.Equal(string.Join(".", shufflingDeal.North.Hand), actualNandNorth);
            // Check south
            var actualNandSouth = hands[(int)Player.South - 1];
            Assert.Equal(shufflingDeal.South.Controls.Min, Util.GetControlCount(actualNandSouth));
            var suits = actualNandSouth.Split('.');
            Assert.Equal(shufflingDeal.South.Shape, string.Join("", suits.Select(suit => suit.Length.ToString())));
            if (shufflingDeal.South.Hcp != null)
                Assert.InRange(Util.GetHcpCount(hands[(int)Player.South - 1]), shufflingDeal.South.Hcp.Min, shufflingDeal.South.Hcp.Max);

            foreach (var suit in suits.Select((x, Index) => (x, Index)))
            {
                var specificControls = shufflingDeal.South.SpecificControls;
                if (specificControls != null)
                {
                    if (string.IsNullOrWhiteSpace(specificControls[suit.Index]))
                    {
                        Assert.DoesNotContain("A", suit.x);
                        Assert.DoesNotContain("K", suit.x);
                    }
                    else
                    {
                        Assert.StartsWith(specificControls[suit.Index], suit.x);
                    }
                }

                var queens = shufflingDeal.South.Queens;
                if (!string.IsNullOrWhiteSpace(queens))
                {
                    switch (queens[suit.Index])
                    {
                        case 'Y': Assert.Contains("Q", suit.x);
                            break;
                        case 'N': Assert.DoesNotContain("Q", suit.x);
                            break;
                        case 'X': // TODO not sure what to do;
                            break;
                        default:
                            throw new ArgumentException(queens[suit.Index].ToString());
                    }
                }
            }
        }
    }
}