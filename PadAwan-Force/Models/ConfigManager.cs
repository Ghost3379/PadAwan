using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PadAwan_Force.Models
{
    public class ConfigManager
    {
        private const string ConfigFileName = "macropad_config.json";
        private readonly string _configPath;

        public ConfigManager()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        public async Task<List<Layer>> LoadLayersAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return CreateDefaultLayers();
                }

                var json = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<ConfigData>(json);
                return config?.Layers ?? CreateDefaultLayers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                return CreateDefaultLayers();
            }
        }

        public async Task SaveLayersAsync(List<Layer> layers, string displayMode = "layer", bool displayEnabled = true, int currentLayer = 1)
        {
            const int maxRetries = 3;
            const int retryDelay = 100; // milliseconds
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var config = new ConfigData
                    {
                        Version = "1.0",
                        Created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Device = "FeatherS3",
                        Display = new DisplaySettings
                        {
                            Mode = displayMode,
                            Enabled = displayEnabled
                        },
                        CurrentLayer = currentLayer,
                        Layers = layers,
                        Limits = new ConfigLimits
                        {
                            MaxLayers = layers.Count, // Dynamic based on actual layers
                            MaxButtons = 6,
                            MaxKnobs = 2
                        },
                        LastModified = DateTime.Now
                    };

                    var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    // Use a temporary file to avoid file locking issues
                    var tempPath = _configPath + ".tmp";
                    await File.WriteAllTextAsync(tempPath, json);
                    
                    // Atomic move to replace the original file
                    File.Move(tempPath, _configPath, true);
                    
                    return; // Success, exit the retry loop
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    // File is locked, wait and retry
                    await Task.Delay(retryDelay * (attempt + 1));
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving config (attempt {attempt + 1}): {ex.Message}");
                    if (attempt == maxRetries - 1)
                    {
                        throw; // Re-throw on final attempt
                    }
                    await Task.Delay(retryDelay * (attempt + 1));
                }
            }
        }

        private List<Layer> CreateDefaultLayers()
        {
            return new List<Layer>
            {
                new Layer
                {
                    Id = 1,
                    Name = "Layer 1",
                    Buttons = new Dictionary<string, ButtonConfig>
                    {
                        ["1"] = new ButtonConfig { Action = "None" },
                        ["2"] = new ButtonConfig { Action = "None" },
                        ["3"] = new ButtonConfig { Action = "None" },
                        ["4"] = new ButtonConfig { Action = "None" },
                        ["5"] = new ButtonConfig { Action = "None" },
                        ["6"] = new ButtonConfig { Action = "None" }
                    },
                    Knobs = new Dictionary<string, KnobConfig>
                    {
                        ["A"] = new KnobConfig { CcwAction = "None", CwAction = "None", PressAction = "None" },
                        ["B"] = new KnobConfig { CcwAction = "None", CwAction = "None", PressAction = "None" }
                    }
                }
            };
        }
    }

    public class ConfigData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("created")]
        public string Created { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [JsonPropertyName("device")]
        public string Device { get; set; } = "FeatherS3";

        [JsonPropertyName("display")]
        public DisplaySettings Display { get; set; } = new();

        [JsonPropertyName("currentLayer")]
        public int CurrentLayer { get; set; } = 1;

        [JsonPropertyName("layers")]
        public List<Layer> Layers { get; set; } = new();

        [JsonPropertyName("limits")]
        public ConfigLimits Limits { get; set; } = new();

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }
    }

    public class DisplaySettings
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "layer";

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public class ConfigLimits
    {
        [JsonPropertyName("maxLayers")]
        public int MaxLayers { get; set; } = 3;

        [JsonPropertyName("maxButtons")]
        public int MaxButtons { get; set; } = 6;

        [JsonPropertyName("maxKnobs")]
        public int MaxKnobs { get; set; } = 2;
    }
}
