using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FocusRgb.Contracts;

namespace FocusRgb.PluginSdk.Tests
{
    public sealed class ProtocolDriftTests
    {
        private static readonly Dictionary<Type, string> SchemaNames = new()
        {
            [typeof(GameReference)] = "pluginGameReference",
            [typeof(TelemetrySectionDescriptor)] = "pluginTelemetrySection",
            [typeof(TelemetryChoiceDescriptor)] = "pluginTelemetryChoice",
            [typeof(TelemetryDescriptor)] = "pluginTelemetryCatalogMetadata",
            [typeof(TelemetrySource)] = "pluginTelemetrySource",
            [typeof(TelemetryValue)] = "pluginTelemetryValue",
            [typeof(TelemetryEvent)] = "pluginTelemetryEvent"
        };

        [Fact]
        public void ContractVersion_MatchesPinnedProtocol() => Assert.Equal("1.3", ContractVersion.Current);

        [Fact]
        public void ProtocolSchema_UsesPinnedReleaseId()
        {
            using var schema = ReadSchema("plugin-protocol.schema.json");

            Assert.Equal(
                "https://raw.githubusercontent.com/FocusRGB/FocusRGB/spec-v1.3.0/sdk/spec/plugin-protocol.schema.json",
                schema.RootElement.GetProperty("$id").GetString());
        }

        [Fact]
        public void ManifestSchema_UsesPinnedReleaseId()
        {
            using var schema = ReadSchema("plugin-manifest.schema.json");

            Assert.Equal(
                "https://raw.githubusercontent.com/FocusRGB/FocusRGB/spec-v1.3.0/sdk/spec/plugin-manifest.schema.json",
                schema.RootElement.GetProperty("$id").GetString());
        }

        [Fact]
        public void ProtocolSchema_MethodsMatchContractConstants()
        {
            using var schema = ReadSchema("plugin-protocol.schema.json");
            var definitions = schema.RootElement.GetProperty("$defs");
            var schemaMethods = definitions.GetProperty("request").GetProperty("properties")
                .GetProperty("method").GetProperty("enum").EnumerateArray()
                .Concat(definitions.GetProperty("notification").GetProperty("properties")
                    .GetProperty("method").GetProperty("enum").EnumerateArray())
                .Select(value => value.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            var contractMethods = typeof(PluginMethods).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToHashSet(StringComparer.Ordinal);

            Assert.True(contractMethods.SetEquals(schemaMethods));
        }

        public static TheoryData<Type, string> ProtocolShapes() => new()
        {
            { typeof(PluginHostInfo), "pluginHostInfo" },
            { typeof(InitializeParameters), "initializeParameters" },
            { typeof(InitializeResult), "initializeResult" },
            { typeof(PluginSdkInfo), "pluginSdkInfo" },
            { typeof(PluginCapability), "pluginCapability" },
            { typeof(PluginReason), "pluginReason" },
            { typeof(PluginStatusResult), "pluginStatusResult" },
            { typeof(PluginSelection), "pluginSelection" },
            { typeof(PluginResourceReference), "pluginResourceReference" },
            { typeof(CatalogListParameters), "catalogListParameters" },
            { typeof(PluginCatalogResult), "pluginCatalogResult" },
            { typeof(PluginCatalogItem), "pluginCatalogItem" },
            { typeof(PluginGameCatalogMetadata), "pluginGameCatalogMetadata" },
            { typeof(GameReference), "pluginGameReference" },
            { typeof(TelemetrySectionDescriptor), "pluginTelemetrySection" },
            { typeof(TelemetryChoiceDescriptor), "pluginTelemetryChoice" },
            { typeof(TelemetryDescriptor), "pluginTelemetryCatalogMetadata" },
            { typeof(TelemetrySource), "pluginTelemetrySource" },
            { typeof(TelemetryValue), "pluginTelemetryValue" },
            { typeof(TelemetryEvent), "pluginTelemetryEvent" },
            { typeof(GameTelemetryNotification), "gameTelemetryParameters" },
            { typeof(PluginDeviceScope), "pluginDeviceScope" },
            { typeof(BindingApplyParameters), "bindingApplyParameters" },
            { typeof(BindingApplyResult), "bindingApplyResult" },
            { typeof(BindingReleaseParameters), "bindingReleaseParameters" },
            { typeof(BindingReleaseResult), "bindingReleaseResult" },
            { typeof(EffectInputsRequest), "effectInputParameters" },
            { typeof(EffectInputsResult), "effectInputResult" },
            { typeof(CancelParameters), "cancelParameters" },
        };

        [Theory]
        [MemberData(nameof(ProtocolShapes))]
        public void ProtocolSchema_ShapesMatchPublicTypes(Type contractType, string definitionName)
        {
            using var schema = ReadSchema("plugin-protocol.schema.json");
            var definition = schema.RootElement.GetProperty("$defs").GetProperty(definitionName);
            var schemaProperties = definition.GetProperty("properties").EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
            var contractProperties = contractType.GetProperties().ToDictionary(
                property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                    ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name),
                StringComparer.Ordinal);

            Assert.True(contractProperties.Keys.ToHashSet(StringComparer.Ordinal)
                .SetEquals(schemaProperties.Keys));

            var required = definition.TryGetProperty("required", out var requiredElement)
                ? requiredElement.EnumerateArray().Select(value => value.GetString()!)
                    .ToHashSet(StringComparer.Ordinal)
                : [];
            var nullability = new NullabilityInfoContext();
            var contractRequired = contractProperties
                .Where(pair => Nullable.GetUnderlyingType(pair.Value.PropertyType) is null
                    && (pair.Value.PropertyType.IsValueType
                        || nullability.Create(pair.Value).WriteState == NullabilityState.NotNull))
                .Select(pair => pair.Key)
                .ToHashSet(StringComparer.Ordinal);
            Assert.True(contractRequired.SetEquals(required));

            foreach (var property in contractProperties)
                Assert.Equal(ExpectedSchemaType(property.Value.PropertyType), SchemaType(schemaProperties[property.Key]));
        }

