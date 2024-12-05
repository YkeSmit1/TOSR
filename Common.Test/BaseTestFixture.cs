using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BiddingLogic;
using Common.Tosr;

namespace Common.Test
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BaseTestFixture
    {
        public readonly Dictionary<Phase, bool> phasesWithOffset;
        public readonly ReverseDictionaries reverseDictionaries;

        public BaseTestFixture()
        {
            phasesWithOffset = JsonSerializer.Deserialize<Dictionary<Phase, bool>>(File.ReadAllText("phasesWithOffset.json"));
            reverseDictionaries = new ReverseDictionaries(phasesWithOffset, new Progress<string>());

            BidManager.SetSystemParameters(UtilTosr.ReadResource("BiddingLogic.SystemParameters.json"));
            BidManager.SetOptimizationParameters(UtilTosr.ReadResource("BiddingLogic.OptimizationParameters.json"));
        }
    }
}
