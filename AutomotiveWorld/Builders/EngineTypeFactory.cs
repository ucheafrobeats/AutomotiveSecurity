using AutomotiveWorld.Models.Parts;

namespace AutomotiveWorld.Builders
{
    public static class EngineTypeFactory
    {
        public static EngineType FromString(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return EngineType.Unknown;
            }

            return value.ToLower().Trim() switch
            {
                "electric" => EngineType.BEV,
                "gasoline" => EngineType.ESS,
                "diesel" => EngineType.DSL,
                _ => EngineType.Unknown,
            };
        }
    }
}
