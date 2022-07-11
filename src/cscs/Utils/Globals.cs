using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using csscript;
using CSScripting.CodeDom;
using CSScriptLib;

namespace CSScripting
{
    /// <summary>
    /// The configuration and methods of the global context.
    /// </summary>
    public partial class Globals
    {
        static internal string DynamicWrapperClassName = "DynamicClass";
        static internal string RootClassName = "css_root";
        // Roslyn still does not support anything else but `Submission#0` (17 Jul 2019) [update]
        // Roslyn now does support alternative class names (1 Jan 2020)

        static internal string build_server
        {
            get
            {
                var path = Environment.SpecialFolder.CommonApplicationData.GetPath().PathJoin("cs-script",
                                                                                     "bin",
                                                                                     "compiler",
                                                                                     Assembly.GetExecutingAssembly().GetName().Version,
                                                                                     "build.dll");
                if (Runtime.IsLinux)
                {
                    path = path.Replace("/usr/share/cs-script", "/usr/local/share/cs-script");
                }
                return path;
            }
        }

        /// <summary>
        /// Removes the build server from the target system.
        /// </summary>
        /// <returns><c>true</c> if success; otherwise <c>false</c></returns>
        static public bool RemoveBuildServer()
        {
            try
            {
                File.Delete(build_server);
                File.Delete(build_server.ChangeExtension(".deps.json"));
                File.Delete(build_server.ChangeExtension(".runtimeconfig.json"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return !File.Exists(build_server);
        }

        /// <summary>
        /// Pings the running instance of the build server.
        /// </summary>
        static public void Ping()
        {
            Console.WriteLine(BuildServer.PingRemoteInstance(null));
        }

        static string csc_file = Environment.GetEnvironmentVariable("css_csc_file");

        static internal string LibDir => Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("lib");

        /// <summary>
        /// Gets the path to the assembly implementing Roslyn compiler.
        /// </summary>
        static public string roslyn => typeof(Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript).Assembly.Location;

        /// <summary>
        /// Gets the path to the dotnet executable.
        /// </summary>
        /// <value>The dotnet executable path.</value>
        static public string dotnet
        {
            get
            {
                var dotnetExeName = Runtime.IsLinux ? "dotnet" : "dotnet.exe";

                var file = "".GetType().Assembly.Location
                    .Split(Path.DirectorySeparatorChar)
                    .TakeWhile(x => x != "dotnet")
                    .JoinBy(Path.DirectorySeparatorChar.ToString())
                    .PathJoin("dotnet", dotnetExeName);

                return File.Exists(file) ? file : dotnetExeName;
            }
        }

        static internal string GetCompilerFor(string file)
            => file.GetExtension().SameAs(".cs") ? csc : csc.ChangeFileName("vbc.dll");

        static internal string CheckAndGenerateSdkWarning()
        {
            if (!csc.FileExists())
            {
                return $"WARNING: .NET {Environment.Version.Major} SDK cannot be found. It's required for `csc` and `dotnet` compiler engines.";
            }
            return null;
        }

        /// <summary>
        /// Gets or sets the path to the C# compiler executable (e.g. csc.exe or csc.dll)
        /// </summary>
        /// <value>The CSC.</value>
        static public string csc
        {
            set
            {
                csc_file = value;
            }

            get
            {
                if (csc_file == null)
                {
#if class_lib
                    if (!Runtime.IsCore)
                    {
                        csc_file = Path.Combine(Path.GetDirectoryName("".GetType().Assembly.Location), "csc.exe");
                    }
                    else
#endif
                    {
                        // Win: C:\Program Files\dotnet\sdk\6.0.100-rc.2.21505.57\Roslyn\bincore\csc.dll
                        //      C:\Program Files (x86)\dotnet\sdk\5.0.402\Roslyn\bincore\csc.dll
                        // Linux: ~dotnet/.../3.0.100-preview5-011568/Roslyn/... (cannot find SDK in preview)
                        //        /snap/dotnet-sdk/current/sdk/6.0.201/Roslyn/bincore/csc.dll
                        //        /snap/dotnet-sdk/158/sdk/6.0.201/Roslyn/bincore/csc.dll

                        // win:   program_files/dotnet/sdk/<version>/Roslyn/bincore/csc.dll
                        // linux:          root/dotnet/sdk/<version>/Roslyn/bincore/csc.dll

                        var dotnet_root = "".GetType().Assembly.Location;

                        // old algorithm: find first "dotnet" parent dir by trimming till the last "dotnet" token
                        // new algorithm: go back by 4 levels as on Linux in snap based deployments

                        dotnet_root = dotnet_root.Split(Path.DirectorySeparatorChar)
                                                 .Reverse()
                                                 // .SkipWhile(x => x != "dotnet")
                                                 .Skip(4)
                                                 .Reverse()
                                                 .JoinBy(Path.DirectorySeparatorChar.ToString());

                        if (dotnet_root.PathJoin("sdk").DirExists()) // need to check as otherwise it will throw
                        {
                            var dirs = dotnet_root.PathJoin("sdk")
                                                  .PathGetDirs($"{Environment.Version.Major}*")
                                                  .Where(dir => char.IsDigit(dir.GetFileName()[0]))
                                                  .OrderBy(x => System.Version.Parse(x.GetFileName().Split('-').First()))
                                                  .SelectMany(dir => dir.PathGetDirs("Roslyn"))
                                                  .ToArray();

                            csc_file = dirs.Select(dir => dir.PathJoin("bincore", "csc.dll"))
                                                   .LastOrDefault(File.Exists);
                        }
                    }
                }
                return csc_file;
            }
        }
    }
}