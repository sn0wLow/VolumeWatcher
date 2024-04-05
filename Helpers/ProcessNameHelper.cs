using NAudio.CoreAudioApi;
using System.Diagnostics;

namespace VolumeWatcher
{
    public static class ProcessNameHelper
    {
        public static string GetProcessNameByAudioSession(AudioSessionControl session)
        {
            string processName = string.Empty;

            if (session.IsSystemSoundsSession)
            {
                processName = "System Sound";
            }
            else
            {
                try
                {
                    Process process = Process.GetProcessById((int)session.GetProcessID);

                    if (!string.IsNullOrEmpty(process?.MainWindowTitle))
                    {
                        processName = process.MainWindowTitle;
                    }  // Fallbacks
                    else if (!string.IsNullOrEmpty(process?.MainModule?.FileVersionInfo?.FileDescription))
                    {
                        processName = process.MainModule.FileVersionInfo.FileDescription;
                    }
                    else if (!string.IsNullOrEmpty(process?.MainModule?.FileVersionInfo.OriginalFilename))
                    {
                        processName = Path.GetFileNameWithoutExtension(process.MainModule.FileVersionInfo.OriginalFilename);
                    }
                }
                catch (Exception)
                {
                }
            }


            return string.IsNullOrEmpty(processName) ? "<Unknown Process>" : processName;
        }
    }
}
