using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Globalization;
using System.Text;
using VolumeWatcher.Helpers;

namespace VolumeWatcher
{
    public class Program
    {
        private readonly VWConfigManager _configManager;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private MMDevice? _selectedDevice;
        private AudioSessionManager? _sessionManager;

        private readonly Dictionary<string, Func<Task>> _commands;

        private bool _isSessionWatcherRunning;
        private CancellationTokenSource _sessionWatcherCTS;
        private TaskCompletionSource _sessionWatcherTCS;

        private readonly object sessionLock = new();

        public static readonly Lazy<Program> Instance = new(() => new Program());

        public Program()
        {
            _configManager = VWConfigManager.Instance;
            _deviceEnumerator = new MMDeviceEnumerator();
            _selectedDevice = PickDeviceFromConfig();

            _commands = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
            {
                ["exit"] = () => ExitApplication(),
                ["quit"] = () => ExitApplication(),
                ["hide"] = () => Task.Run(() => HideConsoleWindow()),
                ["hide_permanently"] = () => Task.Run(() => ConfirmHideConsolePermanently()),
                ["tw"] = () => Task.Run(() => ToggleStartWithWindows()),
                ["ta"] = () => Task.Run(() => ToggleAutoStart()),
                ["tb"] = () => Task.Run(() => ToggleMuteOnBeep()),
                ["tp"] = () => ToggleSessionWatcher(),
                ["ct"] = () => Task.Run(() => ChangePeakVolumeThreshold()),
                ["cr"] = () => Task.Run(() => ChangePollingRate()),
                ["cd"] = () => Task.Run(() => ChangeDevice()),
                ["sd"] = () => Task.Run(() => SaveDefaultDevice()),
                ["clear"] = () => Task.Run(() => ClearConsoleAndPrintMenu()),
                ["cls"] = () => Task.Run(() => ClearConsoleAndPrintMenu())
            };

            _isSessionWatcherRunning = _configManager.UseAutoStartSW;
            _sessionWatcherCTS = new CancellationTokenSource();
            _sessionWatcherTCS = new TaskCompletionSource();
        }

