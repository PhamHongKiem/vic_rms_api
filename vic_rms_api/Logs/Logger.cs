using System;
using System.IO;
using System.Text;

namespace vic_rms_api.Logs
{
    
    public class Logger
    {
        private static readonly string logDirectory = @"C:\Logs\";
        private static int stackFromLevel = 2;
        /// <summary>
        /// start stack level to be get when get stack track
        /// </summary>
        public static int StackFromLevel
        {
            get { return stackFromLevel; }
            set { stackFromLevel = value; }
        }
        public static string getStackTrack()
        {
            StringBuilder str = new StringBuilder();

            try
            {
                System.Diagnostics.StackTrace stt = new System.Diagnostics.StackTrace();
                //get date
                //write message
                //stack info
                str.AppendLine("\tStack track:");
                for (int j = stackFromLevel; j < stt.FrameCount; j++)
                {
                    string module = stt.GetFrame(j).GetMethod().Module.ToString();
                    string cla = stt.GetFrame(j).GetMethod().DeclaringType.Name;
                    string method = stt.GetFrame(j).GetMethod().Name;
                    //if (module.StartsWith("System.") || module.StartsWith("CommonLanguageRuntimeLibrary") && !string.IsNullOrEmpty(str.ToString().Trim())) break;
                    if (module == "CommonLanguageRuntimeLibrary") break;
                    str.Append("\tLibrary: " + module + ";");
                    str.Append(" Class: " + cla + ";");
                    str.AppendLine(" Method: " + method + ";");
                }
            }
            catch
            {
            }
            return str.ToString().TrimEnd();
        }

        public static void Log(string message)
        {
            try
            {
                // Ensure the directory exists
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Create the file name based on the current date
                string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
                string logFilePath = Path.Combine(logDirectory, fileName);

                // Create a message with timestamp
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";

                // Append the log message to the file
                File.AppendAllText(logFilePath, logMessage);
                //string callStack = getStackTrack();
                //File.AppendAllText(logFilePath, callStack);

            }
            catch (Exception ex)
            {
                // If logging fails, write to console or handle it as necessary
                Console.WriteLine($"Failed to log: {ex.Message}");
            }
        }
    }

}
