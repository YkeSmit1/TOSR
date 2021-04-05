using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Common
{
    public partial class BiddingBox : UserControl
    {
        private readonly List<BiddingBoxButton> buttons = new List<BiddingBoxButton>();
        private const int defaultButtonWidth = 40;
        private const int defaultButtonHeight = 23;
        private BidType currentBidType = BidType.pass;
        private Player currentDeclarer = Player.UnKnown;

        public BiddingBox(EventHandler eventHandler)
        {
            BiddingBoxClick += eventHandler;
            InitializeComponent();
            Name = "BiddingBox";
            Size = new Size(200, 210);

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (var level in Enumerable.Range(1, 7))
                {
                    var button = new BiddingBoxButton(new Bid(level, suit))
                    {
                        Width = defaultButtonWidth,
                        Left = (4 - (int) suit) * defaultButtonWidth,
                        Top = (level - 1) * defaultButtonHeight,
                        Parent = this,
                        Text = Convert.ToString(level) + Util.GetSuitDescription(suit),
                        ForeColor = suit == Suit.Diamonds || suit == Suit.Hearts ? Color.Red : Color.Black
                    };
                    button.Click += BiddingBoxClick;
                    button.Show();
                    buttons.Add(button);
                }
            }

            AddButton(BidType.pass, 0, "pass", 100);
            AddButton(BidType.dbl, 100, "dbl", 40);
            AddButton(BidType.rdbl, 140, "rdbl", 60);
        }

        private void AddButton(BidType bidType, int buttonLeft, string buttonText, int buttonWidth)
        {
            var button = new BiddingBoxButton(new Bid(bidType))
            {
                Width = buttonWidth,
                Top = defaultButtonHeight * 7,
                Left = buttonLeft,
                Parent = this,
                Text = buttonText
            };
            button.Click += BiddingBoxClick;
            button.Show();
            buttons.Add(button);
        }

        public void UpdateButtons(Bid bid, Player auctionCurrentPlayer)
        {
            currentBidType = bid.bidType;

            switch (bid.bidType)
            {
                case BidType.bid:
                    EnableButtons(new[] {Bid.Dbl});
                    DisableButtons(new[] {Bid.Rdbl});
                    foreach (var button in buttons)
                    {
                        Bid localBid = button.bid;
                        if (localBid.bidType == BidType.bid && button.bid <= bid)
                        {
                            button.Enabled = false;
                        }
                    }
                    if (currentDeclarer == Player.UnKnown)
                    {
                        currentDeclarer = auctionCurrentPlayer;
                    }
                    break;
                case BidType.pass:
                    if (Util.IsSameTeam(auctionCurrentPlayer, currentDeclarer))
                    {
                        switch (currentBidType)
                        {
                            case BidType.bid:
                                EnableButtons(new[] {Bid.Dbl});
                                DisableButtons(new[] {Bid.Rdbl});
                                break;
                            case BidType.dbl:
                                DisableButtons(new[] {Bid.Dbl, Bid.Rdbl});
                                break;
                        }
                    }
                    else
                    {
                        switch (currentBidType)
                        {
                            case BidType.bid:
                                DisableButtons(new[] {Bid.Dbl, Bid.Rdbl});
                                break;
                            case BidType.dbl:
                                EnableButtons(new[] {Bid.Rdbl});
                                DisableButtons(new[] {Bid.Dbl});
                                break;
                        }

                    }
                    break;
                case BidType.dbl:
                    EnableButtons(new[] {Bid.Rdbl});
                    DisableButtons(new[] {Bid.Dbl});
                    break;
                case BidType.rdbl:
                    DisableButtons(new[] {Bid.Dbl, Bid.Rdbl});
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private BiddingBoxButton FindButton(Bid bid)
        {
            return buttons.Find(x => x.bid == bid);
        }

        private void EnableAllButtons()
        {
            foreach (var button in buttons)
            {
                button.Enabled = true;
            }
        }

        private void EnableButtons(IEnumerable<Bid> bids)
        {
            foreach (var bid in bids)
            {
                FindButton(bid).Enabled = true;
            }
        }

        private void DisableButtons(IEnumerable<Bid> bids)
        {
            foreach (var bid in bids)
            {
                FindButton(bid).Enabled = false;
            }
        }

        public void Clear()
        {
            EnableAllButtons();
        }


        public event EventHandler BiddingBoxClick;
    }
}
