using Newtonsoft.Json;
using System;

namespace AutomotiveWorld.Models.Software.Applications
{
    public class DummyApplication : Application
    {
        public DummyApplication(string name, double version, bool enabled) : base(name, version, enabled)
        {
        }

        public override void Main(object arg)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
