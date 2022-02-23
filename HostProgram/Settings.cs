using LogWindowUI;

using Microsoft.Extensions.Configuration;

using SharedLibrary;

using System;
using System.IO;

using System.Text.Json;

namespace HostProgram
{
    public class Settings : ISettings
    {
        /**
         *  JSON SERIALIZABLE SETTINGS FIELDS
         *  Public property has to be named same as in appSettings.json
         */

        // TODO: This is too complicated

        public string DelaySend { get; private set; } = "";
        private const string _delaySendConst = nameof(DelaySend);

        public string MinDelayReceive { get; private set; } = "";
        private const string _minDelayReceiveConst = nameof(MinDelayReceive);

        public string MaxDelayReceive { get; private set; } = "";
        private const string _maxDelayReceiveConst = nameof(MaxDelayReceive);

        public string[] WindowTitles { get; private set; }
        private const string _windowTitlesConst = nameof(WindowTitles);

        public string Port { get; private set; } = "";
        private const string _portConst = nameof(Port);

        public string ImageAmountSeed { get; set; } = "";
        private const string _imageAmountSeedConst = nameof(ImageAmountSeed);

        /**
         *  END SETTINGS
         */

        private const string _appSettingsConst = "appsettings.json";


        // Singleton instance
        private static Settings instance = null;
        public static Settings Instance => instance;

        // Initialize Settings
        private static IConfiguration config;
        public static void InitializeSettings()
        {
            if (instance != null) return;
            instance = new Settings();

            config = new ConfigurationBuilder()
                .AddJsonFile(_appSettingsConst, true, true)
                .Build();

            instance.Port = config[_portConst];
            instance.DelaySend = config[_delaySendConst];
            instance.MinDelayReceive = config[_minDelayReceiveConst];
            instance.MaxDelayReceive = config[_maxDelayReceiveConst];
            instance.WindowTitles = config[_windowTitlesConst].Split(',');
            instance.ImageAmountSeed = config[_imageAmountSeedConst];

            // Create default settings.. if some important setting are nulls
            if (string.IsNullOrEmpty(instance.Port)
                || string.IsNullOrEmpty(instance.DelaySend)
                || string.IsNullOrEmpty(instance.MinDelayReceive)
                || string.IsNullOrEmpty(instance.MaxDelayReceive)
                || string.IsNullOrEmpty(instance.ImageAmountSeed))
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server,
                    $"SETTINGS IsNullOrEmpty.. creating default settings.", LoggerStateConst.INFORMATIVE);
                // Create settings with default values
                var defaultSettings = new Settings
                {
                    DelaySend = "850",
                    MinDelayReceive = "40",
                    MaxDelayReceive = "100",
                    Port = "4000",
                    ImageAmountSeed = "3",
                    WindowTitles = new string[10]
                };

                UpdateSettings(defaultSettings);
            }
        }

        /// <summary>
        /// Update interface
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddOrUpdateAppSetting(string key, string value)
        {
            // Quick hacking
            // We call this method trough ISettings asof we only update ProcessWindowClass
            if (key != _windowTitlesConst) return;

            var updatedSettings = new Settings
            {
                DelaySend = Instance.DelaySend,
                MinDelayReceive = Instance.MinDelayReceive,
                MaxDelayReceive = Instance.MaxDelayReceive,
                Port = Instance.Port,
                ImageAmountSeed = Instance.ImageAmountSeed,
                WindowTitles = value.Split(','),
            };

            UpdateSettings(updatedSettings);
        }

        /// <summary>
        /// Used internally to update settings
        /// </summary>
        private static void UpdateSettings(Settings updatedSettings)
        {
            Instance.WindowTitles = updatedSettings.WindowTitles;
            Instance.Port = updatedSettings.Port;
            Instance.MinDelayReceive = updatedSettings.MinDelayReceive;
            Instance.MaxDelayReceive = updatedSettings.MaxDelayReceive;
            Instance.DelaySend = updatedSettings.DelaySend;
            Instance.ImageAmountSeed = updatedSettings.ImageAmountSeed;

            config[_windowTitlesConst] = string.Join(",", updatedSettings.WindowTitles);
            config[_portConst] = updatedSettings.Port;
            config[_minDelayReceiveConst] = updatedSettings.MinDelayReceive;
            config[_maxDelayReceiveConst] = updatedSettings.MaxDelayReceive;
            config[_delaySendConst] = updatedSettings.DelaySend;
            config[_imageAmountSeedConst] = updatedSettings.ImageAmountSeed;

            ParseWriteToJson(updatedSettings);
        }


        /// <summary>
        /// Write settings back to appsettings.json
        /// </summary>
        /// <param name="updatedSettings"></param>
        private static void ParseWriteToJson(Settings updatedSettings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                // Parse json file
                var filePath = Path.Combine(AppContext.BaseDirectory, _appSettingsConst);
                string jsonStr = File.ReadAllText(filePath);
                var jsonModel = JsonSerializer.Deserialize<Settings>(jsonStr, options);

                // Set writable values
                jsonModel.WindowTitles = updatedSettings.WindowTitles;
                jsonModel.Port = updatedSettings.Port;
                jsonModel.MinDelayReceive = updatedSettings.MinDelayReceive;
                jsonModel.MaxDelayReceive = updatedSettings.MaxDelayReceive;
                jsonModel.DelaySend = updatedSettings.DelaySend;
                jsonModel.ImageAmountSeed = updatedSettings.ImageAmountSeed;

                // Write json file back
                var modelJson = JsonSerializer.Serialize(jsonModel, options);
                File.WriteAllText(filePath, modelJson);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
