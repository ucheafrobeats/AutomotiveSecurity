using AutomotiveWorld.Models;
using RandomNameGeneratorLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace AutomotiveWorld.Builders
{
    public abstract class DriverGenerator
    {
        private static readonly Random Rand = new();

        private static readonly PersonNameGenerator PersonNameGenerator = new PersonNameGenerator();

        public static DriverDto GenerateDriverDto()
        {
            string id = PinGenerator(10);
            return new()
            {
                Id = id,
                Name = PersonNameGenerator.GenerateRandomFirstAndLastName()
            };
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
