using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge.ViewModel;

public class DoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && reader.GetString() == "NaN")
        {
            return double.NaN;
        }

        return reader.GetDouble(); // JsonException thrown if reader.TokenType != JsonTokenType.Number
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            writer.WriteStringValue("null");    
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}
