using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Commons.Options;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Odasoft.XBOL.ClientAPI.Schema;

public static class AppSettingsSchemaGenerator
{
    private const string SchemaEnvironmentName = "Production";

    public static JsonObject Generate(string? configurationDirectory = null)
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
        ApplyRequiredProperties(customSchema, typeof(AppSettingsSchema));
        ApplyConfigurationDefaults(
            customSchema,
            BuildConfiguration(configurationDirectory, SchemaEnvironmentName));

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
        var schema = Generate(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
        var json = schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json + Environment.NewLine);
    }

    private static IConfigurationRoot BuildConfiguration(string? configurationDirectory, string environmentName)
    {
        var builder = new ConfigurationBuilder();
        if (string.IsNullOrWhiteSpace(configurationDirectory))
        {
            return builder.Build();
        }

        return builder
            .SetBasePath(configurationDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .Build();
    }

    private static void ApplyRequiredProperties(JsonNode? schemaNode, Type clrType)
    {
        if (schemaNode is not JsonObject schemaObject)
        {
            return;
        }

        if (schemaObject["properties"] is not JsonObject properties)
        {
            return;
        }

        foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (properties[property.Name] is not JsonObject propertySchema)
            {
                continue;
            }

            if (property.GetCustomAttribute<RequiredAttribute>() is not null)
            {
                AddRequiredProperty(schemaObject, property.Name);
            }

            if (propertySchema["properties"] is JsonObject)
            {
                ApplyRequiredProperties(propertySchema, property.PropertyType);
            }
        }
    }

    private static void AddRequiredProperty(JsonObject schemaObject, string propertyName)
    {
        if (schemaObject["required"] is not JsonArray required)
        {
            required = [];
            schemaObject["required"] = required;
        }

        if (!required.Any(item => item?.GetValue<string>() == propertyName))
        {
            required.Add(propertyName);
        }
    }

    private static void ApplyConfigurationDefaults(
        JsonNode? schemaNode,
        IConfiguration configuration,
        string configurationPath = "")
    {
        if (schemaNode is not JsonObject schemaObject)
        {
            return;
        }

        if (schemaObject["properties"] is not JsonObject properties)
        {
            return;
        }

        foreach (var property in properties)
        {
            if (property.Value is not JsonObject propertySchema)
            {
                continue;
            }

            var propertyPath = string.IsNullOrEmpty(configurationPath)
                ? property.Key
                : $"{configurationPath}:{property.Key}";

            if (propertySchema["properties"] is JsonObject)
            {
                ApplyConfigurationDefaults(propertySchema, configuration, propertyPath);
                continue;
            }

            if (TryCreateDefaultNode(configuration.GetSection(propertyPath), propertySchema, out var defaultNode))
            {
                propertySchema["default"] = defaultNode;
            }
        }
    }

    private static bool TryCreateDefaultNode(
        IConfigurationSection section,
        JsonObject propertySchema,
        out JsonNode? defaultNode)
    {
        defaultNode = null;
        var schemaType = SchemaType(propertySchema);

        if (schemaType == "array")
        {
            var children = section.GetChildren().ToArray();
            if (children.Length == 0)
            {
                return false;
            }

            var itemType = SchemaType(propertySchema["items"] as JsonObject) ?? "string";
            var array = new JsonArray();
            foreach (var child in children.OrderBy(ConfigurationArrayIndex))
            {
                if (child.Value is null
                    || !TryCreateScalarDefault(child.Value, itemType, out var childNode))
                {
                    return false;
                }

                array.Add(childNode);
            }

            defaultNode = array;
            return true;
        }

        if (section.Value is null)
        {
            return false;
        }

        return TryCreateScalarDefault(section.Value, schemaType, out defaultNode);
    }

    private static int ConfigurationArrayIndex(IConfigurationSection section)
    {
        return int.TryParse(section.Key, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            ? index
            : int.MaxValue;
    }

    private static bool TryCreateScalarDefault(
        string value,
        string? schemaType,
        out JsonNode? defaultNode)
    {
        defaultNode = schemaType switch
        {
            "boolean" when bool.TryParse(value, out var boolValue) => JsonValue.Create(boolValue),
            "integer" when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue) => JsonValue.Create(longValue),
            "number" when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue) => JsonValue.Create(decimalValue),
            "string" or null => JsonValue.Create(value),
            _ => null
        };

        return defaultNode is not null;
    }

    private static string? SchemaType(JsonObject? schemaObject)
    {
        if (schemaObject is null)
        {
            return null;
        }

        if (schemaObject["type"] is JsonValue typeValue
            && typeValue.TryGetValue<string>(out var type))
        {
            return type;
        }

        if (schemaObject["type"] is JsonArray typeArray)
        {
            foreach (var item in typeArray)
            {
                if (item is JsonValue itemValue
                    && itemValue.TryGetValue<string>(out var itemType)
                    && itemType != "null")
                {
                    return itemType;
                }
            }
        }

        return schemaObject["enum"] is JsonArray ? "string" : null;
    }

    public sealed class AppSettingsSchema
    {
        [Description("Cross-origin policy registered in the HTTP pipeline.")]
        public CorsOptions? Cors { get; set; }

        [Description("Firebase Admin SDK authentication settings for Client root authentication.")]
        public GcipAuthOptionsSchema? GcipAuth { get; set; }

        [Description("Ticketing API client settings.")]
        public TicketingClientOptions? TicketingClient { get; set; }

        [Description("Fuzzy search matching parameters.")]
        public SearchSettings? SearchSettings { get; set; }

        [Description("Events view-tracking rate limits and deduplication.")]
        public EventsTrackingSettings? EventsTrackingSettings { get; set; }
    }

    public sealed class GcipAuthOptionsSchema
    {
        [Required]
        [Description("Firebase project ID.")]
        public string? ProjectId { get; set; }

        [Description("Path to a Firebase service account JSON file.")]
        public string? ServiceAccountJsonPath { get; set; }

        [Description("Firebase service account JSON content.")]
        public string? ServiceAccountJson { get; set; }
    }
}
