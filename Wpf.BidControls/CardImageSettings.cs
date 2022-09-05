using System;

namespace Wpf.BidControls
{
    public class CardImageSettings
    {
        public string CardImage { get; set; }
        public bool FirstCardIsAce { get; init; }
        public int TopClubs { get; init; }
        public int TopDiamonds { get; init; }
        public int TopHearts { get; init; }
        public int TopSpades { get; init; }
        public int XOffSet { get; init; }
        public int CardWidth { get; init; }
        public int CardHeight { get; init; }
        public int XCardPadding { get; init; }
        public int CardDistance { get; init; }

        private static readonly CardImageSettings DefaultCardImageSettings = new()
        {
            CardImage = "/Wpf.BidControls;component/Views/cardfaces.png",
            FirstCardIsAce = true,
            TopClubs = 0,
            TopDiamonds = 294,
            TopHearts = 196,
            TopSpades = 98,
            XOffSet = 0,
            CardWidth = 73,
            CardHeight = 97,
            XCardPadding = 0,
            CardDistance = 20
        };

        // ReSharper disable once InconsistentNaming
        private static readonly CardImageSettings BBOCardImageSettings = new()
        {
            CardImage = "/Wpf.BidControls;component/Views/cardfaces2.jpg",
            FirstCardIsAce = false,
            TopClubs = 138,
            TopDiamonds = 14,
            TopHearts = 76,
            TopSpades = 198,
            XOffSet = 14,
            CardWidth = 38,
            CardHeight = 48,
            XCardPadding = 5,
            CardDistance = 32
        };

        public static CardImageSettings GetCardImageSettings(string settings)
        {
            return settings switch
            {
                "default" => DefaultCardImageSettings,
                "bbo" => BBOCardImageSettings,
                _ => throw new NotImplementedException(),
            };
        }

    }
}
