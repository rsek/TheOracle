﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheOracle.IronSworn;

namespace TheOracle.Core
{
    public class OracleService
    {
        public List<OracleTable> OracleList { get; set; }

        public OracleService()
        {
            OracleList = new List<OracleTable>();

            if (File.Exists("IronSworn\\oracles.json"))
            {
                var ironSworn = JsonConvert.DeserializeObject<List<OracleTable>>(File.ReadAllText("IronSworn\\oracles.json"));
                OracleList.AddRange(ironSworn);
            }
            if (File.Exists("StarForged\\StarforgedOracles.json"))
            {
                var starForged = JsonConvert.DeserializeObject<List<OracleTable>>(File.ReadAllText("StarForged\\StarforgedOracles.json"));
                OracleList.AddRange(starForged);
            }
        }

        public IOracleEntry RandomRow(string TableName, GameName game = GameName.None, Random rand = null)
        {
            if (rand == null) rand = BotRandom.Instance;
            return OracleList.Single(ot => ot.Name == TableName && (ot.Game == game || game == GameName.None)).Oracles.GetRandomRow(rand);
        }
    }
}