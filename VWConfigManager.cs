using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace VolumeWatcher
{
    public class VWConfigManager
    {
        private static readonly Lazy<VWConfigManager> _instance =
            new(() => new VWConfigManager(), LazyThreadSafetyMode.ExecutionAndPublication);


        private readonly string _exePath;
        private readonly string _configFilePath;
        private readonly VWConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        private VWConfigManager()
        {
            _exePath = Environment.ProcessPath!;
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vwconfig.json");
            _config = ReadOrInitializeConfig();
            _jsonOptions = new JsonSerializerOptions { WriteIndented = true };


            SyncRegistryAutostartKey();
        }

        /// <summary>
        /// Instance of the VolumeWatcher Configuration Manager
        /// </summary>
        public static VWConfigManager Instance => _instance.Value;


        /// <summary>
        /// Reads the configuration from the file system or initializes a new one if it doesn't exist
        /// </summary>
        /// <returns>The loaded or newly created VWConfig instance</returns>
        private VWConfig ReadOrInitializeConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                var config = new VWConfig();
                SaveConfigToFile(config);

                return config;
            }

            return ReadConfigFromFile();
        }


        /// <summary>
        /// Reads the configuration from the specified file
        /// </summary>
        /// <returns>The VWConfig instance loaded from the file</returns>
        private VWConfig ReadConfigFromFile()
        {
            try
            {
                string json = File.ReadAllText(_configFilePath);

                var configFile = JsonSerializer.Deserialize<VWConfig>(json);

                if (configFile is null)
                {
                    configFile = new VWConfig();
                    SaveConfigToFile(configFile);
                }

                return configFile;
            }
            catch (JsonException ex)
            {
                ConsoleEx.WriteErrorLine("Couldn't parse Config file, a new empty Config created", ex);

                var newConfig = new VWConfig();
                SaveConfigToFile(newConfig);
                return new VWConfig();
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleEx.WriteErrorLine("Access to the Config file denied, a new temporary empty Config will be used", ex);
                return new VWConfig();
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteErrorLine("Unexpected Exception trying to the read the Config, " +
                    "a new temporary empty Config will be used", ex);
                return new VWConfig();
            }
        }


        /// <summary>
        /// Saves the current configuration to the file system
        /// </summary>
        /// <param name="config">The VWConfig instance to be saved</param>
        private void SaveConfigToFile(VWConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteErrorLine("Error saving Config, changes will not be saved", ex);
            }
        }


        /// <summary>
        /// Synchronizes the application's autostart setting with the Windows registry
        /// </summary>
        /// <returns>True or false whether or not adding the Key was successful</returns>
        private bool SyncRegistryAutostartKey()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ConsoleEx.WriteErrorLine("Start with Windows not available on any platform other than Windows");
                return false;
            }

            // Don't add Registry Keys in Debug
            if (System.Diagnostics.Debugger.IsAttached)
            {
                if (UseStartWithWindows)
                {
                    ConsoleEx.WriteWarningLine("DEBUG: SyncRegistryAutostartKey, key would've been added");
                }
                else
                {
                    ConsoleEx.WriteWarningLine("DEBUG: SyncRegistryAutostartKey, key would've been removed");
                }

                return false;
            }

            var keyState = RegistryKeyManager.GetKeyValueState("VolumeWatcher");
            var regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (regKey is null)
            {
                ConsoleEx.WriteErrorLine("Failed to synchronize the VolumeWatcher AutoStart Setting with the Registry " +
                    "Key: \"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\" was null");

                return false;
            }

            if (UseStartWithWindows)
            {
                if (keyState == RegistryKeyValueState.NonExistant)
                {
                    RegistryKeyManager.SetKeyValue(regKey, "VolumeWatcher", _exePath);

                }
            }
            else
            {
                if (keyState == RegistryKeyValueState.IsValid)
                {
                    RegistryKeyManager.RemoveKey(regKey, "VolumeWatcher");
                }
            }

            return true;
        }

        /// <summary>
        /// Defines the polling rate in milliseconds - Only values between 5 and 1000 are accepted
        /// </summary>
        public int PollingRate
        {
            get => _config.PollingRate;
            set
            {
                _config.PollingRate = value;
                SaveConfigToFile(_config);
            }
        }


        /// <summary>
        /// Defines the peak volume threshold.
        /// Only values between 0.005 and 1.0 are accepted, 1.0 representing 100% Volume or 0dB
        /// </summary>
        public double PeakVolumeThreshold
        {
            get => _config.PeakVolumeThreshold;
            set
            {
                _config.PeakVolumeThreshold = value;
                SaveConfigToFile(_config);
            }
        }


        /// <summary>
        /// Indicates whether the application should start with Windows
        /// </summary>
        public bool UseStartWithWindows
        {
            get => _config.UseStartWithWindows;
            set
            {
                _config.UseStartWithWindows = value;
                var syncSuccessful = SyncRegistryAutostartKey();

                if (UseStartWithWindows && !syncSuccessful)
                {
                    _config.UseStartWithWindows = false;
                }

                SaveConfigToFile(_config);
            }
        }

        /// <summary>
        /// Indicates whether the application should beep when a Session got muted
        /// </summary>
        public bool UseBeepOnMute
        {
            get => _config.UseBeepOnMute;
            set
            {
                _config.UseBeepOnMute = value;
                SaveConfigToFile(_config);
            }
        }


        /// <summary>
        /// Indicates whether to automatically start the SessionWatcher on program start
        /// </summary>
        public bool UseAutoStartSW
        {
            get => _config.UseAutoStartSW;
            set
            {
                _config.UseAutoStartSW = value;
                SaveConfigToFile(_config);
            }
        }

        /// <summary>
        /// Indicates whether to hide the console
        /// </summary>
        public bool UseHiddenConsole
        {
            get => _config.UseHiddenConsole;
            set
            {
                _config.UseHiddenConsole = value;
                SaveConfigToFile(_config);
            }
        }


        /// <summary>
        /// Represents the unique identifier of the selected device
        /// </summary>
        public string DeviceID
        {
            get => _config.DeviceID;
            set
            {
                _config.DeviceID = value;
                SaveConfigToFile(_config);
            }
        }
    }
}
