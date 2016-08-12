using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotManeger
{
    class Program
    {
        const int accountCount = 50;
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "copy":
                        CopyConfig();
                        break;
                    case "run":
                        RunBot();
                        break;
                    default:
                        break;
                }
            }
        }

        private static void RunBot()
        {
            var lines = File.ReadAllLines("subpaths.txt");
            foreach(var line in lines)
            {
                Process.Start("NecroBot.exe", line);
            }
        }

        private static void CopyConfig()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var proxyStrings = File.ReadAllLines("proxies.txt");
            for (int i = 1; i <= accountCount; i++)
            {
                var soursePath = path + "utah9527@google";
                var destPath = path + "mp3" + i.ToString("0000") + "ya@ptc";
                if (Directory.Exists(destPath))
                    Directory.Delete(destPath, true);
                DirectoryCopy(soursePath, destPath, true);
                var lines = File.ReadAllLines(Path.Combine(destPath, "config", "auth.json"));
                for (int j = 0; j < lines.Length; j++)
                {
                    if (lines[j].Contains("\"AuthType\""))
                        lines[j] = "  \"AuthType\": \"ptc\",";
                    if (lines[j].Contains("\"GoogleUsername\""))
                        lines[j] = "  \"GoogleUsername\": null,";
                    if (lines[j].Contains("\"GooglePassword\""))
                        lines[j] = "  \"GooglePassword\": null,";
                    if (lines[j].Contains("\"PtcUsername\""))
                        lines[j] = $"  \"PtcUsername\": \"mp3{i.ToString("0000")}ya\",";
                    if (lines[j].Contains("\"PtcPassword\""))
                        lines[j] = "  \"PtcPassword\": \"kk752722\",";
                    if (lines[j].Contains("\"UseProxyHost\""))
                        lines[j] = $"  \"UseProxyHost\": \"{proxyStrings[i - 1].Split(':')[0]}\",";
                    if (lines[j].Contains("\"UseProxyPort\""))
                        lines[j] = $"  \"UseProxyPort\": \"{proxyStrings[i - 1].Split(':')[1]}\",";
                }
                File.WriteAllLines(Path.Combine(destPath, "config", "auth.json"), lines);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
