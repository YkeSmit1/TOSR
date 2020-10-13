using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Solver
{
    public class DealHands
    {
        public struct SouthHandInfo
        {
            // Shape as a four digit string, e.g. "4432"
            public string shape { get; set; }
            // The minimum number of controls
            public int minControls { get; set; }
            // The maximum number of controls 
            public int maxControls { get; set; }
            // Information from ace/king denial cuebidding
            // e.g. {1, 0, null, null} means 1 in spades, 0/2 in hearts, unknown in minors
            public int?[] DCB1 { get; set; }
            // Information from queen denial cuebidding
            // e.g. {1, 0, null, null} means queen in spades, no queen in hearts, unknown in minors
            public int?[] DCB2 { get; set; }
        }
        public static string getTCLInput(string northHandString, SouthHandInfo southHandInfo)
        {
            StringBuilder TCLInput = new StringBuilder();

            // Some basic definitions in TCL file
            TCLInput.AppendLine("defvector controls 2 1");
            TCLInput.AppendLine("holdingProc DCB1 {A K} {");
            TCLInput.AppendLine("   if {$A + $K == 1} {return 1}");
            TCLInput.AppendLine("   return 0");
            TCLInput.AppendLine("}");
            TCLInput.AppendLine("defvector DCB2 0 0 1");

            // Local variables
            string[] suits = { "spades", "hearts", "diamonds", "clubs" };

            // Stack north hand
            var northHand = string.Join(" ", northHandString.Split(","));
            TCLInput.AppendLine($"stack_hand north {{{northHand}}}");

            // Fix shape of south hand
            var southShape = southHandInfo.shape;
            TCLInput.AppendLine($"shapeclass south_shape {{ expr $s == {southShape[0]}" +
                $" && $h == {southShape[1]} && $d == {southShape[2]}" +
                $" && $c == {southShape[3]} }}");
            
            // Dealing command with shape condition
            TCLInput.Append("deal::input smartstack south south_shape");

            // Add condition for controls
            if (southHandInfo.maxControls > 0)
            {
                TCLInput.AppendLine($" controls {southHandInfo.minControls} {southHandInfo.maxControls}");
            }

            // Add condition for aces/kings DCB
            if (southHandInfo.DCB1 != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (southHandInfo.DCB1[i].ToString() == "1")
                    {
                        TCLInput.AppendLine($"smartstack::restrictHolding {suits[i]} DCB1 1 1");
                    }
                    else if (southHandInfo.DCB1[i].ToString() == "0")
                    {
                        TCLInput.AppendLine($"smartstack::restrictHolding {suits[i]} DCB1 0 0");
                    }
                }
            }

            // Add condition for queens DCB
            if (southHandInfo.DCB2 != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (southHandInfo.DCB2[i] == 1)
                    {
                        TCLInput.AppendLine($"smartstack::restrictHolding {suits[i]} DCB2 1 1");
                    }
                    else if (southHandInfo.DCB2[i] == 0)
                    {
                        TCLInput.AppendLine($"smartstack::restrictHolding {suits[i]} DCB2 0 0");
                    }
                }
            }
            return TCLInput.ToString();
        }
    }
}