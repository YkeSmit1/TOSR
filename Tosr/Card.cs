using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Tosr
{
    /// <summary>
    /// enumeration that represents back bitmaps available in cards.dll of Windows 2K or XP 
    /// </summary>
    public enum Back
    {
        Crosshatch,
        Sky,
        Mineral,
        Fish,
        Frog,
        MoonFlower,
        Island,
        Squares,
        Magenta,
        Moonland,
        Space,
        Lines,
        Toycars,
        X = 14,
        O,
    }
    /// <summary>
    /// Summary description for UserControl1.
    /// </summary>
    public class Card : UserControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private readonly Container components = null;
        private Face face;
        private Suit suit;
        private Back back;
        private bool highlighted;
        private bool showback;

        public Card() : this(Face.Ace, Suit.Clubs, Back.Fish, false, false)
        {
        }

        /// <summary>
        /// Draws a card with the specified given proterties using 
        /// </summary>
        /// <param name="face">Number of card face</param>
        /// <param name="suit">Suit of the card</param>
        /// <param name="back">Background image</param>
        /// <param name="highlighted">Sets if it will be displayed in reverse video mode </param>
        /// <param name="showback">Sets if back of face of the card will be dislpayed</param>
        public Card(Face face, Suit suit, Back back, bool highlighted, bool showback)
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitForm call
            int width = ClientRectangle.Width,
                height = ClientRectangle.Height;
            if (!cdtInit(ref width, ref height))
            {
                throw new Exception("cannot initialize dll (\"cards.dll\"");
            }
            ResizeRedraw = true;
            this.face = face;
            this.suit = suit;
            this.back = back;
            this.highlighted = highlighted;
            this.showback = showback;
            Invalidate();

        }

        public override string ToString()
        {
            return Face + " of " + Suit;
        }

        #region CardProperties
        [Description("The number displayed on card face 1,2,3,...,queen,king"), Category("Card Properties")]
        public Face Face
        {
            get => face;
            set
            {
                face = value;
                Invalidate();
            }
        }

        [Description("The suit of the card."), Category("Card Properties")]
        public Suit Suit
        {
            get => suit;
            set
            {
                suit = value;
                Invalidate();
            }
        }

        [Description("The image of card back."), Category("Card Properties")]
        public Back Back
        {
            get => back;
            set
            {
                back = value;
                Invalidate();
            }
        }
        [Description("Sets whether to show back or face of the card."), Category("Card Properties")]
        public bool ShowBack
        {
            get => showback;
            set
            {
                showback = value;
                Invalidate();
            }
        }
        [Description("Sets whether to draw the card highlighted or normal."), Category("Card Properties")]
        public bool Highlighted
        {
            get => highlighted;
            set
            {
                highlighted = value;
                Invalidate();
            }
        }


        #endregion
        #region Setting properties functions
        public void SetShowBack()
        {
            ShowBack = true;
        }
        public void SetShowFace()
        {
            ShowBack = false;

        }
        public void Highlight()
        {
            Highlighted = true;
        }
        public void UndoHighlight()
        {
            Highlighted = false;
        }

        #endregion
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
            }

            base.Dispose(disposing);
            cdtTerm();          // to release resources used by the control
        }
        /// <summary>
        /// Here on paint is overriden to force the designer to draw cards 
        /// on the surface of the control, since the designer executes these 
        /// lines of code stored in OnPaint to obtain appearance of the 
        /// control. 
        /// </summary>
        protected override void OnPaint(PaintEventArgs pea)
        {
            // to fully understand the way cards.dll functions operate see this
            // http://freespace.virgin.net/james.brown7/tuts/cardtut.htm
            // http://freespace.virgin.net/james.brown7/tuts/cardtut2.htm
            Rectangle rect = ClientRectangle;
            Graphics grfx = pea.Graphics;
            IntPtr hdc = grfx.GetHdc();
            int card;
            int type;
            long color = Color.Transparent.ToArgb();
            if (ShowBack)
            {
                card = ((int)back) + 53;
                type = 1;
            }
            else
            {
                card = ((int)face) * 4 + ((int)suit);
                if (Highlighted)
                {
                    type = 2;
                }
                else
                {
                    type = 0;
                }
            }
            if (!cdtDrawExt(hdc, rect.X, rect.Y, rect.Width, rect.Height, card, type, color))
            {
                grfx.ReleaseHdc(hdc);
                throw new Exception("could not draw selected card");
            }
            grfx.ReleaseHdc(hdc);


            base.OnPaint(pea);
        }

        /// <summary>
        /// Here Onsize is overridden to adjust the mannar of using IsDefaultSize 
        /// property so that the designer changes size when it is set and, on the other hand
        /// if size is set to defaultsize(71X96) the designer sets the property
        /// </summary>
        /// <param name="ea"></param>
        protected override void OnSizeChanged(EventArgs ea)
        {
            if (Size != new Size(71, 95))
            {
            }
            else
            {
            }
            base.OnSizeChanged(ea);
        }


        #region Basic Card Drawing Functions from cards.dll
        /// <summary>
        /// Initializes the cards.dll library for drawing
        /// </summary>
        /// <param name="height">height of cards to be drawn</param>
        /// <param name="width">width of card to be drawn</param>
        /// <returns>returns true if successful</returns>
        [DllImport("cards.dll")]
        public static extern bool cdtInit(ref int width, ref int height);
        /// <summary>
        /// Draws a card on the surface of the given hdc at the given (x,y) pair  
        /// </summary>
        /// <param name="card">Number of card to be drawn 
        ///  If a card face is to be drawn (type is 0 or 2), then card must be a value from 0 through 51 to represent each card.
        ///  If type specifies that a card back is to be drawn (type is 1), then card must be a value from 53 to 68 (inclusive), to represent one of the 16 possible card backs.
        ///  The card faces are organised in increasing order. That is, the aces come first, then the two's and so on. In each group, the cards are ordered by suit. The order is clubs, diamons, hearts, spades. This pattern is repeated as the card values increase.
        /// </param>
        /// <param name="hdc">the handle of the device context on which the card will be drawn</param>
        /// <param name="type">Controls whether the front, the back, or the inverted front of the card is drawn.
        /// 0: front, 1: back, 2: hilite, 3: ghost, 4: remove, 5: invisible ghost, 6: X, 7: O
        /// </param>
        /// <param name="x">X position of the card upper left corner</param>
        /// <param name="y">X position of the card upper left corner</param>
        /// <param name="color"> Sets the background color for the CrossHatch card back(card = 53), which uses a pattern drawn with lines. All the other backs and fronts are bitmaps, so color has no effect.</param>
        [DllImport("cards.dll")]
        public static extern bool cdtDraw(IntPtr hdc, int x, int y, int card, int type, long color);
        /// <summary>
        /// Draws a card at (x,y) Position with the specified dx width and dy height. Images will be stretched to fit size.
        /// </summary>
        /// <param name="card">Number of card to be drawn 
        ///  If a card face is to be drawn (type is 0 or 2), then card must be a value from 0 through 51 to represent each card.
        ///  If type specifies that a card back is to be drawn (type is 1), then card must be a value from 53 to 68 (inclusive), to represent one of the 16 possible card backs.
        ///  The card faces are organised in increasing order. That is, the aces come first, then the two's and so on. In each group, the cards are ordered by suit. The order is clubs, diamons, hearts, spades. This pattern is repeated as the card values increase.
        /// </param>
        /// <param name="hdc">the handle of the device context on which the card will be drawn</param>
        /// <param name="suit">Controls whether the front, the back, or the inverted front of the card is drawn.
        /// 0: front, 1: back, 2: hilite, 3: ghost, 4: remove, 5: invisible ghost, 6: X, 7: O
        /// </param>
        /// <param name="color"> Sets the background color for the CrossHatch card back(card = 53), which uses a pattern drawn with lines. All the other backs and fronts are bitmaps, so color has no effect.</param>
        /// <param name="dx">Width of the drawn card</param>
        /// <param name="dy">Height of the drawn card</param>
        [DllImport("cards.dll")]
        public static extern bool cdtDrawExt(IntPtr hdc, int x, int y, int dx, int dy, int card, int suit, long color);
        /// <summary>
        /// This function animates the backs of cards by overlaying part of the card back with an alternative bitmap. 
        /// It creates effects: blinking lights on the robot, the sun donning 
        /// sunglasses, bats flying across the castle, and a card sliding out of a sleeve. 
        /// The function works only for cards of normal size drawn with cdtDraw.
        /// To draw each state, start with frame set to 0 and increment through until cdtAnimate returns 0(false) 
        /// </summary>
        /// <param name="hdc"></param>
        /// <param name="cardback"> A value of 
        /// CROSSHATCH 53
        ///ecbWEAVE1   54
        ///WEAVE2      55
        ///ROBOT       56
        ///FLOWERS     57
        ///VINE1       58
        ///VINE2       59
        ///FISH1       60
        ///FISH2       61
        ///SHELLS      62
        ///CASTLE      63
        ///ISLAND      64
        ///CARDHAND    65
        ///UNUSED      66
        ///THE_X       67
        ///THE_O       68 
        /// </param>
        /// <param name="frame">. </param>		
        /// <param name="x">X position of the card upper left corner</param>
        /// <param name="y">Y position of the card upper left corner</param>
        [DllImport("cards.dll")]
        public static extern bool cdtAnimate(IntPtr hdc, int cardback, int x, int y, int frame);
        /// <summary>
        ///  Cleans up the card resources from your program. It takes no parameters, and returns no values. 
        ///  It is a good idea to call this function just before your card game exits.
        /// </summary>
        [DllImport("cards.dll")]
        public static extern void cdtTerm();

        #endregion
        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // Card
            // 
            this.Name = "Card";
            this.Size = new System.Drawing.Size(71, 95);

        }

        #endregion
    }
}
