using AlarmClock.Picovoice;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class Program
    {        
        public static void Main(string[] args)
        {
            Console.WriteLine("AlarmClockPI V1.2");
            Console.WriteLine("");

            if (SystemdHelpers.IsSystemdService()==false && args.Length > 0 && args[0].Equals("Debug", StringComparison.CurrentCultureIgnoreCase))
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

            List<Task> systemTasks = new List<Task>();

            // Start the Website
            var taskWebSite = Task.Run(() =>
            {
               Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
               CreateHostBuilder(args).Build().Run();
            });
            systemTasks.Add(taskWebSite);

            // Start the hardware loop
            var taskHardware = Task.Run(() =>
            {
               // Thread.CurrentThread.Priority= ThreadPriority.BelowNormal;
               if (Environment.OSVersion.Platform == PlatformID.Unix)
               {
                   AlarmClock alarmClock = new AlarmClock(null);
                   alarmClock.Run(args);
               }
            });
            systemTasks.Add(taskHardware);

            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appSettings.json", true)
                                        .AddJsonFile($"appSettings.{Environment.MachineName}.json", true)
                                        .Build();

            AlarmClock alarmClock = new AlarmClock(config);
            alarmClock.Init();

            Jarvis jarvis = null;
            try
            {
                LoggerFactory loggerFactory = new LoggerFactory();
                jarvis = new Jarvis(loggerFactory.CreateLogger<Jarvis>(), config);
                jarvis.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            //// Start the voice interface
            //var taskPico = Task.Run(() =>
            //{
            //    Thread.CurrentThread.Priority = ThreadPriority.Highest;
            //    Jarvis jarvis = null;
            //    try
            //    {
            //        LoggerFactory loggerFactory = new LoggerFactory();
            //        jarvis = new Jarvis(loggerFactory.CreateLogger<Jarvis>(),config);
            //        jarvis.Run();
            //    }
            //    catch(Exception ex) { 
            //        Console.WriteLine(ex.Message);
            //        Console.WriteLine(ex.StackTrace);
            //    }
            //    do
            //    {                 
            //        System.Threading.Thread.Sleep(1000);
            //    }
            //    while (true);
            //});
            //systemTasks.Add(taskPico);

            // Look for user pressing 'Q' key to quit if not running as systemd service
            var taskQuit = Task.Run(() =>
            {
               Thread.CurrentThread.Priority = ThreadPriority.Lowest;
               if (SystemdHelpers.IsSystemdService() == false)
               {
                   bool quit = false;
                   do
                   {
                       if (Console.KeyAvailable)
                       {
                           var key = Console.ReadKey().Key;
                           quit = (key == ConsoleKey.Q);
                           if (key == ConsoleKey.P)
                               AlarmClock.PlayRadio();
                           if (key == ConsoleKey.S)
                               AlarmClock.StopRadio();
                           if (key == ConsoleKey.Z)
                               AlarmClock.ChangeVolume(1);
                           if (key == ConsoleKey.X)
                               AlarmClock.ChangeVolume(-1);
                       }
                       System.Threading.Thread.Yield();
                   }
                   while (quit == false);
               }
               else
               {
                   do
                   {
                       System.Threading.Thread.Yield();
                   }
                   while (true) ;
               }
            });
            systemTasks.Add(taskQuit);

            // Wait for one of the tasks to complete then quit.
            Task.WaitAny(systemTasks.ToArray());
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
