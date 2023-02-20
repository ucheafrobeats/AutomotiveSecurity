using System;

namespace AutomotiveWorld.Builders
{
    public abstract class VersionGenerator
    {
        private static readonly Random Rand = new();

        public static double Next(int minValue = 1, int maxValue = 3)
        {
            double value = Rand.NextDouble() * (maxValue - minValue) + minValue;
            value = Math.Round(value, 1, MidpointRounding.AwayFromZero);

            return value;
        }
    }
}
