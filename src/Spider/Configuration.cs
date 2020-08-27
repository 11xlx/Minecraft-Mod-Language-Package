﻿using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Spider
{
    public class Configuration
    {
        public static Configuration Default { get; } = new Configuration()
        {
            EnabledGameVersions = new List<string> { "1.12.2" },
#if DEBUG
            ModCount = 1000,
#else
            ModCount = 1000,
#endif
            ModInfoPath = "./config/mod_info.json"
        };

        public List<string> EnabledGameVersions { get; set; }

        public int ModCount { get; set; }

        public string ModInfoPath { get; set; }
    }
}