        public static void Main(string[] args)
        {
            // Keep the application running
            Instance.Value.InitializeSessionWatcher().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes the Session Watcher with the options 
        /// from the loaded configuration
        /// </summary>
        private async Task InitializeSessionWatcher()
        {
            // Make the user pick a valid Device if no Device
            // could be found with the ID from the config
            if (_selectedDevice is null)
            {
                ConsoleEx.WriteErrorLine($"Could not find Device by ID: " +
                    $"{_configManager.DeviceID}{System.Environment.NewLine}");
                _selectedDevice = SelectDeviceByUserInput();
            }
            _selectedDevice ??= SelectDeviceByUserInput();

            _sessionManager = _selectedDevice.AudioSessionManager;
            _sessionManager.OnSessionCreated += SessionManager_OnSessionCreated;

            if (_configManager.UseHiddenConsole)
            {
                HideConsoleWindow();
            }
            else
            {
                PrintMenu();
            }

            _ = SessionWatcherLoopAsync();
            await HandleUserInputAsync();
        }

        /// <summary>
        /// Pick a Device by using the ID from the Config
        /// </summary>
        /// <returns>The MMDevice if a matching ID was found</returns>
        private MMDevice? PickDeviceFromConfig()
        {
            var savedDeviceID = _configManager.DeviceID;
            return GetDeviceByID(savedDeviceID);
        }

        /// <summary>
        /// Pick a Device by using some ID
        /// </summary>
        /// <returns>The MMDevice if a matching ID was found</returns>
        private MMDevice? GetDeviceByID(string id)
        {
            try
            {
                var device = _deviceEnumerator.GetDevice(id);
                return device;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Procedure to pick a valid Device
        /// </summary>
        /// <returns>The MMDevice picked by the User</returns>
        private MMDevice SelectDeviceByUserInput()
        {
            bool hasPickedDevice = false;
            MMDevice pickedDevice = null!;

            while (!hasPickedDevice)
            {
                Console.WriteLine($"Please pick one of the following devices:{System.Environment.NewLine}");

                var deviceCollection = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All)
                    .OrderBy(x => x.State)
                    .ToList();

                var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);


                for (int i = 0; i < deviceCollection.Count; i++)
                {
                    var deviceChoice = deviceCollection[i];

                    if (deviceChoice.ID == defaultDevice.ID)
                    {
                        ConsoleEx.WriteInfoLine($"{i}: {deviceChoice.FriendlyName} - [{deviceChoice.State}] - Default Device");
                    }
                    else
                    {
                        Console.WriteLine($"{i}: {deviceChoice.FriendlyName} - [{deviceChoice.State}]");
                    }
                }

                var input = Console.ReadLine();

                try
                {
                    var pickedIndex = int.Parse(input!);

                    //if (pickedIndex >= 0 && pickedIndex <= deviceCollection.Count() - 1)
                    //{
                    //}

                    pickedDevice = deviceCollection.ElementAt(pickedIndex);
                    hasPickedDevice = true;
                    Console.Clear();
                }
                catch (Exception)
                {
                    Console.Clear();
                    ConsoleEx.WriteWarningLine($"\"{input}\" is not a valid index{System.Environment.NewLine}");
                }


            }

            return pickedDevice;
        }

        /// <summary>
        /// Write informations about the Session Watcher and 
        /// commands the User can use to the Console
        /// </summary>
        private void PrintMenu()
        {
            Console.WriteLine($"Device: {_selectedDevice!.FriendlyName} [{_selectedDevice.State}]");

            Console.WriteLine($"Peak volume threshold set to \"{_configManager.PeakVolumeThreshold.ToInvariantString("0.000")}\"");
            Console.WriteLine($"Polling rate set to \"{_configManager.PollingRate} ms\"{System.Environment.NewLine}");

            if (_isSessionWatcherRunning)
            {
                ConsoleEx.WriteSuccessLine("Session Watcher is running");
            }
            else
            {
                ConsoleEx.WriteErrorLine("Session Watcher is paused");
            }


            Console.WriteLine();
            Console.WriteLine("Type \"ct\" to change the peak volume threshold");
            Console.WriteLine("Type \"cr\" to change the polling rate");
            Console.WriteLine("Type \"cd\" to change the current device");
            Console.WriteLine("Type \"sd\" to save the current device as the default one");
            Console.WriteLine();

            if (_configManager.UseStartWithWindows)
            {
                Console.WriteLine("Type \"tw\" to stop starting this program with Windows");
            }
            else
            {
                Console.WriteLine("Type \"tw\" to start this program with Windows");
            }

            if (_configManager.UseAutoStartSW)
            {
                Console.WriteLine("Type \"ta\" to stop the Session Watcher from automatically starting");
            }
            else
            {
                Console.WriteLine("Type \"ta\" to automatically start the Session Watcher");
            }

            if (_configManager.UseBeepOnMute)
            {
                Console.WriteLine("Type \"tb\" to stop beeping when a Session gets muted");
            }
            else
            {
                Console.WriteLine("Type \"tb\" for a beep when a Session gets muted");
            }


            if (_isSessionWatcherRunning)
            {
                Console.WriteLine("Type \"tp\" to pause the Session Watcher");
            }
            else
            {
                Console.WriteLine("Type \"tp\" to start the Session Watcher");
            }

            Console.WriteLine();
            Console.WriteLine("Type \"clear\" to clear the console");
            Console.WriteLine("Type \"hide\" to hide the console until program restart");
            Console.WriteLine("Type \"hide_permanently\" to hide the console permanently");
            Console.WriteLine("Type \"exit\" to exit");
        }

        /// <summary>
        /// Called if a new Sound Session or "Processes with Sound" is created
        /// </summary>
        private void SessionManager_OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            // Use a lock so that the currently watched
            // Sessions do not get disposed
            lock (sessionLock)
            {
                // Dispose the old Sessions
                for (int i = 0; i < _sessionManager!.Sessions.Count; i++)
                {
                    _sessionManager.Sessions[i].Dispose();
                }

                // "Reload" all the Sessions of the current device's session manager
                _sessionManager.RefreshSessions();
            }
        }

