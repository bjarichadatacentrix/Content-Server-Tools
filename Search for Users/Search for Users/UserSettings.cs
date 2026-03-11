using System;
using System.IO;

namespace Search_for_Users
{
    /// <summary>
    /// Persists user preferences (e.g. last log folder) in the user's AppData folder.
    /// </summary>
    internal static class UserSettings
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Search for Users");

        private static string LastLogFolderFilePath =>
            Path.Combine(SettingsFolder, "last_log_folder.txt");

        /// <summary>
        /// Gets the last selected log folder path, or null if not set or invalid.
        /// </summary>
        public static string? GetLastLogFolder()
        {
            try
            {
                var path = File.Exists(LastLogFolderFilePath)
                    ? File.ReadAllText(LastLogFolderFilePath).Trim()
                    : null;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    return path;
            }
            catch
            {
                // Ignore read errors
            }
            return null;
        }

        /// <summary>
        /// Saves the selected log folder path so it can be restored next time.
        /// </summary>
        public static void SetLastLogFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                File.WriteAllText(LastLogFolderFilePath, path.Trim());
            }
            catch
            {
                // Ignore write errors
            }
        }
    }
}
