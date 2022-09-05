using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NLog;

namespace Solver
{
    public class MinMax
    {
        public MinMax() { }

        public MinMax(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Min { get; init; }
        public int Max { get; init; }
    }
    public class North
    {
        public string[] Hand { get; init; }
        public MinMax Hcp { get; init; }
    }
    public class South
    {
        public string[] Hand { get; init; }
        public MinMax Hcp { get; set; } = new(8, 37);
        public MinMax Controls { get; set; }
        public string[] SpecificControls { get; set; }
        public string Queens { get; init; }
        public string Shape { get; set; }
    }

    public class ShufflingDeal
    {
        public North North { get; set; } = new();
        public South South { get; set; } = new();
        private readonly Random seed = new();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string[] Execute()
        {
            GenerateTclScript();
            var boards = new List<string>();
            while (NrOfHands != boards.Count)
            {
                var batchSize = Math.Min(40, NrOfHands - boards.Count);
                boards.AddRange(RunDeal(batchSize));
            }
            Debug.Assert(NrOfHands == boards.Count);
            return boards.ToArray();
        }

        public int NrOfHands { get; init; } = 10;

        private void GenerateTclScript()
        {
            var sb = new StringBuilder();
            if (North.Hand != null)
                sb.AppendLine($"stack_hand north {{{string.Join(" ", North.Hand.Select(suit => suit == "" ? "-" : suit))}}}");
            if (South.Hand != null)
                sb.AppendLine($"stack_hand south {{{string.Join(" ", South.Hand.Select(suit => suit == "" ? "-" : suit))}}}");

            if (South.SpecificControls != null)
            {
                var topCards = !string.IsNullOrWhiteSpace(South.Queens)
                    ? South.SpecificControls.Zip(South.Queens.Select(x => x == 'Y' ? "Q" : ""), (controls, queens) => controls + queens).ToArray()
                    : South.SpecificControls;
                sb.AppendLine(
                    @$"stack_cards south {
                        (!string.IsNullOrWhiteSpace(topCards[0]) ? $"spades {topCards[0]} " : "")}{
                            (!string.IsNullOrWhiteSpace(topCards[1]) ? $"hearts {topCards[1]} " : "")}{
                                (!string.IsNullOrWhiteSpace(topCards[2]) ? $"diamonds {topCards[2]} " : "")}{
                                    (!string.IsNullOrWhiteSpace(topCards[3]) ? $"clubs {topCards[3]} " : "")}");
            }
            if (South.Shape != null)
            {
                Debug.Assert(South.Shape.Length == 4);
                sb.AppendLine($"shapeclass exact_shape {{expr $s=={South.Shape[0]} && $h=={South.Shape[1]} && $d=={South.Shape[2]} && $c=={South.Shape[3]}}}");
                sb.AppendLine(South.Controls != null
                    ? $"deal::input smartstack south exact_shape controls {South.Controls.Min} {South.Controls.Max}"
                    : $"deal::input smartstack south exact_shape");
            }
            sb.AppendLine("");
            if (!string.IsNullOrWhiteSpace(South.Queens))
            {
                sb.AppendLine("holdingProc -boolean has_queen { Q} {");
                sb.AppendLine("\treturn $Q");
                sb.AppendLine("}");
                sb.AppendLine("");
            }

            // Main
            sb.AppendLine("main {");

            if (!string.IsNullOrWhiteSpace(South.Queens))
            {
                foreach (var queen in South.Queens.Select((x, index) => (x, Index: index)).Where(queen => queen.x == 'N'))
                    sb.AppendLine($"\treject if {{[has_queen south {GetDealSuit(queen.Index)}]}}");
            }

            if (North.Hcp != null)
            {
                sb.AppendLine("\tset hcp_north [hcp north]");
                sb.AppendLine($"\treject if {{$hcp_north < {North.Hcp.Min} || $hcp_north > {North.Hcp.Max}}}");
            }

            if (South.Hcp != null)
            {
                sb.AppendLine("\tset hcp_south [hcp south]");
                sb.AppendLine($"\treject if {{$hcp_south < {South.Hcp.Min} || $hcp_south > {South.Hcp.Max}}}");
            }
            if (South.Controls != null && South.Shape == null)
            {
                sb.AppendLine("\tset controls_south [controls south]");
                sb.AppendLine($"\treject if {{$controls_south < {South.Controls.Min} || $controls_south > {South.Controls.Max}}}");
            }

            sb.AppendLine("\taccept");
            sb.AppendLine("}");
            var shuffleFilepath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "deal319", "shuffle.tcl");
            File.WriteAllText(shuffleFilepath, sb.ToString());

            static string GetDealSuit(int suitIndex) => suitIndex switch
            {
                0 => "spades",
                1 => "hearts",
                2 => "diamonds",
                3 => "clubs",
                _ => throw new ArgumentOutOfRangeException(nameof(suitIndex)),
            };

        }

        private string[] RunDeal(int nrOfHands)
        {
            var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "deal319");

            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = directory,
                FileName = Path.Combine(directory, "deal.exe"),
                UseShellExecute = false,
                Arguments = $"{nrOfHands} -s {seed.Next()} -i format/pbn -i shuffle.tcl ",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Process process = new Process { StartInfo = startInfo };
            process.Start();
            // 5 second timeout
            if (!process.WaitForExit(5 * 1000))
            {
                process.Kill();
                throw new TimeoutException("Deal.exe timed out");
            }
            if (process.ExitCode != 0)
            {
                using var errorReader = process.StandardError;
                var message = $"Dealer has incorrect exit code: {process.ExitCode}. Error:{errorReader.ReadToEnd()}";
                Logger.Warn(message);
                throw new Exception(message);
            }

            using var reader = process.StandardOutput;
            var output = reader.ReadToEnd().Split("\n");
            var boards = output.Where(x => x.StartsWith("[")).Select(x => x.Substring(7, 69)).ToArray();
            return boards;


        }
    }
}
