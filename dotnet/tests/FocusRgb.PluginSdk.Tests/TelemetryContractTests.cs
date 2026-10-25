using System.Text.Json;
using FocusRgb.Contracts;

namespace FocusRgb.PluginSdk.Tests
{
    public sealed class TelemetryContractTests
    {
        private static readonly GameReference Game = new() { Launcher = "example", Id = "sample-game" };
        private static readonly TelemetrySectionDescriptor Section = new()
        {
            Id = "status",
            DisplayName = "Status",
            Order = 0
        };
        private static readonly TelemetrySource Source = new()
        {
            Game = Game,
            Adapter = "example.synthetic"
        };

        [Fact]
        public void DescriptorHelpers_CreateValidCatalogItemsAndPresentationHints()
        {
            var numeric = TelemetryDescriptor.Input(
                    Game, Section, TelemetryValueType.Number, "A synthetic level.")
                .WithUnit("percent")
                .WithRange(0, 100)
                .ToCatalogItem("example.status.level", "Level");
            var choice = TelemetryDescriptor.Input(
                    Game, Section, TelemetryValueType.String, "A synthetic mode.")
                .WithChoice("idle", "Idle")
                .ToCatalogItem("example.status.mode", "Mode");

            Assert.Equal("telemetry", CatalogKind.Telemetry);
            Assert.Equal(100, numeric.Telemetry!.Maximum);
            Assert.Equal("percent", numeric.Telemetry.Unit);
            Assert.Equal("idle", Assert.Single(choice.Telemetry!.Choices!).Value);
        }

        [Fact]
        public void NotificationHelpers_BuildFullSnapshotsAndRejectDuplicateInputs()
        {
            var notification = GameTelemetryNotification.Create(
                    Source, 1, DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5))
                .WithInput(TelemetryValue.FromBoolean("example.status.ready", true))
                .WithEvent(TelemetryEvent.Create(
                    "example.status.changed",
                    DateTimeOffset.UnixEpoch,
                    [TelemetryValue.FromString("example.status.mode", "active")]));

            Assert.Equal(1000, notification.FreshForMs);
            Assert.Equal(5000, notification.DisconnectAfterMs);
            Assert.Throws<ArgumentException>(() => notification.WithInput(
                TelemetryValue.FromBoolean("example.status.ready", false)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameTelemetryNotification.Create(
                Source, 2, DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentException>(() => notification.WithInput(new TelemetryValue
            {
                ResourceId = "example.status.invalid",
                Boolean = true,
                Number = 1
            }));
        }

        [Fact]
        public void PluginJson_RoundTripsTypedTelemetryWithoutNullAlternatives()
        {
            var value = TelemetryValue.FromNumber("example.status.level", 42);

            var json = PluginJson.Serialize(value);
            var roundTrip = PluginJson.Deserialize<TelemetryValue>(json);
            using var document = JsonDocument.Parse(json);

            Assert.Equal(42, roundTrip.Number);
            Assert.False(document.RootElement.TryGetProperty("boolean", out _));
            Assert.False(document.RootElement.TryGetProperty("string", out _));
        }
    }
}
