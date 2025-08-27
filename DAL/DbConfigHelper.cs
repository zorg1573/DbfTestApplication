using System.IO;
using System.Text.Json;
using DbfTest.MODEL;

namespace DbfTest.DAL
{
    public static class DbConfigHelper
    {
        private static readonly string configPath = "dbconfig.json";

        public static void SaveConfig(DbConfig config)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, json);
        }

        public static DbConfig LoadConfig()
        {
            if (!File.Exists(configPath)) return null;

            string json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<DbConfig>(json);
        }

    }
}
