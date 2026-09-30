using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SmartBox.Services
{
    /// <summary>
    /// Поддерживает пользовательский config.json в актуальном состоянии относительно встроенного в приложение
    /// </summary>
    public static class ConfigMerger
    {
        // Microsoft.Extensions.Configuration допускает комментарии и висячие запятые, поэтому и тут их допускаем
        private static readonly JsonDocumentOptions DocumentOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        // Ключи конфигурации регистронезависимы, иначе допишем дубликат ключа и Configuration упадет при чтении
        private static readonly JsonNodeOptions NodeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// Копирует встроенный конфиг, если пользовательского нет. Иначе дописывает в пользовательский недостающие ключи, не меняя существующие значения
        /// </summary>
        public static void EnsureUserConfig(string bundledConfigPath, string userConfigPath)
        {
            if (!File.Exists(userConfigPath))
            {
                File.Copy(bundledConfigPath, userConfigPath);
                return;
            }

            JsonObject bundledConfig = ParseObject(bundledConfigPath);
            JsonObject userConfig = ParseObject(userConfigPath);

            if (AddMissingKeys(bundledConfig, userConfig))
            {
                File.WriteAllText(userConfigPath, userConfig.ToJsonString(WriteOptions));
            }
        }

        private static JsonObject ParseObject(string path)
        {
            return JsonNode.Parse(File.ReadAllText(path), NodeOptions, DocumentOptions)?.AsObject()
                ?? throw new InvalidOperationException($"{path} does not contain a JSON object");
        }

        private static bool AddMissingKeys(JsonObject source, JsonObject target)
        {
            bool isChanged = false;

            foreach (KeyValuePair<string, JsonNode?> property in source)
            {
                if (!target.TryGetPropertyValue(property.Key, out JsonNode? targetValue))
                {
                    target[property.Key] = property.Value?.DeepClone();
                    isChanged = true;
                    continue;
                }

                if (property.Value is JsonObject sourceObject && targetValue is JsonObject targetObject)
                {
                    isChanged |= AddMissingKeys(sourceObject, targetObject);
                }
            }

            return isChanged;
        }
    }
}