        /// <summary>
        /// Session Watcher which will mute Sessions ("Processes with Sound")
        /// if they reach a certain threshold
        /// </summary>
        private async Task SessionWatcherLoopAsync()
        {
            try
            {
                while (!_sessionWatcherCTS.Token.IsCancellationRequested)
                {
                    lock (sessionLock)
                    {
                        var sessionCollection = _sessionManager!.Sessions;

                        for (int i = 0; i < sessionCollection.Count; i++)
                        {
                            var session = sessionCollection[i];

                            // Calculate the actual peak volume by multiplying the current master peak value
                            // times the volume limit for each session - using just the master peak value could
                            // result in muting sounds that dont reach the actual selected peak volume threshold 
                            double peakVolume = Math.Min(session.AudioMeterInformation.MasterPeakValue, 1) * session.SimpleAudioVolume.Volume;

                            // also only mute if the session is not already muted
                            if (peakVolume >= _configManager.PeakVolumeThreshold && !session.SimpleAudioVolume.Mute)
                            {
                                var dB = ConvertToDB(peakVolume);
                                MuteSession(session, dB, peakVolume);
                            }
                        }
                    }



                    await Task.Delay(_configManager.PollingRate, _sessionWatcherCTS.Token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                _sessionWatcherTCS.SetResult();
            }
        }

        /// <summary>
        /// Converts a normalized audio peak value to its equivalent in decibels (dB)
        /// </summary>
        /// <param name="peakValue">The audio peak value to convert, expected to be in the range 0 to 1.0
        /// </param>
        /// <returns>The converted value in decibels (dB), ranging from negative infinity 
        /// up to 0 dB for a peak value of 1.0.</returns>
        private double ConvertToDB(double peakValue)
        {
            return 20 * Math.Log10(peakValue);
        }

        //double ConvertToPeakValue(double dB)
        //{
        //    return Math.Pow(10, dB / 20);
        //}

        /// <summary>
        /// Will Mute a Session and inform the User about it
        /// </summary>
        /// <param name="session">The Session which crossed the threshold</param>
        /// <param name="dB">The detected volume in decibels</param>
        /// <param name="peakVolume">The detected volume in normalized peak value</param>
        private void MuteSession(AudioSessionControl session, double dB, double peakVolume)
        {
            var processName = ProcessNameHelper.GetProcessNameByAudioSession(session);
            session.SimpleAudioVolume.Mute = true;

            StringBuilder builder = new StringBuilder().AppendLine();

            builder.AppendLine(DateTime.Now.ToString());
            builder.AppendLine($"Muted \"{processName}\" at");
            builder.AppendLine($"{dB.ToInvariantString("0.00")} dB");
            builder.AppendLine($"{peakVolume.ToInvariantString("0.00")} Peak Volume");

            ConsoleEx.WriteInfoLine(builder.ToString());

            if (_configManager.UseBeepOnMute)
            {
                Task.Run(() =>
                {
                    Console.Beep();
                    Console.Beep();
                    Console.Beep();
                });
            }
        }

        /// <summary>
        /// Will Invoke User Commands if the input was a valid Command
        /// </summary>
        private async Task HandleUserInputAsync()
        {
            while (true)
            {
                string input = Console.ReadLine() ?? "";

                if (_commands.TryGetValue(input, out var action))
                {
                    await action.Invoke();
                }
                else
                {
                    await Console.Out.WriteLineAsync();
                    Console.ForegroundColor = ConsoleColor.Green;
                    await Console.Out.WriteLineAsync($"   > {input}");
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Stops the Session Watcher and waits
        /// for it to finish using a TaskCompletionSource
        /// </summary>
        private async Task StopSessionWatcher()
        {
            await _sessionWatcherCTS.CancelAsync();
            await _sessionWatcherTCS.Task;
        }

        #region User Commands

        /// <summary>
        /// Waits for the Session Watcher to stop and
        /// exits the application
        /// </summary>
        private async Task ExitApplication()
        {
            await StopSessionWatcher();
            Environment.Exit(0);
        }

        /// <summary>
        /// Hides the Console Window
        /// </summary>
        private void HideConsoleWindow()
        {
            WindowHelper.HideWindow();
        }

        /// <summary>
        /// Hides the Console Windows keeps it hidden by saving
        /// that option to the configuration
        /// </summary>
        private void ConfirmHideConsolePermanently()
        {
            Console.Clear();

            ConsoleEx.WriteWarningLine("Are you sure you want to permanently hide the console?");
            ConsoleEx.WriteWarningLine("Hiding the Console permanently can only be reversed by changing the config file!");
            Console.WriteLine("Type \"YES\" to confirm");

            string input = Console.ReadLine() ?? "";

            if (input.Equals("YES"))
            {
                _configManager.UseHiddenConsole = true;
                HideConsoleWindow();
            }
            else
            {
                ClearConsoleAndPrintMenu();
                ConsoleEx.WriteInfoLine($"{System.Environment.NewLine}Hiding Console canceled");
            }
        }

        /// <summary>
        /// Toggles whether or not to start the application with Windows
        /// using a Windows autorun Registry Key
        /// </summary>
        private void ToggleStartWithWindows()
        {
            _configManager.UseStartWithWindows = !_configManager.UseStartWithWindows;

            ClearConsoleAndPrintMenu();
            Console.WriteLine();

            if (_configManager.UseStartWithWindows)
            {
                ConsoleEx.WriteInfoLine("Added program to Windows autostart");
            }
            else
            {
                ConsoleEx.WriteInfoLine("Removed program from Windows autostart");
            }
        }

        /// <summary>
        /// Toggles whether or not the Session Watcher will automatically
        /// start when the application launches
        /// </summary>
        private void ToggleAutoStart()
        {

            _configManager.UseAutoStartSW = !_configManager.UseStartWithWindows;

            ClearConsoleAndPrintMenu();
            Console.WriteLine();

            if (_configManager.UseAutoStartSW)
            {
                ConsoleEx.WriteInfoLine("Session Watcher will start automatically now");
            }
            else
            {
                ConsoleEx.WriteInfoLine("Session Watcher will no longer automatically start");
            }


        }

        private void ToggleMuteOnBeep()
        {
            _configManager.UseBeepOnMute = !_configManager.UseBeepOnMute;

            ClearConsoleAndPrintMenu();
            Console.WriteLine();

            if (_configManager.UseBeepOnMute)
            {
                ConsoleEx.WriteInfoLine("Volume Watcher will now beep if a Session gets muted");
            }
            else
            {
                ConsoleEx.WriteInfoLine("Volume Watcher will no longer beep if a Session gets muted");
            }
        }

        /// <summary>
        /// Starts or pauses the Session Watcher
        /// </summary>
        private async Task ToggleSessionWatcher()
        {
            if (!_isSessionWatcherRunning)
            {
                _ = SessionWatcherLoopAsync();

                _isSessionWatcherRunning = true;

                ClearConsoleAndPrintMenu();

                ConsoleEx.WriteInfoLine($"{System.Environment.NewLine}Session Watcher started");
            }
            else
            {
                _isSessionWatcherRunning = false;

                Console.Clear();
                Console.WriteLine("Waiting for Session Watcher Task to stop");

                await StopSessionWatcher();

                _sessionWatcherCTS = new CancellationTokenSource();
                _sessionWatcherTCS = new TaskCompletionSource();

                ClearConsoleAndPrintMenu();

                ConsoleEx.WriteInfoLine($"{System.Environment.NewLine}Session Watcher paused");
            }
        }


        /// <summary>
        /// Changes the Peak Volume Threshold by using User inputs
        /// </summary>
        private void ChangePeakVolumeThreshold()
        {
            Console.Clear();

            var isValidNumber = false;
            while (!isValidNumber)
            {
                Console.WriteLine("Enter a valid number between 0.005 and 1.0 or \"a\" to abort");
                string input = Console.ReadLine() ?? "";

                if (input.Equals("") ||
                    input.Equals("a", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("abort", StringComparison.OrdinalIgnoreCase))
                {
                    ClearConsoleAndPrintMenu();
                    return;
                }

                if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                {
                    if (number >= 0.005 && number <= 1.0)
                    {
                        _configManager.PeakVolumeThreshold = number;
                        isValidNumber = true;
                    }
                    else
                    {
                        Console.Clear();
                        ConsoleEx.WriteErrorLine($"\"{number}\" is not a valid Peak Volume Threshold Value{System.Environment.NewLine}");
                    }
                }
                else
                {
                    Console.Clear();
                    ConsoleEx.WriteErrorLine($"\"{input}\" is not a valid Peak Volume Threshold Value{System.Environment.NewLine}");
                }
            }

            ClearConsoleAndPrintMenu();

            ConsoleEx.WriteInfoLine($"{System.Environment.NewLine}Peak volume threshold updated to " +
                $"\"{_configManager.PeakVolumeThreshold.ToInvariantString("0.000")}\"");
        }


        /// <summary>
        /// Changes the Session Watcher Polling Rate by using User inputs
        /// </summary>
        private void ChangePollingRate()
        {
            if (_isSessionWatcherRunning)
            {
                Console.WriteLine("Pause the SessionWatcher before changing the polling rate");
            }
            else
            {
                Console.Clear();

                Console.WriteLine("Changing the polling rate will change how often");
                Console.WriteLine("the peak volume threshold value will get checked in milliseconds");
                ConsoleEx.WriteWarningLine($"Change with caution{System.Environment.NewLine}");

                var isNumberValid = false;
                while (!isNumberValid)
                {
                    Console.WriteLine("Enter a valid number between 5 and 1000 or \"a\" to abort");
                    string input = Console.ReadLine() ?? "";

                    if (input.Equals("") ||
                        input.Equals("a", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("abort", StringComparison.OrdinalIgnoreCase))
                    {
                        ClearConsoleAndPrintMenu();
                        return;
                    }

                    if (int.TryParse(input, out var number))
                    {
                        if (number >= 5 && number <= 1000)
                        {
                            _configManager.PollingRate = number;

                            isNumberValid = true;
                        }
                        else
                        {
                            Console.Clear();
                            ConsoleEx.WriteErrorLine($"\"{number}\" is not a valid Polling Rate Value{System.Environment.NewLine}");
                        }
                    }
                    else
                    {
                        Console.Clear();
                        ConsoleEx.WriteErrorLine($"\"{input}\" is not a valid Polling Rate Value{System.Environment.NewLine}");
                    }
                }

                ClearConsoleAndPrintMenu();

                ConsoleEx.WriteInfoLine($"{System.Environment.NewLine}Polling rate changed to {_configManager.PollingRate}");
            }
        }


        /// <summary>
        /// Changes the currently picked Device
        /// </summary>
        private void ChangeDevice()
        {
            if (_isSessionWatcherRunning)
            {
                Console.WriteLine("Pause the SessionWatcher before changing devices");
            }
            else
            {
                _sessionManager!.OnSessionCreated -= SessionManager_OnSessionCreated;

                _selectedDevice = SelectDeviceByUserInput();

                _sessionManager = _selectedDevice.AudioSessionManager;
                _sessionManager.OnSessionCreated += SessionManager_OnSessionCreated;

                ClearConsoleAndPrintMenu();

                ConsoleEx.WriteInfoLine($"{System.Environment.NewLine}Device changed to {_selectedDevice.FriendlyName}");
            }
        }


        /// <summary>
        /// Saves the currently picked Device as the default one
        /// </summary>
        private void SaveDefaultDevice()
        {
            ClearConsoleAndPrintMenu();

            _configManager.DeviceID = _selectedDevice!.ID;

            ConsoleEx.WriteInfoLine($"{System.Environment.NewLine}{_selectedDevice.FriendlyName} saved as default device");
        }


        /// <summary>
        /// Clears the console and reprints the Menu
        /// </summary>
        private void ClearConsoleAndPrintMenu()
        {
            Console.Clear();
            PrintMenu();
        }

        #endregion


    }
}
