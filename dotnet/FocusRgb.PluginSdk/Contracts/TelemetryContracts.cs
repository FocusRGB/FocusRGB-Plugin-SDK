using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FocusRgb.Contracts
{
    /// <summary>Names the scalar and event types a telemetry descriptor can expose.</summary>
    public static class TelemetryValueType
    {
        public const string Boolean = "boolean";
        public const string Number = "number";
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Matches the protocol wire value.")]
        public const string String = "string";
        public const string Event = "event";
    }

    /// <summary>Names whether a telemetry descriptor publishes a retained input or one-shot event.</summary>
    public static class TelemetryDescriptorKind
    {
        public const string Input = "input";
        public const string Event = "event";
    }

    /// <summary>Names the freshness states delivered to an active effect input.</summary>
    public static class TelemetryFreshness
    {
        public const string Fresh = "fresh";
        public const string Stale = "stale";
        public const string Disconnected = "disconnected";
    }

    /// <summary>Names the outcomes returned by an output plugin after an effect-input update.</summary>
    public static class EffectInputOutcome
    {
        public const string Accepted = "accepted";
        public const string Unsupported = "unsupported";
        public const string Unavailable = "unavailable";
    }

    /// <summary>Identifies one game without assigning meaning to its launcher-owned ID.</summary>
    public sealed class GameReference
    {
        public required string Launcher { get; init; }
        public required string Id { get; init; }
    }

    /// <summary>Groups telemetry descriptors using plugin-owned display text and order.</summary>
    public sealed class TelemetrySectionDescriptor
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public int Order { get; init; }
    }

    /// <summary>Describes one selectable string value without making it a host enum.</summary>
    public sealed class TelemetryChoiceDescriptor
    {
        public required string Value { get; init; }
        public required string DisplayName { get; init; }
    }

    /// <summary>Describes one game-neutral telemetry input or event.</summary>
    public sealed record TelemetryDescriptor
    {
        public required GameReference Game { get; init; }
        public required TelemetrySectionDescriptor Section { get; init; }
        public required string Kind { get; init; }
        public required string ValueType { get; init; }
        public required string Description { get; init; }
        public string? Unit { get; init; }
        public double? Minimum { get; init; }
        public double? Maximum { get; init; }
        public IReadOnlyList<TelemetryChoiceDescriptor>? Choices { get; init; }

        public static TelemetryDescriptor Input(
            GameReference game,
            TelemetrySectionDescriptor section,
            string valueType,
            string description)
        {
            ArgumentNullException.ThrowIfNull(game);
            ArgumentNullException.ThrowIfNull(section);
            TelemetryContractValidation.RequireGame(game);
            TelemetryContractValidation.RequireSection(section);
            if (valueType is not (TelemetryValueType.Boolean or TelemetryValueType.Number or TelemetryValueType.String))
                throw new ArgumentOutOfRangeException(nameof(valueType));
            TelemetryContractValidation.RequireText(description, 512, nameof(description));
            return new TelemetryDescriptor
            {
                Game = game,
                Section = section,
                Kind = TelemetryDescriptorKind.Input,
                ValueType = valueType,
                Description = description
            };
        }

        public static TelemetryDescriptor Event(
            GameReference game,
            TelemetrySectionDescriptor section,
            string description)
        {
            ArgumentNullException.ThrowIfNull(game);
            ArgumentNullException.ThrowIfNull(section);
            TelemetryContractValidation.RequireGame(game);
            TelemetryContractValidation.RequireSection(section);
            TelemetryContractValidation.RequireText(description, 512, nameof(description));
            return new TelemetryDescriptor
            {
                Game = game,
                Section = section,
                Kind = TelemetryDescriptorKind.Event,
                ValueType = TelemetryValueType.Event,
                Description = description
            };
        }

        public TelemetryDescriptor WithUnit(string unit)
        {
            if (Kind != TelemetryDescriptorKind.Input) throw new InvalidOperationException("Events do not have units.");
            TelemetryContractValidation.RequireText(unit, 32, nameof(unit));
            return this with { Unit = unit };
        }

        public TelemetryDescriptor WithRange(double minimum, double maximum)
        {
            if (ValueType != TelemetryValueType.Number)
                throw new InvalidOperationException("Only number descriptors have a range.");
            if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum > maximum)
                throw new ArgumentOutOfRangeException(nameof(minimum), "The range must contain finite ordered values.");
            return this with { Minimum = minimum, Maximum = maximum };
        }

        public TelemetryDescriptor WithChoice(string value, string displayName)
        {
            if (ValueType != TelemetryValueType.String)
                throw new InvalidOperationException("Only string descriptors have choices.");
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > 256) throw new ArgumentOutOfRangeException(nameof(value));
            TelemetryContractValidation.RequireText(displayName, 128, nameof(displayName));
            var choices = Choices ?? [];
            if (choices.Count >= 128) throw new InvalidOperationException("A descriptor supports at most 128 choices.");
            if (choices.Any(choice => string.Equals(choice.Value, value, StringComparison.Ordinal)))
                throw new ArgumentException("Choice values must be unique.", nameof(value));
            return this with
            {
                Choices =
                [
                    .. choices,
                    new TelemetryChoiceDescriptor { Value = value, DisplayName = displayName }
                ]
            };
        }

        public PluginCatalogItem ToCatalogItem(
            string resourceId,
            string displayName,
            bool available = true,
            PluginReason? reason = null)
        {
            TelemetryContractValidation.RequireResourceId(resourceId, nameof(resourceId));
            TelemetryContractValidation.RequireText(displayName, 128, nameof(displayName));
            return new PluginCatalogItem
            {
                ResourceId = resourceId,
                DisplayName = displayName,
                Available = available,
                Reason = reason,
                Telemetry = this
            };
        }
    }

    /// <summary>Identifies the game and adapter that produced telemetry.</summary>
    public sealed class TelemetrySource
    {
        public required GameReference Game { get; init; }
        public required string Adapter { get; init; }
    }

    /// <summary>Carries exactly one Boolean, finite number, or bounded string telemetry value.</summary>
    public sealed class TelemetryValue
    {
        public required string ResourceId { get; init; }
        public bool? Boolean { get; init; }
        public double? Number { get; init; }
        [JsonPropertyName("string")]
        public string? StringValue { get; init; }

        public static TelemetryValue FromBoolean(string resourceId, bool value)
        {
            TelemetryContractValidation.RequireResourceId(resourceId, nameof(resourceId));
            return new TelemetryValue { ResourceId = resourceId, Boolean = value };
        }

        public static TelemetryValue FromNumber(string resourceId, double value)
        {
            TelemetryContractValidation.RequireResourceId(resourceId, nameof(resourceId));
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            return new TelemetryValue { ResourceId = resourceId, Number = value };
        }

        public static TelemetryValue FromString(string resourceId, string value)
        {
            TelemetryContractValidation.RequireResourceId(resourceId, nameof(resourceId));
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > 1024) throw new ArgumentOutOfRangeException(nameof(value));
            return new TelemetryValue { ResourceId = resourceId, StringValue = value };
        }
    }

    /// <summary>Reports one namespaced, one-shot event and its typed fields.</summary>
    public sealed class TelemetryEvent
    {
        public required string ResourceId { get; init; }
        public required DateTimeOffset ObservedAt { get; init; }
        public IReadOnlyList<TelemetryValue> Values { get; init; } = [];

        public static TelemetryEvent Create(
            string resourceId,
            DateTimeOffset observedAt,
            IReadOnlyList<TelemetryValue> values)
        {
            TelemetryContractValidation.RequireResourceId(resourceId, nameof(resourceId));
            ArgumentNullException.ThrowIfNull(values);
            if (values.Count > 64) throw new ArgumentOutOfRangeException(nameof(values));
            foreach (var value in values) TelemetryContractValidation.RequireValue(value, nameof(values));
            var telemetryEvent = new TelemetryEvent
            {
                ResourceId = resourceId,
                ObservedAt = observedAt.ToUniversalTime(),
                Values = values
            };
            return telemetryEvent;
        }
    }

    /// <summary>Publishes a full telemetry snapshot plus one-shot events with bounded freshness.</summary>
    public sealed record GameTelemetryNotification
    {
        public required TelemetrySource Source { get; init; }
        public long Sequence { get; init; }
        public required DateTimeOffset ObservedAt { get; init; }
        public int FreshForMs { get; init; }
        public int DisconnectAfterMs { get; init; }
        public IReadOnlyList<TelemetryValue> Inputs { get; init; } = [];
        public IReadOnlyList<TelemetryEvent> Events { get; init; } = [];

        public static GameTelemetryNotification Create(
            TelemetrySource source,
            long sequence,
            DateTimeOffset observedAt,
            TimeSpan freshFor,
            TimeSpan disconnectAfter)
        {
            ArgumentNullException.ThrowIfNull(source);
            TelemetryContractValidation.RequireSource(source);
            ArgumentOutOfRangeException.ThrowIfNegative(sequence);
            var freshForMs = TelemetryContractValidation.ToMilliseconds(freshFor, 100, 60000, nameof(freshFor));
            var disconnectAfterMs = TelemetryContractValidation.ToMilliseconds(
                disconnectAfter, 101, 300000, nameof(disconnectAfter));
            if (disconnectAfterMs <= freshForMs)
                throw new ArgumentOutOfRangeException(nameof(disconnectAfter));
            return new GameTelemetryNotification
            {
                Source = source,
                Sequence = sequence,
                ObservedAt = observedAt.ToUniversalTime(),
                FreshForMs = freshForMs,
                DisconnectAfterMs = disconnectAfterMs
            };
        }

        public GameTelemetryNotification WithInput(TelemetryValue input)
        {
            ArgumentNullException.ThrowIfNull(input);
            TelemetryContractValidation.RequireValue(input, nameof(input));
            if (Inputs.Count >= 2048) throw new InvalidOperationException("A notification supports at most 2048 inputs.");
            if (Inputs.Any(existing => string.Equals(existing.ResourceId, input.ResourceId, StringComparison.Ordinal)))
                throw new ArgumentException("Input resource IDs must be unique.", nameof(input));
            return this with { Inputs = [.. Inputs, input] };
        }

        public GameTelemetryNotification WithEvent(TelemetryEvent telemetryEvent)
        {
            ArgumentNullException.ThrowIfNull(telemetryEvent);
            TelemetryContractValidation.RequireEvent(telemetryEvent, nameof(telemetryEvent));
            if (Events.Count >= 128) throw new InvalidOperationException("A notification supports at most 128 events.");
            return this with { Events = [.. Events, telemetryEvent] };
        }
    }

    /// <summary>Routes one mapped telemetry value or event to the active output binding.</summary>
    public sealed class EffectInputsRequest
    {
        public required string Profile { get; init; }
        public required string BindingId { get; init; }
        public required string MappingId { get; init; }
        public required string SourcePluginId { get; init; }
        public required TelemetrySource Source { get; init; }
        public required string SourceResourceId { get; init; }
        public long Sequence { get; init; }
        public required DateTimeOffset ReceivedAt { get; init; }
        public required string Freshness { get; init; }
        public bool ConditionMatched { get; init; }
        public required string TargetInputId { get; init; }
        public TelemetryValue? Value { get; init; }
        public TelemetryEvent? Event { get; init; }
    }

    /// <summary>Reports whether an active output accepted one mapped effect input.</summary>
    public sealed class EffectInputsResult
    {
        public required string Outcome { get; init; }
        public PluginReason? Reason { get; init; }
    }

    internal static class TelemetryContractValidation
    {
        public static void RequireResourceId(string value, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            if (value.Length is < 3 or > 128 || !IsNamespacedId(value))
                throw new ArgumentException("Telemetry resource IDs must use lowercase dot-separated segments.", parameterName);
        }

        public static int ToMilliseconds(TimeSpan value, int minimum, int maximum, string parameterName)
        {
            if (value.TotalMilliseconds < minimum || value.TotalMilliseconds > maximum)
                throw new ArgumentOutOfRangeException(parameterName);
            return checked((int)value.TotalMilliseconds);
        }

        public static void RequireGame(GameReference game)
        {
            ArgumentNullException.ThrowIfNull(game);
            RequireText(game.Launcher, 64, nameof(game.Launcher));
            RequireText(game.Id, 128, nameof(game.Id));
        }

        public static void RequireSection(TelemetrySectionDescriptor section)
        {
            ArgumentNullException.ThrowIfNull(section);
            RequireText(section.Id, 64, nameof(section.Id));
            RequireText(section.DisplayName, 128, nameof(section.DisplayName));
        }

        public static void RequireSource(TelemetrySource source)
        {
            ArgumentNullException.ThrowIfNull(source);
            RequireGame(source.Game);
            RequireResourceId(source.Adapter, nameof(source.Adapter));
        }

        public static void RequireText(string value, int maximumLength, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            if (value.Length > maximumLength) throw new ArgumentOutOfRangeException(parameterName);
        }

        public static void RequireValue(TelemetryValue value, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(value, parameterName);
            RequireResourceId(value.ResourceId, parameterName);
            var populated = (value.Boolean.HasValue ? 1 : 0)
                + (value.Number.HasValue ? 1 : 0)
                + (value.StringValue is not null ? 1 : 0);
            if (populated != 1) throw new ArgumentException("Telemetry values contain exactly one typed value.", parameterName);
            if (value.Number.HasValue && !double.IsFinite(value.Number.Value))
                throw new ArgumentOutOfRangeException(parameterName);
            if (value.StringValue?.Length > 1024) throw new ArgumentOutOfRangeException(parameterName);
        }

        public static void RequireEvent(TelemetryEvent telemetryEvent, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(telemetryEvent, parameterName);
            RequireResourceId(telemetryEvent.ResourceId, parameterName);
            ArgumentNullException.ThrowIfNull(telemetryEvent.Values, parameterName);
            if (telemetryEvent.Values.Count > 64) throw new ArgumentOutOfRangeException(parameterName);
            foreach (var value in telemetryEvent.Values) RequireValue(value, parameterName);
        }

        private static bool IsNamespacedId(string value)
        {
            var segments = value.Split('.');
            return segments.Length > 1 && segments.All(segment =>
                segment.Length > 0
                && segment[0] is >= 'a' and <= 'z'
                && segment.All(character => character is >= 'a' and <= 'z'
                    or >= '0' and <= '9' or '-'));
        }
    }
}
