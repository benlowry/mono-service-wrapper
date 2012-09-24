using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MongoDB.Bson;

namespace ServiceWrapper
{
    public class ServiceThread
    {
        public static void Run(object o)
        {
            var service = (BsonDocument) o;
            var path = new DirectoryInfo(service["path"].AsString);
            var projectfile = path.GetFiles("*.csproj")[0];

            var xbuild = new ProcessStartInfo
            {
                FileName = "xbuild",
                Arguments = path.FullName + "/" + projectfile,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Console.WriteLine("** compiling " + xbuild.FileName + " " + xbuild.Arguments);
            var compiler = Process.Start(xbuild);
            compiler.StandardOutput.ReadToEnd();
            compiler.WaitForExit();

            Console.WriteLine("** starting " + projectfile);
            var buildpath = new DirectoryInfo(path.FullName + "/bin/Debug/");
            var exes = buildpath.GetFiles("*.exe");

            if(exes.Length == 0)
            {
                Console.WriteLine("** no exes found in build path " + buildpath.FullName);
                return;
            }

            var mono = new ProcessStartInfo
            {
                FileName = "mono",
                Arguments = exes[0].FullName,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var proc = Process.Start(mono);

            while(true)
            {
                if (!proc.StandardOutput.EndOfStream)
                {
                    Console.WriteLine(proc.StandardOutput.ReadLine());
                }

                if (DataStore.GetStatus(service))
                {
                    Console.WriteLine("** restarting " + projectfile);

                    try
                    {
                        proc.Kill();
                    }
                    catch(Exception err)
                    {
                        Console.WriteLine("** error restarting " + projectfile + ": " + err.Message);
                    }
                    finally
                    {
                        Console.WriteLine("** created new proces for " + projectfile);
                        proc = Process.Start(mono);
                        DataStore.SetStatus(service, false);
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
