using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Commons.Options;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Odasoft.XBOL.ClientAPI.Schema;

public static class AppSettingsSchemaGenerator
{
    public static JsonObject Generate()
    {
        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, node) =>
            {
                var description = context.PropertyInfo?.AttributeProvider
                    ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .OfType<DescriptionAttribute>()
                    .FirstOrDefault()?.Description;

                if (description is not null)
                {
                    node["description"] = description;
                }

                var defaultValue = context.PropertyInfo?.AttributeProvider
                    ?.GetCustomAttributes(typeof(DefaultValueAttribute), false)
                    .OfType<DefaultValueAttribute>()
                    .FirstOrDefault()?.Value;

                if (defaultValue is not null)
                {
                    node["default"] = JsonSerializer.SerializeToNode(defaultValue);
                }

                return node;
            }
        };

        var customSchema = JsonSchemaExporter.GetJsonSchemaAsNode(
            JsonSerializerOptions.Default, typeof(AppSettingsSchema), exporterOptions);

        return new JsonObject
        {
            ["allOf"] = new JsonArray
            {
                new JsonObject
                {
                    ["$ref"] = "https://json.schemastore.org/appsettings.json"
                }
            },
            ["properties"] = customSchema["properties"]?.DeepClone(),
            ["type"] = "object"
        };
    }

    public static void GenerateAndWrite(string outputPath)
    {
        var schema = Generate();
        var json = schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }

    public sealed class AppSettingsSchema
    {
        [Description("Cross-origin policy registered in the HTTP pipeline.")]
        public CorsOptions? Cors { get; set; }

        [Description("Accounts permitted to authenticate against the Client API.")]
        public AuthenticationOptions? Authentication { get; set; }

        [Description("Ticketing API client settings.")]
        public TicketingClientOptions? TicketingClient { get; set; }

        [Description("Fuzzy search matching parameters.")]
        public SearchSettings? SearchSettings { get; set; }

        [Description("Events view-tracking rate limits and deduplication.")]
        public EventsTrackingSettings? EventsTrackingSettings { get; set; }
    }
}
