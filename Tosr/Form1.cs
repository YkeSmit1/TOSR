using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Tosr
{
    enum Fase
    {
        Shape,
        Controls,
        Scanning
    };

    public partial class Form1 : Form
    {
        readonly BiddingBox biddingBox;
        private readonly Auction auction;
        private CardDto[] unOrderedCards;

        private readonly Dictionary<string, List<string>> handPerauction = new Dictionary<string, List<string>>();
        private string[] hands;
        private int batchSize;

        public Form1()
        {
            InitializeComponent();
            biddingBox = new BiddingBox((x, y) =>
            {
                var biddingBoxButton = (BiddingBoxButton) x;
                auction.AddBid(biddingBoxButton.bid, true);
                biddingBox.UpdateButtons(biddingBoxButton.bid, auction.currentPlayer);
            });
            biddingBox.Parent = this;
            biddingBox.Left = 40;
            biddingBox.Top = 130;
            biddingBox.Show();

            auction = new Auction
            {
                Parent = this,
                Left = 350,
                Top = 120,
                Width = 250,
                Height = 200
            };
            auction.Show();
            Shuffle();
        }

        private void buttonShuffleClick(object sender, EventArgs e)
        {
            Shuffle();
        }

        private void Shuffle()
        {
            bool HasCorrectControls(string s)
            {
                var controls = s.Count(x => x == 'A') * 2 + s.Count(x => x == 'K');
                return (!checkBox2.Checked && controls >= 1) || controls == numericUpDown2.Value;
            }

            bool HasCorrectDistribution(string s)
            {
                return !checkBox1.Checked || string.Join("", s.Split(',').Select(x => x.Length)) == textBox1.Text;
            }

            foreach (var card in Controls.OfType<Card>())
            {
                card.Hide();
            }

            string handsString;
            do
            {
                unOrderedCards = Shuffling.RandomizeDeck(13);
                var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
                handsString = GetDeckAsString(orderedCards);
            } while (!HasCorrectDistribution(handsString) || !HasCorrectControls(handsString));

            var left = 20 * 13;
            var cardDtos = unOrderedCards.OrderBy(x => x.Suit, new Card.GuiSuitComparer()).ThenBy(c => c.Face, new Card.FaceComparer()).ToArray();
            foreach (var cardDto in cardDtos) 
            {
                var card = new Card(cardDto.Face, cardDto.Suit, Back.Crosshatch, false, false)
                {
                    Left = left, Top = 20, Parent = this
                };
                card.Show();
                left -= 20;
            }
        }

        private void buttonClearAuctionClick(object sender, EventArgs e)
        {
            auction.Clear();
            auction.ReDraw();
            biddingBox.Clear();
        }

        private void GetAuctionFromRules(string handsString, bool updateUi)
        {
            auction.Clear();

            var fase = Fase.Shape;
            int lastBidId = 1;
            int bidId = int.MaxValue;
            var currentPlayer = Player.West;
            Bid currentBid = Bid.PassBid;
            int relayBidIdLastFase = 0;

            do
            {
                switch (currentPlayer)
                {
                    case Player.West:
                    case Player.East:
                        auction.AddBid(Bid.PassBid, updateUi);
                        break;
                    case Player.North:
                        currentBid = Common.NextBid(currentBid);
                        auction.AddBid(currentBid, updateUi);
                        lastBidId = Common.GetBidId(currentBid);
                        break;
                    case Player.South:
                        var bidFromRule = Pinvoke.GetBidFromRule(fase, handsString, lastBidId - relayBidIdLastFase, out var nextfase);
                        bidId = bidFromRule + relayBidIdLastFase;
                        if (bidFromRule == 0)
                        {
                            auction.AddBid(Bid.PassBid, updateUi);
                            return;
                        }
                        if (nextfase != fase)
                        {
                            relayBidIdLastFase = bidId + 1;
                            fase = nextfase;
                        }

                        currentBid = Common.GetBid(bidId);
                        auction.AddBid(currentBid, updateUi);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (currentPlayer == Player.South)
                    currentPlayer = Player.West;
                else currentPlayer++;
            }
            while (bidId != 0);
        }

        private string GetDeckAsString(IEnumerable<CardDto> orderedCards)
        {
            var listCards = orderedCards.ToList();
            var suitAsString = SuitAsString(listCards, Suit.Spades) + "," + 
                               SuitAsString(listCards, Suit.Hearts) + "," + 
                               SuitAsString(listCards, Suit.Diamonds) + "," + 
                               SuitAsString(listCards, Suit.Clubs);
            return suitAsString;
        }

        private static string SuitAsString(List<CardDto> listCards, Suit suit)
        {
            return listCards.Where(c => c.Suit == suit).Aggregate("", (x, y) => x + Common.GetFaceDescription(y.Face));
        }

        private void buttonGetAuctionClick(object sender, EventArgs e)
        {
            buttonClearAuctionClick(null, e);
            var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
            var handsString = GetDeckAsString(orderedCards);
            GetAuctionFromRules(handsString, true);
        }

        private void buttonBatchBiddingClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                listBox1.Items.Clear();
                handPerauction.Clear();

                BatchBidding();
                SaveAuctions();
            }
            catch (Exception exception)
            {
                listBox1.Items.Add(exception.Message);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void SaveAuctions()
        {
            var multiHandPerAuction = handPerauction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction));
        }

        private bool IsFreakHand(string handLength)
        {
            var handPattern = new string(handLength.OrderByDescending(y => y).ToArray());
            return handPattern == "7321" || handPattern[0] == '8' || handPattern[0] == '9' || 
                   (handPattern[0] == '7' && handPattern[1] == '5') ||
                    (handPattern[0] == '6' && handPattern[1] == '6') ||
                    (handPattern[0] == '7' && handPattern[1] == '6');
        }

        private string[] GenerateHandStrings()
        {
            hands = new string[batchSize];

            for (int i = 0; i < batchSize; ++i)
            {
                do
                {
                    unOrderedCards = Shuffling.RandomizeDeck(13);
                    var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit)
                        .ThenByDescending(c => c.Face, new Card.FaceComparer());
                    hands[i] = GetDeckAsString(orderedCards);
                } while (hands[i].Count(x => x == 'A') * 2 + hands[i].Count(x => x == 'K') < 2);
            }

            return hands;
        }

        private void BatchBidding()
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < batchSize; ++i)
            {
                GetAuctionFromRules(hands[i], false);
                var bids = auction.GetBids(Player.South);
                AddHandAndAuction(hands[i], bids.Substring(0, bids.Length - 4));
            }
            MessageBox.Show(stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        private void AddHandAndAuction(string strHand, string strAuction)
        {
            var str = string.Join("", strHand.Split(',').Select(x => x.Length));

            if (IsFreakHand(str))
                return;

            if (!handPerauction.ContainsKey(strAuction))
                handPerauction[strAuction] = new List<string>();
            if (!handPerauction[strAuction].Contains(str))
                handPerauction[strAuction].Add(str);
        }

        private void buttonGenerateHandsClick(object sender, EventArgs e)
        {
            batchSize = (int) numericUpDown1.Value;
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                hands = GenerateHandStrings();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }
    }
}
