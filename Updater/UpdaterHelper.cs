using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Octokit;
using System.Security.AccessControl;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;

namespace Updater
{
    /// <summary>
    /// A helper class for downloading releases content
    /// </summary>
    public class UpdaterHelper
    {
        private static string _owner = "";
        private static string _repo = "";
        private static string _process_name = "";

        private static Release[] releases;

        /// <summary>
        /// Prepares the updater with the repo owner, repo name, and process to target installing.
        /// </summary>
        public static void Setup(string owner, string repo, string process = "")
        {
            _owner = owner;
            _repo = repo;
            _process_name = process;

            //Get the current set of releases for the owner and repo
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new GitHubClient(new ProductHeaderValue("UpdaterTool"));
            GetReleases(client).Wait();
        }

        /// <summary>
        /// Gets the first release instance of the github releases.
        /// </summary>
        public Release GetRelease() => releases.FirstOrDefault();

        /// <summary>
        /// Downloads the latest release if the version does not match the current version.
        /// </summary>
        public static void DownloadLatest(string folder, int assetIndex = 0, bool force = false)
        {
            Console.WriteLine($"Downloading latest repo!");

            //Check the current version date
            string currentDate = GetRepoCompileDate(folder);
            //Check if the current date matches the first release
            var release = releases.FirstOrDefault();
            if (release == null) { //No release uploaded so skip
                Console.WriteLine($"Failed to find release! None found!");
                return;
            }
            if (release.Assets.Count <= assetIndex) {
                Console.WriteLine($"Failed to uploaded asset for the latest release!");
                return;
            }
            //Check if the asset uploaded has an equal compile date
            if (!release.Assets[assetIndex].UpdatedAt.ToString().Equals(currentDate) || force)
            {
                //Remove existing install directories if they exist
                if (Directory.Exists($"{folder}\\{"latest"}" + "/"))
                    Directory.Delete($"{folder}\\{"latest"}" + "/", true);

                DownloadRelease(folder, release, assetIndex).Wait();
            }
            else
            {
                Console.WriteLine($"Current repo is up to date!");
            }
        }

        static async Task DownloadRelease(string folder, Release release, int assetIndex)
        {
            ProgressBar progressBar = new ProgressBar();
            Console.WriteLine();
            Console.WriteLine($"Downloading release {release.Name} Asset { release.Assets[assetIndex].Name}!");
            string address = release.Assets[assetIndex].BrowserDownloadUrl;

            string name = "latest";
            //Download the releases zip
            using (var webClient = new WebClient())
            {
                IWebProxy webProxy = WebRequest.DefaultWebProxy;
                webProxy.Credentials = CredentialCache.DefaultCredentials;
                webClient.Proxy = webProxy;
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    var pos = Console.GetCursorPosition();
                    progressBar.Report(e.ProgressPercentage / 100.0f);
                    //Thread.Sleep(20);
                };
                Uri uri = new Uri(address);
                await webClient.DownloadFileTaskAsync(uri, $"{folder}\\{name}.zip").ConfigureAwait(false);

                progressBar.Report(1.0f);
                progressBar.Dispose();
                Console.WriteLine($"");

                Console.WriteLine($"Extracting update!");
                //Extract the zip for intalling
                ExtractZip($"{folder}\\{name}");
                // Save the version info
                WriteRepoVersion(folder, release);

                Console.WriteLine($"Download finished!");
            }
        }

        /// <summary>
        /// Installs the currently downloaded and extracted update to the given folder directory.
        /// </summary>
        public static void Install(string folderDir)
        {
            string path = $"{folderDir}\\latest\\net5.0";

            if (!Directory.Exists(path)) {
                Console.WriteLine($"No downloaded directory found!");
                return;
            }

            if (Process.GetProcessesByName(_process_name).Any()) {
                Console.WriteLine($"Cannot install update while application is running. Please close it then try again!");
                return;
            }

            //Transfer the downloaded update files onto the current tool. 
            foreach (string dir in Directory.GetDirectories(path))
            {
                string dirName = new DirectoryInfo(dir).Name;
                //Remove existing directories
                if (Directory.Exists(Path.Combine(folderDir, dirName + @"\")))
                    Directory.Delete(Path.Combine(folderDir, dirName + @"\"), true);

                Directory.Move(dir, Path.Combine(folderDir, dirName + @"\"));
            }
            foreach (string file in Directory.GetFiles(path))
            {
                //Little hacky. Just skip the updater files as it currently uses the same directory as the installed tool.
                if (Path.GetFileName(file).StartsWith("Updater") || file.Contains("Octokit"))
                    continue;

                //Remove existing files
                if (File.Exists(Path.Combine(folderDir, Path.GetFileName(file))))
                    File.Delete(Path.Combine(folderDir, Path.GetFileName(file)));

                File.Move(file, Path.Combine(folderDir, Path.GetFileName(file)));
            }
            Directory.Delete($"{folderDir}\\latest", true);
        }

        static async Task GetReleases(GitHubClient client)
        {
            List<Release> Releases = new List<Release>();
            foreach (Release r in await client.Repository.Release.GetAll(_owner, _repo))
                Releases.Add(r);
            releases = Releases.ToArray();
        }

        //
        static string GetRepoCompileDate(string folder)
        {
            if (!File.Exists($"{folder}\\Version.txt"))
                return "";

            string[] versionInfo = File.ReadLines($"{folder}\\Version.txt").ToArray();
            if (versionInfo.Length >= 3)
                return versionInfo[1];

            return "";
        }

        //Stores the current release information within a .txt file
        static void WriteRepoVersion(string folder, Release release)
        {
            using (StreamWriter writer = new StreamWriter($"{folder}\\Version.txt"))
            {
                writer.WriteLine($"{release.TagName}");
                writer.WriteLine($"{release.Assets[0].UpdatedAt.ToString()}");
                writer.WriteLine($"{release.TargetCommitish}");
            }
        }

        static void ExtractZip(string filePath)
        {
            //Extract the updated zip
            ZipFile.ExtractToDirectory(filePath + ".zip", filePath + "/");
            //Zip not needed anymore
            File.Delete(filePath + ".zip");
        }
    }
}
