using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Wpf.BidControls.Converters
{
    public class CardToImageRectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var card = (Card)value;
            var settings = card.CardImageSettings;
            var suit = card.Suit;
            var face = settings.FirstCardIsAce ? (int)card.Face : card.Face == Face.Ace ? 12 : (int)card.Face - 1;
            var topy = suit switch
            {
                Suit.Clubs => settings.TopClubs,
                Suit.Diamonds => settings.TopDiamonds,
                Suit.Hearts => settings.TopHearts,
                Suit.Spades => settings.TopSpades,
                _ => throw new ArgumentException(nameof(suit)),
            };
            var topx = settings.XOffSet + (settings.CardWidth * face);
            return new Int32Rect(topx, topy, settings.CardWidth - settings.XCardPadding, settings.CardHeight);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
