using AutomotiveWorld.Models;

namespace AutomotiveWorld
{
    public static class Constants
    {
        public static class SimulatorProbability
        {
            public const double SpareTierPressureReduction = 0.01;
            public const double NonSpareTierPressureReduction = 0.9; // 0.05;
            public const double TierYearReduction = 0.01;
        }

        public static class Vehicle
        {
            public const int MinYear = 2018;

            public static class Psi
            {
                public static readonly PsiSpec Scooter = new(40, 50);
                public static readonly PsiSpec Car = new(32, 38);
                public static readonly PsiSpec Motor = new(28, 40);
                public static readonly PsiSpec Truck = new(32, 38);
                public static readonly PsiSpec Default = new(32, 38);
            }
        }


        public static class Assignment
        {
            public const int TotalKilometerMinValue = 50;
            public const int TotalKilometerMaxValue = 2000;
            public const int ScheduledTimeOffsetInMinutes = 1;
        }
    }
}
