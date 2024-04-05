namespace VolumeWatcher
{
    public static class ConsoleEx
    {
        /// <summary>
        /// Writes a success message to the console in green color, then resets the color
        /// </summary>
        /// <param name="message">The message to be written</param>
        public static void WriteSuccessLine(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }


        /// <summary>
        /// Writes an information message to the console in cyan color, then resets the color
        /// </summary>
        /// <param name="message">The message to be written</param>
        public static void WriteInfoLine(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Writes a warning message to the console in yellow color, then resets the color
        /// </summary>
        /// <param name="message">The message to be written</param>
        public static void WriteWarningLine(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Writes an error message to the console in red color, then resets the color
        /// </summary>
        /// <param name="message">The message to be written</param>
        public static void WriteErrorLine(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Writes an error message and exception message to the console in red color, then resets the color
        /// </summary>
        /// <param name="message">The error message to be written</param>
        /// <param name="ex">The exception whose message is to be written</param>
        public static void WriteErrorLine(string message, Exception ex)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.WriteLine(ex.Message);
            Console.ForegroundColor = originalColor;
        }




    }
}