        [Fact]
        public void ManifestSchema_PropertiesMatchPublicType()
        {
            using var schema = ReadSchema("plugin-manifest.schema.json");
            var schemaProperties = schema.RootElement.GetProperty("properties").EnumerateObject()
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal);
            var contractProperties = typeof(PluginManifest).GetProperties()
                .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
                .ToHashSet(StringComparer.Ordinal);

            Assert.True(contractProperties.SetEquals(schemaProperties));
        }

        [Fact]
        public void GameCatalogSchema_RequiresUniqueExecutableFileNames()
        {
            using var schema = ReadSchema("plugin-protocol.schema.json");
            var definitions = schema.RootElement.GetProperty("$defs");
            var kinds = definitions.GetProperty("catalogListParameters").GetProperty("properties")
                .GetProperty("kind").GetProperty("enum").EnumerateArray()
                .Select(item => item.GetString());
            var executables = definitions.GetProperty("pluginGameCatalogMetadata")
                .GetProperty("properties").GetProperty("executables");

            Assert.Contains("game", kinds);
            Assert.True(executables.GetProperty("uniqueItems").GetBoolean());
            Assert.Equal(1, executables.GetProperty("items").GetProperty("minLength").GetInt32());
            Assert.Equal("^[^/\\\\]+$", executables.GetProperty("items").GetProperty("pattern").GetString());
        }

