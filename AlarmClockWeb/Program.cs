using AlarmClock.Voice;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.WriteLine("AlarmClockPI V1.31");
            Console.WriteLine("");

            if (args.Length>0)
            {
                int idx = 0;
                foreach (var a in args)
                {
                    Console.WriteLine($"Arg[{idx++}] = {a}");
                }
            }

            if (SystemdHelpers.IsSystemdService() == false && args.Length > 0 && args[1].Equals("Debug", StringComparison.CurrentCultureIgnoreCase))
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

            IConfiguration config = new ConfigurationBuilder()
                            .AddJsonFile("appSettings.json", true)
                            .AddJsonFile($"appSettings.{Environment.MachineName}.json", true)
                            .Build();

            List<Task> systemTasks = new List<Task>();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            // Start the Website as a seperate thread (Low Priority)
            bool bEnableWeb = false;
            if (bEnableWeb)
            {
                systemTasks.Add(new Task(() =>
                {
                    // Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    CreateHostBuilder(args).Build().RunAsync(Program.cancellationToken);
                    do
                    {
                        Thread.Sleep(50);
                    }
                    while (Program.cancellationToken.IsCancellationRequested == false);
                }));
            }

            AlarmClock alarmClock = null;
            alarmClock = new AlarmClock(config);
            // Init Alarmclock Hardware                
            alarmClock.Init();
            systemTasks.Add(new Task(() =>
            {
                alarmClock.Run();
            }));

            // Init Voice Assistant                
            LoggerFactory loggerFactory = new LoggerFactory();
            Jarvis jarvis = null;
            jarvis = new Jarvis(loggerFactory.CreateLogger<Jarvis>(), config);
            systemTasks.Add(new Task(() =>
            {
                try
                {
                    jarvis.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }));

            // Look for user pressing 'Q' key to quit if not running as systemd service
            systemTasks.Add(new Task(() =>
            {
                if (SystemdHelpers.IsSystemdService() == false)
                {
                    bool quit = false;
                    do
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey().Key;
                            Console.WriteLine($"Key Pressed : {key.ToString()}");
                            quit = (key == ConsoleKey.Q);
                            if (key == ConsoleKey.P)
                                AlarmClock.PlayRadio();
                            if (key == ConsoleKey.S)
                                AlarmClock.StopRadio();
                            if (key == ConsoleKey.UpArrow)
                                AlarmClock.ChangeVolume(1);
                            if (key == ConsoleKey.DownArrow)
                                AlarmClock.ChangeVolume(-1);
                        }
                        if (quit)
                            cancellationTokenSource.Cancel();
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        System.Threading.Thread.Sleep(100);
                    }
                    while (quit == false);
                }
                else
                {
                    do
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        System.Threading.Thread.Sleep(100);
                    }
                    while (true);
                }
            }));

            foreach (Task t in systemTasks)
            {
                Console.WriteLine($"Starting Task : {t.Id}");
                t.Start();
            }
            Console.WriteLine($"Waiting for Tasks to complete");
            Task.WaitAny(systemTasks.ToArray());

            Console.WriteLine("Make sure we turn LED's off etc");
            if (alarmClock!=null)
                alarmClock.Dispose();
            alarmClock = null;

            if (jarvis!=null) jarvis.Dispose();
            jarvis = null;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled Exception");
            if (e.ExceptionObject != null && e.ExceptionObject is Exception)
            {
                Exception ex = (e.ExceptionObject as Exception);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException!=null)
                {
                    Console.WriteLine("Inner Exception");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
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
