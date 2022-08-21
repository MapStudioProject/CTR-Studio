using System;
using System.Threading;
using System.IO;
using System.Linq;

namespace Updater
{
    internal class Program
    {
        static string execDirectory = "";

        static void Main(string[] args)
        {
            execDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            bool force = args.Contains("-f");
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-d":
                    case "--download":
                        UpdaterHelper.Setup("MapStudioProject", "Track-Studio", "TrackStudio.exe");
                        UpdaterHelper.DownloadLatest(execDirectory, 0, force);
                        break;
                    case "-i":
                    case "--install":
                        UpdaterHelper.Install(execDirectory);
                        break;
                    case "-b":
                    case "--boot":
                        Boot();
                        Environment.Exit(0);
                        break;
                    case "-bl":
                    case "--boot_launcher":
                        BootLauncher();
                        Environment.Exit(0);
                        break;
                    case "-e":
                    case "--exit":
                        Environment.Exit(0);
                        break;
                }
            }
        }

        static void BootLauncher()
        {
            Console.WriteLine("Booting...");

            Thread.Sleep(3000);
            System.Diagnostics.Process.Start(Path.Combine(execDirectory, "TrackStudioLauncher.exe"));
        }

        static void Boot()
        {
            Console.WriteLine("Booting...");

            Thread.Sleep(3000);
            System.Diagnostics.Process.Start(Path.Combine(execDirectory, "TrackStudio.exe"));
        }
    }
}