        [Fact]
        public void TelemetryConstants_MatchPinnedProtocolEnums()
        {
            using var schema = ReadSchema("plugin-protocol.schema.json");
            var definitions = schema.RootElement.GetProperty("$defs");
            var kinds = definitions.GetProperty("catalogListParameters").GetProperty("properties")
                .GetProperty("kind").GetProperty("enum").EnumerateArray().Select(item => item.GetString());
            var valueTypes = definitions.GetProperty("pluginTelemetryCatalogMetadata").GetProperty("properties")
                .GetProperty("valueType").GetProperty("enum").EnumerateArray().Select(item => item.GetString());

            Assert.Equal([CatalogKind.Effect, CatalogKind.Layout, CatalogKind.Game, CatalogKind.Telemetry], kinds);
            Assert.Equal(
                [TelemetryValueType.Boolean, TelemetryValueType.Number, TelemetryValueType.String, TelemetryValueType.Event],
                valueTypes);
        }

        [Fact]
        public void TelemetrySchema_PreservesBoundsAndEffectInputIdentity()
        {
            using var schema = ReadSchema("plugin-protocol.schema.json");
            var definitions = schema.RootElement.GetProperty("$defs");
            var notification = definitions.GetProperty("gameTelemetryParameters").GetProperty("properties");
            var effectInput = definitions.GetProperty("effectInputParameters");

            Assert.Equal(2048, notification.GetProperty("inputs").GetProperty("maxItems").GetInt32());
            Assert.Equal(128, notification.GetProperty("events").GetProperty("maxItems").GetInt32());
            Assert.Contains("sourceResourceId", effectInput.GetProperty("required")
                .EnumerateArray().Select(item => item.GetString()));
            Assert.Equal(
                [TelemetryFreshness.Fresh, TelemetryFreshness.Stale, TelemetryFreshness.Disconnected],
                effectInput.GetProperty("properties").GetProperty("freshness").GetProperty("enum")
                    .EnumerateArray().Select(item => item.GetString()));
        }

        [Fact]
        public void ManifestRuntime_MatchesPinnedProtocol()
        {
            using var schema = ReadSchema("plugin-manifest.schema.json");
            var values = schema.RootElement.GetProperty("properties").GetProperty("runtime")
                .GetProperty("enum").EnumerateArray().Select(item => item.GetString()).ToArray();

            Assert.Equal(["node", "python"], values);
            Assert.Equal(typeof(string), typeof(PluginManifest).GetProperty(nameof(PluginManifest.Runtime))!.PropertyType);
            Assert.Equal(
                ["process.read", "process.start", "filesystem.read", "filesystem.write", "network.listen.loopback"],
                schema.RootElement.GetProperty("properties").GetProperty("resources")
                    .GetProperty("items").GetProperty("enum").EnumerateArray()
                    .Select(item => item.GetString()));
        }

        private static JsonDocument ReadSchema(string fileName)
        {
            var specRoot = Environment.GetEnvironmentVariable("FOCUSRGB_SPEC_ROOT");
            Assert.False(string.IsNullOrWhiteSpace(specRoot),
                "FOCUSRGB_SPEC_ROOT must point to a checkout of FocusRGB tag spec-v1.3.0.");
            var path = Path.Combine(specRoot, "sdk", "spec", fileName);
            Assert.True(File.Exists(path), $"Pinned schema was not found at {path}.");
            return JsonDocument.Parse(File.ReadAllText(path));
        }

        private static string ExpectedSchemaType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(long) || type == typeof(int)) return "integer";
            if (type == typeof(double)) return "number";
            if (type == typeof(DateTimeOffset)) return "string:date-time";
            var itemType = type.GetInterfaces().Append(type)
                .FirstOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];
            return itemType is not null
                ? "array:" + ExpectedSchemaType(itemType)
                : "ref:" + (SchemaNames.TryGetValue(type, out var schemaName)
                    ? schemaName
                    : JsonNamingPolicy.CamelCase.ConvertName(type.Name));
        }

        private static string SchemaType(JsonElement schema)
        {
            if (schema.TryGetProperty("$ref", out var reference))
                return "ref:" + reference.GetString()!.Split('/')[^1];
            var type = schema.GetProperty("type").GetString()!;
            if (type == "array") return "array:" + SchemaType(schema.GetProperty("items"));
            return schema.TryGetProperty("format", out var format) ? type + ":" + format.GetString() : type;
        }
    }
}
