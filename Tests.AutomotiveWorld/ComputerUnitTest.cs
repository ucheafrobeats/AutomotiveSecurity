using AutomotiveWorld.Builders;
using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.AutomotiveWorld
{
    [TestClass]
    public class ComputerUnitTest
    {
        [TestMethod]
        public void MicrosoftDefenderApplicationCommand()
        {
            Computer computer = ComputerGenerator.Next();

            VehicleDto vehicleDto = new VehicleDto()
            {
                Id = "123",
                Color = Color.Green,
                Year = 1990,
                SerialNumber = "123"
            };

            computer.NextCommand(vehicleDto);

            Assert.IsTrue(computer.Events.Count > 0);
        }
    }
}