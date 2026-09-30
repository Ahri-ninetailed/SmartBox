using Microsoft.Extensions.Configuration;
using SmartBox.Services;
using System.Text.Json.Nodes;

namespace SmartBox.Tests.Tests
{
    public class ConfigMergerTests : IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private readonly string _bundledPath;
        private readonly string _userPath;

        public ConfigMergerTests()
        {
            Directory.CreateDirectory(_tempDir);
            _bundledPath = Path.Combine(_tempDir, "bundled.json");
            _userPath = Path.Combine(_tempDir, "user.json");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void EnsureUserConfig_NoUserConfig_CopiesBundled()
        {
            string bundled = """{ "Ai": { "ExamplesPerMeaning": 3 } }""";
            File.WriteAllText(_bundledPath, bundled);

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            Assert.Equal(bundled, File.ReadAllText(_userPath));
        }

        [Fact]
        public void EnsureUserConfig_MissingSection_AddsIt()
        {
            File.WriteAllText(_bundledPath, """{ "Ai": { "ExamplesPerMeaning": 3 }, "CambridgeDictionary": { "TimeoutSeconds": 20 } }""");
            File.WriteAllText(_userPath, """{ "Ai": { "ExamplesPerMeaning": 3 } }""");

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            Assert.Equal(20, ReadUser()["CambridgeDictionary"]!["TimeoutSeconds"]!.GetValue<int>());
        }

        [Fact]
        public void EnsureUserConfig_MissingNestedKey_AddsItToExistingSection()
        {
            File.WriteAllText(_bundledPath, """{ "Ai": { "ExamplesPerMeaning": 3, "FillIfBelow": 2 } }""");
            File.WriteAllText(_userPath, """{ "Ai": { "ExamplesPerMeaning": 5 } }""");

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            JsonObject ai = ReadUser()["Ai"]!.AsObject();
            Assert.Equal(5, ai["ExamplesPerMeaning"]!.GetValue<int>());
            Assert.Equal(2, ai["FillIfBelow"]!.GetValue<int>());
        }

        [Fact]
        public void EnsureUserConfig_ExistingValues_AreNotOverwritten()
        {
            File.WriteAllText(_bundledPath, """{ "OpenAi": { "Model": "gpt-5.4" }, "Window": { "Width": 1150 }, "New": 1 }""");
            File.WriteAllText(_userPath, """{ "OpenAi": { "Model": "my-model" }, "Window": { "Width": 1600 } }""");

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            JsonObject user = ReadUser();
            Assert.Equal("my-model", user["OpenAi"]!["Model"]!.GetValue<string>());
            Assert.Equal(1600, user["Window"]!["Width"]!.GetValue<int>());
        }

        [Fact]
        public void EnsureUserConfig_UserOnlyKeys_ArePreserved()
        {
            File.WriteAllText(_bundledPath, """{ "Ai": { "ExamplesPerMeaning": 3 }, "New": 1 }""");
            File.WriteAllText(_userPath, """{ "Ai": { "ExamplesPerMeaning": 3, "Custom": true }, "Legacy": "x" }""");

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            JsonObject user = ReadUser();
            Assert.True(user["Ai"]!["Custom"]!.GetValue<bool>());
            Assert.Equal("x", user["Legacy"]!.GetValue<string>());
        }

        [Fact]
        public void EnsureUserConfig_NothingMissing_DoesNotRewriteFile()
        {
            File.WriteAllText(_bundledPath, """{ "Ai": { "ExamplesPerMeaning": 3 } }""");
            string user = """
                {
                  // мой комментарий
                  "Ai": { "ExamplesPerMeaning": 7, },
                }
                """;
            File.WriteAllText(_userPath, user);

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            Assert.Equal(user, File.ReadAllText(_userPath));
        }

        [Fact]
        public void EnsureUserConfig_KeysDifferOnlyInCase_DoesNotAddDuplicates()
        {
            File.WriteAllText(_bundledPath, """{ "CambridgeDictionary": { "TimeoutSeconds": 20 } }""");
            string user = """{ "cambridgedictionary": { "timeoutseconds": 30 } }""";
            File.WriteAllText(_userPath, user);

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            Assert.Equal(user, File.ReadAllText(_userPath));
        }

        [Fact]
        public void EnsureUserConfig_UserValueIsNotObject_KeepsUserValue()
        {
            File.WriteAllText(_bundledPath, """{ "Window": { "Width": 1150 } }""");
            File.WriteAllText(_userPath, """{ "Window": "fullscreen" }""");

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            Assert.Equal("fullscreen", ReadUser()["Window"]!.GetValue<string>());
        }

        [Fact]
        public void EnsureUserConfig_UserConfigWithComments_IsMergedAndReadableByConfiguration()
        {
            File.WriteAllText(_bundledPath, """{ "OpenAi": { "Model": "gpt-5.4" }, "CambridgeDictionary": { "TimeoutSeconds": 20 } }""");
            File.WriteAllText(_userPath, """
                {
                  // мой комментарий
                  "openai": { "model": "my-model", },
                }
                """);

            ConfigMerger.EnsureUserConfig(_bundledPath, _userPath);

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(_tempDir)
                .AddJsonFile("user.json", optional: false)
                .Build();
            Assert.Equal("my-model", config["OpenAi:Model"]);
            Assert.Equal(20, config.GetValue<int>("CambridgeDictionary:TimeoutSeconds"));
        }

        private JsonObject ReadUser()
        {
            return JsonNode.Parse(File.ReadAllText(_userPath))!.AsObject();
        }
    }
}
