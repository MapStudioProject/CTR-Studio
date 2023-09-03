using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OpenTK.Graphics;
using Toolbox.Core;
using GLFrameworkEngine;
using System.Reflection;
using MapStudio.UI;
using System.Linq;

namespace CTRStudio
{
    public class Program
    {
        static bool IS_DEBUG = false;

        const string DLL_DIRECTORY = "Lib";

        static void Main(string[] args)
        {
            //Set global for method that compiles during debug building.
            IsDebugCheck(ref IS_DEBUG);

            //Hide the console unless debugging
            if (!IS_DEBUG)
                ConsoleWindowUtil.Hide();
            else
                ConsoleWindowUtil.Show();

            //Assembly searching from folders
            var domain = AppDomain.CurrentDomain;
            domain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);
            domain.AssemblyResolve += LoadAssembly;
            //Arguments in the command line
            var argumentHandle = LoadCmdArguments(args);
            if (argumentHandle.SkipWindow)
                return;

            //Global variables across the application
            InitRuntime();
            //Reload the language keys
            TranslationSource.Instance.Reload();
            //Initiate the texture resource creator for making texture instances from STGenericTexture.
            InitGLResourceCreation();
            //Load the window and run the application
            GraphicsMode mode = new GraphicsMode(new ColorFormat(32), 24, 8, 4, new ColorFormat(32), 2, false);
            var asssemblyVersion = GetRepoCompileDate(Runtime.ExecutableDir);

            var wnd = new UIFramework.Framework(new MainWindow(argumentHandle), mode, asssemblyVersion, "CTR Studio");
           // wnd.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            wnd.VSync = OpenTK.VSyncMode.On;
            wnd.Run();
        }

        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.IsTerminating)
            {
                if (!Directory.Exists("Logs"))
                    Directory.CreateDirectory("Logs");

                string date = DateTime.Now.ToFileTime().ToString();
                Exception e = (Exception)args.ExceptionObject;
                File.WriteAllText(Path.Combine(Runtime.ExecutableDir, "Logs", $"CrashLog_{date}.txt"), $"{e.Message}\n {e.StackTrace}");
            }
        }

        static string GetRepoCompileDate(string folder)
        {
            if (!File.Exists(Path.Combine(folder, "Version.txt")))
                return "";

            string[] versionInfo = File.ReadLines(Path.Combine(folder, "Version.txt")).ToArray();
            if (versionInfo.Length >= 3)
                return $"{versionInfo[0]} Commit: {versionInfo[2]} Compile Date: {versionInfo[1]}";

            return "";
        }

        [Conditional("DEBUG")]
        static void IsDebugCheck(ref bool isDebug)
        {
            isDebug = true;
        }

        static void InitRuntime()
        {
            //Global variables across the application
            Runtime.ExecutableDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Runtime.BonePointSize = 0.7f;
            Runtime.OpenTKInitialized = true;

            Directory.SetCurrentDirectory(Runtime.ExecutableDir);
        }

        static Arguments LoadCmdArguments(string[] args)
        {
            Console.WriteLine($"Args : {string.Join("", args)}");

            Arguments argumentHandle = new Arguments();
            foreach (var arg in args)
            {
                //Autmatically load files that are input into the command line.
                if (File.Exists(arg))
                    argumentHandle.FileInput.Add(arg);
            }
            Console.WriteLine($"FileInput {string.Join(" ", argumentHandle.FileInput)}");

            return argumentHandle;
        }

        //Render creation for the opengl backend
        //This is to keep the render handling more seperated from the core library
        static void InitGLResourceCreation()
        {
            //Called during LoadRenderable() in STGenericTexture to set the RenderableTex instance.
            RenderResourceCreator.CreateTextureInstance += TextureCreationOpenGL;
        }

        static IRenderableTexture TextureCreationOpenGL(object sender, EventArgs e)
        {
            var tex = sender as STGenericTexture;
            return GLTexture.FromGenericTexture(tex, tex.Parameters);
        }

        /// 
        /// Include externals dlls
        /// 
        private static Assembly LoadAssembly(object sender, ResolveEventArgs args)
        {
            Assembly result = null;
            if (args != null && !string.IsNullOrEmpty(args.Name))
            {
                //Get current exe fullpath
                FileInfo info = new FileInfo(Assembly.GetExecutingAssembly().Location);

                //Get folder of the executing .exe
                var folderPath = Path.Combine(info.Directory.FullName, DLL_DIRECTORY);

                //Build potential fullpath to the loading assembly
                var assemblyName = args.Name.Split(new string[] { "," }, StringSplitOptions.None)[0];
                var assemblyExtension = "dll";
                var assemblyPath = Path.Combine(folderPath, string.Format("{0}.{1}", assemblyName, assemblyExtension));

                //Check if the assembly exists in our "Libs" directory
                if (File.Exists(assemblyPath))
                {
                    //Load the required assembly using our custom path
                    result = Assembly.LoadFrom(assemblyPath);
                }
                else
                {
                    //Keep default loading
                    return args.RequestingAssembly;
                }
            }

            return result;
        }

        public class Arguments
        {
            public List<string> FileInput = new List<string>();

            public bool SkipWindow = false;
        }
    }
}