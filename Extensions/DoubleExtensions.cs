using System.Globalization;

namespace VolumeWatcher
{
    public static class DoubleExtensions
    {
        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation
        /// using invariant culture
        /// </summary>
        /// <param name="value">The double value to convert</param>
        /// <returns>The string representation of the value in the invariant culture</returns>
        public static string ToInvariantString(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation
        /// using the specified format and the invariant culture
        /// </summary>
        /// <param name="value">The double value to convert</param>
        /// <param name="format">The numeric format string</param>
        /// <returns>The string representation of the value in the invariant culture</returns>
        public static string ToInvariantString(this double value, string? format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
