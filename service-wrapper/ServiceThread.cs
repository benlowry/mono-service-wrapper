using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ServiceWrapper
{
    public class ServiceThread
    {
        public static void Run(object o)
        {
            var properties = (BsonDocument) o;
            var path = new DirectoryInfo(properties["path"].AsString);
            var projectfile = path.GetFiles("*.csproj")[0];

            var xbuild = new ProcessStartInfo
            {
                FileName = "xbuild",
                Arguments = path.FullName + "/" + projectfile,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Console.WriteLine("** Compiling " + xbuild.FileName + " " + xbuild.Arguments);
            var compiler = Process.Start(xbuild);
            var ret = compiler.StandardOutput.ReadToEnd();
            compiler.WaitForExit();

            Console.WriteLine("** Starting " + projectfile);
            var buildpath = new DirectoryInfo(path.FullName + "/bin/Debug/");
            var exes = buildpath.GetFiles("*.exe");

            if(exes.Length == 0)
            {
                Console.WriteLine("** No exes found in build path " + buildpath.FullName);
                return;
            }

            var server = MongoServer.Create(ConfigurationManager.ConnectionStrings["mongodb"].ConnectionString);
            server.Connect();

            var database = server.GetDatabase("config");
            var collection = database.GetCollection("services");
            var filter = new QueryDocument {{"_id", properties["_id"].AsObjectId}};
            var update = new UpdateDocument();

            foreach (var key in properties)
                update[key.Name] = key.Value;

            update["restart"] = false;

            var service = new ProcessStartInfo
            {
                FileName = "mono",
                Arguments = exes[0].FullName,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var proc = Process.Start(service);

            while(true)
            {
                if (!proc.StandardOutput.EndOfStream)
                {
                    Console.WriteLine(proc.StandardOutput.ReadLine());
                }

                var refresh = collection.FindOne(filter);
                var restart = false;
                
                if(refresh.Contains("restart"))
                    restart = refresh["restart"].AsBoolean;

                if(restart)
                {
                    Console.WriteLine("** restarting " + projectfile);

                    try
                    {
                        proc.Kill();
                    }
                    catch
                    {
                        
                    }
                    finally
                    {
                        Console.WriteLine("** created new proces for " + projectfile);
                        proc = Process.Start(service);
                        collection.Update(filter, update);
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
