using AutomotiveWorld.Builders;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.AutomotiveWorld
{
    [TestClass]
    public class VinUnitTest
    {
        [TestMethod]
        public async Task TestElectricVehicle()
        {
            HttpClient client = new();
            ILogger<VinGenerator> logger = new Mock<ILogger<VinGenerator>>().Object;
            VinGenerator vinGenerator = new VinGenerator(logger, client);
            for (int i = 0; i < 10; i++)
            {
                Vin vin = await vinGenerator.Next(2018, 2020, true);
                int year = vin.ModelYear;
                Assert.IsNotNull(vin);
                Assert.AreEqual(EngineType.BEV, vin.EngineType);
            }

        }
    }
}