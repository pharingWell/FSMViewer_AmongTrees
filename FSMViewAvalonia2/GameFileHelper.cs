using Avalonia.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSMViewAvalonia2
{
    public static class GameFileHelper
    {
        public static readonly int AMONGTREES_APP_ID = 367520;
        public static readonly string AMONGTREES_GAME_NAME = "Among Trees";
        public static readonly string AMONGTREES_PATH_FILE = "atpath.txt";

        public static async Task<string> FindAmongTreesPath(Window win)
        {
            if (File.Exists(AMONGTREES_PATH_FILE))
            {
                return File.ReadAllText(AMONGTREES_PATH_FILE);
            }
            else
            {
                string path = await FindSteamGamePath(win, AMONGTREES_APP_ID, AMONGTREES_GAME_NAME);

                if (path != null)
                {
                    File.WriteAllText(AMONGTREES_PATH_FILE, path);
                }

                return path;
            }
        }

        public static async Task<string> FindSteamGamePath(Window win, int appid, string gameName)
        {
            string path = null;
            if (ReadRegistrySafe("Software\\Valve\\Steam", "SteamPath") != null)
            {
                string appsPath = Path.Combine((string)ReadRegistrySafe("Software\\Valve\\Steam", "SteamPath"), "steamapps");

                if (File.Exists(Path.Combine(appsPath, $"appmanifest_{appid}.acf")))
                {
                    return Path.Combine(Path.Combine(appsPath, "common"), gameName);
                }

                path = SearchAllInstallations(Path.Combine(appsPath, "libraryfolders.vdf"), appid, gameName);
            }

            if (path == null)
            {
                await MessageBoxUtil.ShowDialog(win, "Game location", "Couldn't find installation automatically. Please pick the location manually.");
                OpenFolderDialog ofd = new OpenFolderDialog();
                string folder = await ofd.ShowAsync(win);
                if (folder != null && folder != "")
                {
                    path = folder;
                }
            }

            return path;
        }

        private static string SearchAllInstallations(string libraryfolders, int appid, string gameName)
        {
            if (!File.Exists(libraryfolders))
            {
                return null;
            }
            StreamReader file = new StreamReader(libraryfolders);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Trim();
                line = Regex.Unescape(line);
                Match regMatch = Regex.Match(line, "\"(.*)\"\\s*\"(.*)\"");
                string key = regMatch.Groups[1].Value;
                string value = regMatch.Groups[2].Value;
                if (int.TryParse(key, out int _))
                {
                    if (File.Exists(Path.Combine(value, "steamapps", $"appmanifest_{appid}.acf")))
                    {
                        return Path.Combine(Path.Combine(value, "steamapps", "common"), gameName);
                    }
                }
            }

            return null;
        }

        private static object ReadRegistrySafe(string path, string key)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(path))
            {
                if (subkey != null)
                {
                    return subkey.GetValue(key);
                }
            }

            return null;
        }

        public static string FindGameFilePath(string atRootPath, string file)
        {
            string[] pathTests = new string[]
            {
                "among_trees_Data",
                "Among_Trees_Data",
                "Among Trees_Data",
                Path.Combine("Contents", "Resources", "Data")
            };
            foreach (string pathTest in pathTests)
            {
                string dataPath = Path.Combine(atRootPath, pathTest);
                string filePath = Path.Combine(dataPath, file);
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
            return null;
        }
    }
}
