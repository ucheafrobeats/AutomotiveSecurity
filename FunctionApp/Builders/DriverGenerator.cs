using AutomotiveWorld.DataAccess;
using RandomNameGeneratorLibrary;
using System;

namespace AutomotiveWorld.Builders
{
    public abstract class DriverGenerator
    {
        private static readonly Random Rand = new();

        private static readonly PersonNameGenerator PersonNameGenerator = new PersonNameGenerator();

        public static DriverDto GenerateDriverDto()
        {
            string id = PinGenerator(10);
            DriverDto driverDto = new()
            {
                Id = id,
                Name = PersonNameGenerator.GenerateRandomFirstAndLastName()
            };
            return driverDto;
        }

        private static string PinGenerator(int digits)
        {
            if (digits <= 1)
            {
                throw new ArgumentException($"Invalid number of digits, digits=[{digits}]");
            }

            var _min = (int)Math.Pow(10, digits - 1);
            var _max = (int)Math.Pow(10, digits) - 1;
            return Rand.Next(_min, _max).ToString();
        }
    }
}
