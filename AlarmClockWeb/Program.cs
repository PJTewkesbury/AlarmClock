using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class Program
    {        
        public static void Main(string[] args)
        {
            Console.WriteLine("AlarmClockPI V1.2");
            Console.WriteLine("");
            if (args.Length > 0 && args[0].Equals("Debug", StringComparison.CurrentCultureIgnoreCase))
            {
                DateTime dt = DateTime.Now;
                Console.WriteLine("Waiting for debugger to attach or any key to continue");
                for (; ; )
                {
                    if ((DateTime.Now - dt).TotalMinutes > 2)
                        break;

                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey();
                        break;
                    }

                    if (Debugger.IsAttached)
                        break;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            
            // Start the Website
            var taskWebSite = Task.Run(() =>
            {
                CreateHostBuilder(args).Build().Run();
            });

            // Start the hardware loop
            var taskHardware = Task.Run(() =>
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    AlarmClock alarmClock = new AlarmClock();
                    alarmClock.Run(args);
                }
            });

            // Start the voice interface
            var taskPico = Task.Run(() =>
            {
                Jarvis jarvis = new Jarvis();
                jarvis.Run();
            });

            // Look for user pressing 'Q' key to quit if not running as systemd service
            var taskQuit = Task.Run(() =>
            {
                if (SystemdHelpers.IsSystemdService() == false)
                {
                    bool quit = false;
                    do
                    {
                        if (Console.KeyAvailable)
                        {
                            quit = (Console.ReadKey().Key == ConsoleKey.Q);
                        }
                        else
                        {
                            System.Threading.Thread.Yield();
                        }
                    }
                    while (quit == false);
                }
            });

            // Wait for one of the tasks to complete then quit.
            Task.WaitAny(taskWebSite, taskHardware, taskQuit, taskPico);
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })               
               .UseSystemd();
        }      
    }   
}
