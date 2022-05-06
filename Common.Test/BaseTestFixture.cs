﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BiddingLogic;
using Common.Tosr;

namespace Common.Test
{
    public class BaseTestFixture
    {
        public readonly Dictionary<Fase, bool> fasesWithOffset;
        public readonly ReverseDictionaries reverseDictionaries;
        public BaseTestFixture()
        {
            fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
            reverseDictionaries = new ReverseDictionaries(fasesWithOffset, new Progress<string>());

            BidManager.SetSystemParameters(UtilTosr.ReadResource("BiddingLogic.SystemParameters.json"));
            BidManager.SetOptimizationParameters(UtilTosr.ReadResource("BiddingLogic.OptimizationParameters.json"));
        }
    }
}
