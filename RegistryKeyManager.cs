using Microsoft.Win32;
using System.Runtime.Versioning;

namespace VolumeWatcher
{
    public static class RegistryKeyManager
    {
        [SupportedOSPlatform("windows")]
        public static RegistryKeyValueState GetKeyValueState(string keyName)
        {
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string exePath = Environment.ProcessPath!;

            if (regKey is null)
            {
                return RegistryKeyValueState.NonExistant;
            }

            var keyValue = CheckKeyValue(regKey, keyName, exePath);

            if (keyValue == RegistryKeyValueState.IsInvalid)
            {
                RemoveKey(regKey, keyName);
                keyValue = RegistryKeyValueState.NonExistant;
            }

            return keyValue;
        }

        [SupportedOSPlatform("windows")]
        public static RegistryKeyValueState CheckKeyValue(RegistryKey registryKey, string keyName, string keyValue)
        {
            var regValue = registryKey.GetValue(keyName);

            if (regValue == null)
            {
                return RegistryKeyValueState.NonExistant;
            }

            if (!regValue.Equals(keyValue))
            {
                return RegistryKeyValueState.IsInvalid;
            }

            return RegistryKeyValueState.IsValid;
        }
        [SupportedOSPlatform("windows")]
        public static void SetKeyValue(RegistryKey registryKey, string keyName, string keyValue)
        {
            registryKey.SetValue(keyName, keyValue);
        }
        [SupportedOSPlatform("windows")]
        public static void RemoveKey(RegistryKey registryKey, string keyName)
        {
            registryKey.DeleteValue(keyName);
        }
    }
}
