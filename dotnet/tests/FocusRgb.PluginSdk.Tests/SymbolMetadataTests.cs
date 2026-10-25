using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

namespace FocusRgb.PluginSdk.Tests
{
    public sealed class SymbolMetadataTests
    {
        [Fact]
        public void ReleaseAssembly_ContainsReproduciblePortableSymbolsAndSourceLink()
        {
            var assemblyPath = typeof(PluginServer).Assembly.Location;
            using var assembly = File.OpenRead(assemblyPath);
            using var pe = new PEReader(assembly);
            Assert.Contains(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.Reproducible);

            using var pdb = File.OpenRead(Path.ChangeExtension(assemblyPath, ".pdb"));
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdb);
            var reader = provider.GetMetadataReader();
            var sourceLinkKind = new Guid("cc110556-a091-4d38-9fec-25ab9a351a6a");
            var compilationOptionsKind = new Guid("b5feec05-8cd0-4a83-96da-466284bb4bd8");
            var metadata = reader.CustomDebugInformation
                .Select(handle => reader.GetCustomDebugInformation(handle))
                .Where(item => reader.GetGuid(item.Kind) == sourceLinkKind
                    || reader.GetGuid(item.Kind) == compilationOptionsKind)
                .ToDictionary(item => reader.GetGuid(item.Kind), item => reader.GetBlobBytes(item.Value));
            // Portable PDB custom-debug-information kinds defined by Roslyn.
            var sourceLink = metadata[sourceLinkKind];
            using var document = JsonDocument.Parse(sourceLink);
            var mappings = document.RootElement.GetProperty("documents");
            Assert.Contains(mappings.EnumerateObject(), item => item.Value.GetString()!
                .StartsWith("https://raw.githubusercontent.com/FocusRGB/FocusRGB-Plugin-SDK/", StringComparison.Ordinal));
            var options = Encoding.UTF8.GetString(metadata[compilationOptionsKind]);
            Assert.Contains("compiler-version", options, StringComparison.Ordinal);
            Assert.Contains("language", options, StringComparison.Ordinal);
            Assert.NotEmpty(reader.Documents);
        }
    }
}
