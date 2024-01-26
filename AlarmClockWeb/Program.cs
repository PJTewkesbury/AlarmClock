using AlarmClock.Voice;
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

namespace AlarmClock
{
    public class Program
    {
        private static CancellationTokenSource cancellationTokenSource = null;
        public static CancellationToken cancellationToken;

        public static void Shutdown()
        {
            if (Program.cancellationTokenSource != null)
                Program.cancellationTokenSource.Cancel();
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("AlarmClockPI V1.31");
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

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            List<Task> systemTasks = new List<Task>();

            IConfiguration config = new ConfigurationBuilder()
                            .AddJsonFile("appSettings.json", true)
                            .AddJsonFile($"appSettings.{Environment.MachineName}.json", true)
                            .Build();
             
            // Start the Website as a seperate thread (Low Priority)
            var taskWebSite = Task.Run(() =>
            {
               // Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
               CreateHostBuilder(args).Build().RunAsync(Program.cancellationToken);
            });
            systemTasks.Add(taskWebSite);            

            // Init Alarmclock Hardware
            AlarmClock alarmClock = new AlarmClock(config);
            alarmClock.Init();
            
            // Init Voice Assistant
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

            // Look for user pressing 'Q' key to quit if not running as systemd service
            var taskQuit = Task.Run(() =>
            {
               // Thread.CurrentThread.Priority = ThreadPriority.Lowest;
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
                       if (quit)
                        {
                            cancellationTokenSource.Cancel();
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

            Console.WriteLine("Make sure we turn LED's off etc");
            alarmClock.Dispose();
            alarmClock = null;

            jarvis.Dispose();
            jarvis = null;
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
