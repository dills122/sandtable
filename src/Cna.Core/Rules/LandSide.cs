using System.Text.Json.Serialization;

namespace Cna.Core.Rules;

[JsonConverter(typeof(JsonStringEnumConverter<LandSide>))]
public enum LandSide
{
    Axis,
    Commonwealth,
}
