using System.Text.Json.Serialization;

namespace VolumeWatcher
{
    public class VWConfig
    {
        private int _pollingRate = 100;
        private double _peakVolumeThreshold = 1.0;
        private bool _useStartWithWindows = false;
        private bool _useBeepOnMute = true;
        private bool _useAutoStartSW = false;
        private bool _useHiddenConsole = false;
        private string _deviceID = "";

        /// <summary>
        /// Defines the polling rate in milliseconds. Only values between 5 and 1000 are accepted.
        /// </summary>
        [JsonPropertyName(nameof(PollingRate))]
        public int PollingRate
        {
            get => _pollingRate;
            set
            {
                _pollingRate = Math.Clamp(value, 5, 1000);
            }
        }

        /// <summary>
        /// Defines the peak volume threshold.
        /// Only values between 0.005 and 1.0 are accepted, 1.0 representing 100% Volume or 0dB
        /// </summary>
        [JsonPropertyName(nameof(PeakVolumeThreshold))]
        public double PeakVolumeThreshold
        {
            get => _peakVolumeThreshold;
            set
            {
                _peakVolumeThreshold = Math.Clamp(value, 0.005, 1.0);
            }
        }

        /// <summary>
        /// Indicates whether the application should start with Windows
        /// </summary>
        [JsonPropertyName(nameof(UseStartWithWindows))]
        public bool UseStartWithWindows { get => _useStartWithWindows; set => _useStartWithWindows = value; }

        /// <summary>
        /// Indicates whether the application should beep when a Session got muted
        /// </summary>
        [JsonPropertyName(nameof(UseBeepOnMute))]
        public bool UseBeepOnMute { get => _useBeepOnMute; set => _useBeepOnMute = value; }

        /// <summary>
        /// Indicates whether to automatically start the SessionWatcher on program start
        /// </summary>
        [JsonPropertyName(nameof(UseAutoStartSW))]
        public bool UseAutoStartSW { get => _useAutoStartSW; set => _useAutoStartSW = value; }

        /// <summary>
        /// Indicates whether to hide the console
        /// </summary>
        [JsonPropertyName(nameof(UseHiddenConsole))]
        public bool UseHiddenConsole { get => _useHiddenConsole; set => _useHiddenConsole = value; }

        /// <summary>
        /// Represents the unique identifier of the selected device
        /// </summary>
        [JsonPropertyName(nameof(DeviceID))]
        public string DeviceID { get => _deviceID; set => _deviceID = value; }
    }
}
