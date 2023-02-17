using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomotiveWorld.Builders
{
    public abstract class LocationGenerator
    {
        private static readonly Random Rand = new();

        public static readonly IList<string> LocationDataset = new List<string>()
        {
            "United States",
            "Israel",
            "Germany",
            "Mexico"
        };

        public static string Next()
        {
            return LocationDataset.ElementAt(Rand.Next(0, LocationDataset.Count - 1));
        }
    }
}
