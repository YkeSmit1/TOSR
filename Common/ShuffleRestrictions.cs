using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public class ShuffleRestrictions
    {
        public bool restrictControls = false;
        public int minControls;
        public int maxControls;
        public bool restrictShape = false;
        public string shape;
        public bool restrictHcp = false;
        public int minHcp;
        public int maxHcp;

        private bool HasCorrectDistribution(string s)
        {
            return !restrictShape || string.Join("", s.Split(',').Select(x => x.Length)) == shape;
        }

        private bool HasCorrectControls(string s)
        {
            var controlsInHand = Util.GetControlCount(s);
            return (!restrictControls || (controlsInHand >= minControls && controlsInHand <= maxControls));
        }

        private bool HasCorrectHcp(string s)
        {
            var HcpInHand = Util.GetHcpCount(s);
            return (!restrictHcp || (HcpInHand >= minHcp && HcpInHand <= maxHcp));
        }

        public bool Match(string hand)
        {
            return HasCorrectDistribution(hand) && HasCorrectControls(hand) && HasCorrectHcp(hand);
        }

        public void SetControls(int min, int max)
        {
            minControls = min;
            maxControls = max;
            restrictControls = true;
        }

        public void SetHcp(int min, int max)
        {
            minHcp = min;
            maxHcp = max;
            restrictHcp = true;
        }
    }
}
