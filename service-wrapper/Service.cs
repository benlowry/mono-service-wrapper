using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace ServiceWrapper
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            new Thread(Monitor).Start();
        }

        private static void Monitor()
        {
            var loaded = new List<String>();

            while (true)
            {
                var services = DataStore.ServiceList();

                foreach (var service in services)
                {
                    var projectdir = new DirectoryInfo(service["path"].AsString);

                    var projfiles = projectdir.GetFiles("*.csproj");

                    if (projfiles.Length == 0)
                        continue;

                    // start it
                    if (service["run"].AsBoolean && !loaded.Contains(projfiles[0].FullName))
                    {
                        Console.WriteLine("Starting " + projfiles[0].Name);
                        loaded.Add(projfiles[0].FullName);
                        new Thread(ServiceThread.Run).Start(service);
                    }
                    else
                    {

                        // check for updates
                        var p = Process.Start(new ProcessStartInfo
                                                  {
                                                      FileName = "git",
                                                      Arguments = "pull",
                                                      WorkingDirectory = projectdir.FullName + "/",
                                                      RedirectStandardOutput = true,
                                                      UseShellExecute = false
                                                  });
                        string ret = null;

                        try
                        {
                            ret = p.StandardOutput.ReadToEnd();
                            p.WaitForExit();
                            Console.WriteLine("** git refresh on " + projectdir.Name + " returned: " + ret);
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine("** error checking for update on " + projectdir.Name + ": " + err.Message);
                        }

                        if (!string.IsNullOrEmpty(ret) && ret.Contains("up-to-date"))
                        {
                            DataStore.SetStatus(service, true);
                        }

                        Thread.Sleep(10000);
                    }
                }

                Thread.Sleep(30000);
            }
        }
    }
}
