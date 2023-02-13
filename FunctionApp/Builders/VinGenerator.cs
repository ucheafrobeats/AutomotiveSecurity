using AutomotiveWorld.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reactive.Joins;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace AutomotiveWorld.Builders
{
    public class VinGenerator
    {
        private static readonly string NhtsaUrl = "https://vpic.nhtsa.dot.gov/api/vehicles/decodevin/{0}?format=json";

        private static readonly Random Rand = new();

        private static readonly Regex NhtsaPossibleValuesRegex = new(@"\((\w+):(\w+)\)", RegexOptions.Compiled);

        private static readonly string AllCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private static readonly string VinCharacters = AllCharacters.Replace("I", "").Replace("Q", "").Replace("O", "");

        private static readonly string AlphaCharacters = VinCharacters.Remove(VinCharacters.IndexOf('0'));

        private static readonly string NumericCharacters = VinCharacters.Substring(VinCharacters.IndexOf('0'));

        private static readonly string YearCharacters = VinCharacters.Replace("0", "").Replace("U", "").Replace("Z", "");

        private static char ModelYearToLetter(int modelYear) => YearCharacters[modelYear % YearCharacters.Length];

        private static readonly Dictionary<string, IList<string>> MakeToWmiDictionary = new();

        private static readonly int[] Multipliers = new int[] { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

        private static readonly IDictionary<string, string> ManufacturerMap = new Dictionary<string, string>()
        {
          { "1B3", "Dodge" },
          { "1C3", "Chrysler" },
          { "1C4", "Chrysler" },
          { "1C6", "Chrysler" },
          { "1D3", "Dodge" },
          { "1FA", "Ford Motor Company" },
          { "1FB", "Ford Motor Company" },
          { "1FC", "Ford Motor Company" },
          { "1FD", "Ford Motor Company" },
          { "1FM", "Ford Motor Company" },
          { "1FT", "Ford Motor Company" },
          { "1FU", "Freightliner" },
          { "1FV", "Freightliner" },
          { "1F9", "FWD Corp." },
          { "1G", "General Motors" },
          { "1GC", "Chevrolet Truck" },
          { "1GT", "GMC Truck" },
          { "1G1", "Chevrolet" },
          { "1G2", "Pontiac" },
          { "1G3", "Oldsmobile" },
          { "1G4", "Buick" },
          { "1G6", "Cadillac" },
          { "1G8", "Saturn" },
          { "1GM", "Pontiac" },
          { "1GY", "Cadillac" },
          { "1H", "Honda" },
          { "1HD", "Harley-Davidson" },
          { "1J4", "Jeep" },
          { "1J8", "Jeep" },
          { "1L", "Lincoln" },
          { "1ME", "Mercury" },
          { "1M1", "Mack Truck" },
          { "1M2", "Mack Truck" },
          { "1M3", "Mack Truck" },
          { "1M4", "Mack Truck" },
          { "1M9", "Mynatt Truck & Equipment" },
          { "1N", "Nissan" },
          { "1NX", "NUMMI" },
          { "1P3", "Plymouth" },
          { "1R9", "Roadrunner Hay Squeeze" },
          { "1VW", "Volkswagen" },
          { "1XK", "Kenworth" },
          { "1XP", "Peterbilt" },
          { "1YV", "Mazda (AutoAlliance International)" },
          { "1ZV", "Ford (AutoAlliance International)" },
          { "2A4", "Chrysler" },
          { "2BP", "Bombardier Recreational Products" },
          { "2B3", "Dodge" },
          { "2B7", "Dodge" },
          { "2C3", "Chrysler" },
          { "2CN", "CAMI" },
          { "2D3", "Dodge" },
          { "2FA", "Ford Motor Company" },
          { "2FB", "Ford Motor Company" },
          { "2FC", "Ford Motor Company" },
          { "2FM", "Ford Motor Company" },
          { "2FT", "Ford Motor Company" },
          { "2FU", "Freightliner" },
          { "2FV", "Freightliner" },
          { "2FZ", "Sterling" },
          { "2Gx", "General Motors" },
          { "2G1", "Chevrolet" },
          { "2G2", "Pontiac" },
          { "2G3", "Oldsmobile" },
          { "2G4", "Buick" },
          { "2G9", "Gnome Homes" },
          { "2HG", "Honda" },
          { "2HK", "Honda" },
          { "2HJ", "Honda" },
          { "2HM", "Hyundai" },
          { "2M", "Mercury" },
          { "2NV", "Nova Bus" },
          { "2P3", "Plymouth" },
          { "2T", "Toyota" },
          { "2TP", "Triple E LTD" },
          { "2V4", "Volkswagen" },
          { "2V8", "Volkswagen" },
          { "2WK", "Western Star" },
          { "2WL", "Western Star" },
          { "2WM", "Western Star" },
          { "3C4", "Chrysler" },
          { "3D3", "Dodge" },
          { "3D4", "Dodge" },
          { "3FA", "Ford Motor Company" },
          { "3FE", "Ford Motor Company" },
          { "3G", "General Motors" },
          { "3H", "Honda" },
          { "3JB", "BRP (all-terrain vehicles)" },
          { "3MD", "Mazda" },
          { "3MZ", "Mazda" },
          { "3N", "Nissan" },
          { "3P3", "Plymouth" },
          { "3VW", "Volkswagen" },
          { "4F", "Mazda" },
          { "4JG", "Mercedes-Benz" },
          { "4M", "Mercury" },
          { "4RK", "Nova Bus" },
          { "4S", "Subaru-Isuzu Automotive" },
          { "4T", "Toyota" },
          { "4T9", "Lumen Motors" },
          { "4UF", "Arctic Cat Inc." },
          { "4US", "BMW" },
          { "4UZ", "Frt-Thomas Bus" },
          { "4V1", "Volvo" },
          { "4V2", "Volvo" },
          { "4V3", "Volvo" },
          { "4V4", "Volvo" },
          { "4V5", "Volvo" },
          { "4V6", "Volvo" },
          { "4VL", "Volvo" },
          { "4VM", "Volvo" },
          { "4VZ", "Volvo" },
          { "538", "Zero Motorcycles" },
          { "5F", "Honda Alabama" },
          { "5J", "Honda Ohio" },
          { "5L", "Lincoln" },
          { "5N1", "Nissan" },
          { "5NP", "Hyundai" },
          { "5T", "Toyota - trucks" },
          { "5YJ", "Tesla, Inc." },
          { "LB2", "Geely" },
          { "7FC", "Rivian" },
          { "7PD", "Rivian" },
          { "50E", "Lucid Motors" }
        };

        private static readonly IDictionary<string, string> ElectricVehicleManufacturerMap = new Dictionary<string, string>()
        {
          { "5YJ", "Tesla, Inc." },
          { "7FC", "Rivian" },
          { "7PD", "Rivian" },
          { "50E", "Lucid Motors" }
        };

        private readonly ILogger<VinGenerator> Logger;

        private readonly HttpClient HttpClient;

        public VinGenerator(ILogger<VinGenerator> log, HttpClient httpClient)
        {
            foreach (var kvp in ManufacturerMap)
            {
                string code = kvp.Key;
                string make = kvp.Value;

                if (code.Length == 2)
                {
                    code += "9";
                }
                if (!MakeToWmiDictionary.ContainsKey(make))
                {
                    MakeToWmiDictionary[make] = new List<string>();
                }
                MakeToWmiDictionary[make].Add(code);
            }

            Logger = log;
            HttpClient = httpClient;
        }

        public async Task<Vin> Next(int yearMinValue, int yearMaxValue, bool electricVehicle = false)
        {
            Vin vin;
            int retry = 0;
            do
            {
                string make;

                if (electricVehicle)
                {
                    make = ElectricVehicleManufacturerMap.ElementAt(Rand.Next(0, ElectricVehicleManufacturerMap.Count)).Value;
                }
                else
                {
                    make = ManufacturerMap.ElementAt(Rand.Next(0, ManufacturerMap.Count)).Value;
                }

                vin = await GenerateVin(Rand.Next(yearMinValue, yearMaxValue), make, electricVehicle);
                retry++;

                Logger.LogDebug($"VinGenerator generated an incomplete Vin, retry=[{retry}]...");
            } while (!vin.IsValid());


            return vin;
        }

        public async Task<Vin> GenerateVin(int modelYear, string make, bool electricVehicle = false, string suggetedVin = null)
        {
            string vinStr = suggetedVin is null ? GetVin(modelYear, make, electricVehicle) : suggetedVin;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(string.Format(NhtsaUrl, vinStr))
            };

            using var response = await HttpClient.SendAsync(request);
            string responseStr = await response.Content.ReadAsStringAsync();

            JObject responseJObject = JObject.Parse(responseStr);
            var nhtsaMap = responseJObject["Results"]
                .Select(o => new KeyValuePair<string, string>(o["Variable"].ToString(), o["Value"].ToString()))
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.First().Value);


            Vin vin = new(vinStr, nhtsaMap);

            if (vin.TryGetValue("Possible Values", out string possibleValues))
            {
                // try resolve NHTSA suggestions to provide a real Vin
                StringBuilder newVinSb = new(vinStr);

                MatchCollection matches = NhtsaPossibleValuesRegex.Matches(possibleValues);
                foreach (Match match in matches)
                {
                    int index = Int32.Parse(match.Groups[1].Value);
                    string optionalValues = match.Groups[2].Value;

                    newVinSb[index - 1] = optionalValues.ElementAt(Rand.Next(0, optionalValues.Length));
                }

                string newVinStr = newVinSb.ToString();

                // Try fix vin one time only
                if (suggetedVin is null)
                {
                    return await GenerateVin(modelYear, make, electricVehicle, MakeValid(newVinStr));
                }
                else
                {
                    vin = new Vin(newVinStr, nhtsaMap);
                }
            }

            return vin;
        }

        private static int VinCharacterToNumber(char vinChar)
        {
            if (vinChar >= '0' && vinChar <= '9') return vinChar - 48;
            if (vinChar >= 'S' && vinChar <= 'Z') return (AllCharacters.IndexOf(vinChar) + 1) % 9 + 1;
            return AllCharacters.IndexOf(vinChar) % 9 + 1;
        }

        private static string MakeValid(string vin)
        {
            var checkDigit = vin.Select(c => VinCharacterToNumber(c)).Zip(Multipliers, (a, b) => a * b).Aggregate(0, (x, y) => x + y, a => a % 11);
            return vin.Remove(8, 1).Insert(8, checkDigit == 10 ? "X" : checkDigit.ToString());
        }

        private static string GetWmi(string make)
        {
            try
            {
                var wmiList = MakeToWmiDictionary[make];
                return wmiList[Rand.Next(wmiList.Count)];
            }
            catch (KeyNotFoundException e)
            {
                throw new ArgumentException($"'{make}' is not a valid make identifier.", nameof(make));
            }
        }

        private static string GetVehicleDescriptorSection(string wmi = null)
        {
            IEnumerable<char> values = null;

            switch (wmi)
            {
                case "5YJ": // Tesla
                    values = Enumerable.Range(4, 5).Select(
                    i =>
                    {
                        var charSet = "";
                        switch (i)
                        {
                            case 4:
                                charSet = "RSTXY3";
                                break;
                            case 5:
                                charSet = "1ABCDEFGH";
                                break;
                            case 6:
                                charSet = "12345678ABCDE";
                                break;
                            case 7:
                                charSet = "1ABCDEFHSV";
                                break;
                            case 8:
                                charSet = VinCharacters;
                                break;
                            default:
                                charSet = VinCharacters;
                                break;
                        }
                        return charSet[Rand.Next(charSet.Length)];
                    });
                    break;
                case "7FC": // Rivian
                    values = Enumerable.Range(4, 5).Select(
                    i =>
                    {
                        var charSet = "";
                        switch (i)
                        {
                            case 4:
                                charSet = "T";
                                break;
                            case 5:
                                charSet = "G";
                                break;
                            case 6:
                                charSet = "AB";
                                break;
                            case 7:
                                charSet = "A";
                                break;
                            case 8:
                                charSet = "AEL";
                                break;
                            default:
                                charSet = VinCharacters;
                                break;
                        }
                        return charSet[Rand.Next(charSet.Length)];
                    });
                    break;
                case "7PD": // Rivian
                    values = Enumerable.Range(4, 5).Select(
                    i =>
                    {
                        var charSet = "";
                        switch (i)
                        {
                            case 4:
                                charSet = "S";
                                break;
                            case 5:
                                charSet = "G";
                                break;
                            case 6:
                                charSet = "AB";
                                break;
                            case 7:
                                charSet = "AB";
                                break;
                            case 8:
                                charSet = "AEL";
                                break;
                            default:
                                charSet = VinCharacters;
                                break;
                        }
                        return charSet[Rand.Next(charSet.Length)];
                    });
                    break;
                case "50E": // Lucid Motors
                    values = Enumerable.Range(4, 5).Select(
                    i =>
                    {
                        var charSet = "";
                        switch (i)
                        {
                            case 4:
                                charSet = "a";
                                break;
                            case 5:
                                charSet = "1";
                                break;
                            case 6:
                                charSet = "D";
                                break;
                            case 7:
                                charSet = "A";
                                break;
                            case 8:
                                charSet = "A";
                                break;
                            default:
                                charSet = VinCharacters;
                                break;
                        }
                        return charSet[Rand.Next(charSet.Length)];
                    });
                    break;
                default:
                    values = Enumerable.Range(4, 5).Select(
                    i =>
                    {
                        var charSet = "";
                        switch (i)
                        {
                            case 4:
                                charSet = AlphaCharacters;
                                break;
                            case 5:
                                charSet = AlphaCharacters;
                                break;
                            case 6:
                                charSet = NumericCharacters;
                                break;
                            case 7:
                                charSet = NumericCharacters;
                                break;
                            case 8:
                                charSet = VinCharacters;
                                break;
                            default:
                                charSet = VinCharacters;
                                break;
                        }
                        return charSet[Rand.Next(charSet.Length)];
                    });
                    break;
            }

            return values.Aggregate("", (a, b) => a + b, x => x) + Rand.Next(10).ToString();
        }

        private static string GetVehicleIdentifierSection(int modelYear)
        {
            char modelYearLetter = ModelYearToLetter(modelYear);
            string serialNumber = Rand.Next(10000000).ToString().PadLeft(6, '0');
            return modelYearLetter + "A" + serialNumber;
        }

        private static string GetVin(int modelYear, string make, bool electricVehicle)
        {
            string wmi = GetWmi(make);
            string vds = electricVehicle ? GetVehicleDescriptorSection(wmi) : GetVehicleDescriptorSection();
            string vis = GetVehicleIdentifierSection(modelYear);
            return MakeValid(wmi + vds + vis);
        }
    }
}
