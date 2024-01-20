﻿using AlarmClock.Picovoice;

using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Pv;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class Jarvis : IDisposable
    {
        string accessKey = "+qiP3GMh/Jc4x9KY2H5s/I42H4xFi1t/0jAQjs8Jx8ABzwOWzJz46w==";
        string contextPath = @"/Apps/AlarmClock/Picovoice/AlarmClock_en_raspberry-pi_v3_0_0.rhn";
        string porcupineModelPath = @"/Apps/AlarmClock/Picovoice/porcupine_params.pv";
        string rhinoModelPath = @"/Apps/AlarmClock/Picovoice/rhino_params.pv";
        string wakeWordPath1 = @"/Apps/AlarmClock/Picovoice/jarvis_raspberry-pi.ppn";
        string wakeWordPath2 = @"/Apps/AlarmClock/Picovoice/alexa_raspberry-pi.ppn";

        ILogger<Jarvis> Log;
        PvRecorder recorder = null;
        float rhinoSensitivity = 0.5f;
        bool requireEndpoint = true;
        Porcupine porcupine = null;
        Rhino rhino = null;

        public Jarvis(ILogger<Jarvis> Log, IConfiguration config) 
        {
            this.Log = Log;

            this.Log.LogInformation("Jarvis CTOR");
            var cs = config.GetSection("PicoVoice");
            if (cs != null)
            {
                accessKey = cs.GetValue<string>("AccessKey", "+qiP3GMh/Jc4x9KY2H5s/I42H4xFi1t/0jAQjs8Jx8ABzwOWzJz46w==");
                contextPath = cs.GetValue<string>("IntentFile", @"/Apps/AlarmClock/Picovoice/AlarmClock_en_raspberry-pi_v3_0_0.rhn");

                porcupineModelPath = cs.GetValue<string>("porcupineModelPath", @"/Apps/AlarmClock/Picovoice/porcupine_params.pv");
                rhinoModelPath = cs.GetValue<string>("rhinoModelPath", @"/Apps/AlarmClock/Picovoice/rhino_params.pv");
                wakeWordPath1 = cs.GetValue<string>("wakeWordPath1", "/Apps/AlarmClock/Picovoice/jarvis_raspberry-pi.ppn");
                wakeWordPath2 = cs.GetValue<string>("wakeWordPath2", "/Apps/AlarmClock/Picovoice/alexa_raspberry-pi.ppn");

                this.Log.LogInformation($"Access key : {accessKey}");
                this.Log.LogInformation($"Access key : {contextPath}");

                if (File.Exists(contextPath))
                {
                    this.Log.LogInformation($"File Exists");
                }
                else
                {
                    this.Log.LogInformation($"File DOES NOT Exist");
                }
            }
        }

        public void Run()
        {
            bool bUsePicoVoice = true;            
            // Picovoice picovoice = null;
            try
            {
                int audioDeviceIndex = -1;
                List<BuiltInKeyword> wakeWords = new List<BuiltInKeyword>() { BuiltInKeyword.JARVIS, BuiltInKeyword.ALEXA };

                // string porcupineModelPath = Directory.GetCurrentDirectory()+"/bin/Debug/net7.0/linux-arm64/lib/common/porcupine_params.pv";
                Console.WriteLine($"Checking for {porcupineModelPath}");
                if (!File.Exists(porcupineModelPath))
                {
                    Console.WriteLine($"NOT FOUND  {porcupineModelPath}");
                }

                float porcupineSensitivity = 0.5f;
                // string rhinoModelPath = Directory.GetCurrentDirectory()+"/bin/Debug/net7.0/linux-arm64/lib/common/rhino_params.pv";
                Console.WriteLine($"Checking for {rhinoModelPath}");
                if (!File.Exists(rhinoModelPath))
                {
                    Console.WriteLine($"NOT FOUND  {rhinoModelPath}");
                }
                
                try
                {
                    var keywordPaths = new List<string>();
                    keywordPaths.Add(wakeWordPath1);
                    keywordPaths.Add(wakeWordPath2);

                    porcupine = Porcupine.FromKeywordPaths(accessKey, keywordPaths, porcupineModelPath);
                                         
                    rhino = Rhino.Create(
                        accessKey,
                        contextPath,
                        modelPath: rhinoModelPath,
                        sensitivity: rhinoSensitivity,
                        endpointDurationSec: 1.0f,
                        requireEndpoint: requireEndpoint);

                    if (porcupine.FrameLength != rhino.FrameLength)
                    {
                        throw new ArgumentException($"Porcupine frame length ({porcupine.FrameLength}) and Rhino frame length ({rhino.FrameLength}) are different");
                    }

                    if (porcupine.SampleRate != rhino.SampleRate)
                    {
                        throw new ArgumentException($"Porcupine sample rate ({porcupine.SampleRate}) and Rhino sample rate ({rhino.SampleRate}) are different");
                    }

                    Console.WriteLine($"Frame length : {porcupine.FrameLength}");
                    Console.WriteLine("PvRecorder Create");
                    recorder = PvRecorder.Create(porcupine.FrameLength, audioDeviceIndex);

                    Console.WriteLine("PvRecorder Start");
                    recorder.Start();
                    Console.WriteLine($"Using device: {recorder.SelectedDevice}");
                    Console.WriteLine("Listening...");
                }
                catch (Exception ex)
                {
                    bUsePicoVoice = false;
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                        Console.WriteLine(ex.InnerException.StackTrace);
                    }                    
                    // throw;
                }
                if (bUsePicoVoice)
                {
                    bool _isWakeWordDetected = false;
                    do
                    {
                        // Listen for voice and process.
                        try
                        {
                            if (AlarmClock.ledRing?.LedLitCount > 0
                             // && picovoice._isWakeWordDetected == false
                             )
                            {
                                Task.Run(() =>
                                {
                                    AlarmClock.ledRing.ClearPixels();
                                });
                            }

                            short[] pcm = recorder.Read();

                            try
                            {
                                if (!_isWakeWordDetected)
                                {
                                    int rc = porcupine.Process(pcm);
                                    _isWakeWordDetected = (rc >= 0);
                                    if (_isWakeWordDetected)
                                        wakeWordCallback(rc);
                                }
                                else
                                {
                                    bool isFinalized = rhino.Process(pcm);
                                    if (isFinalized)
                                    {
                                        _isWakeWordDetected = false;
                                        Inference inference = rhino.GetInference();
                                        inferenceCallback(inference);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                    while (true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while configuring PicoVoice - Disabling Pico Voice functions");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                    Console.WriteLine(ex.InnerException.StackTrace);
                }
            }
        }

        public void Dispose()
        {
            if (this.porcupine != null)
            {
                this.porcupine.Dispose();
                this.porcupine = (Porcupine)null;
            }
            if (this.rhino != null)
            {
                this.rhino.Dispose();
                this.rhino = (Rhino)null;
            }
            GC.SuppressFinalize((object)this);
        }

        ~Jarvis() => this.Dispose();

        static void wakeWordCallback(int wakeWordIndex)
        {
            string s = "Unknown";
            switch(wakeWordIndex)
            {
                case 0: s="Jarvis"; break;
                case 1: s="Alexa"; break;                    
            }
            Console.WriteLine($"WakeWord Detected : {s}");

            Task.Run(() =>
            {
                AlarmClock.QuiteVolume();
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisWake);

                AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaThinking); // Should be listening            

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(15));
                if (AlarmClock.ledRing.LedLitCount > 0)
                    AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
            });
        }

        static async void inferenceCallback(Inference inference)
        {
            List<Task> taskList = new List<Task>();

            if (inference.IsUnderstood)
            {
                taskList.Add(new Task(() =>
                {
                    AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaSpeaking);
                }));

                Console.WriteLine("{");
                Console.WriteLine($"  intent : '{inference.Intent}'");
                Console.WriteLine("  slots : {");
                foreach (KeyValuePair<string, string> slot in inference.Slots)
                    Console.WriteLine($"    {slot.Key} : '{slot.Value}'");
                Console.WriteLine("  }");
                Console.WriteLine("}\n");

                AlarmClock.NormalVolume();

                switch (inference.Intent.ToLower())
                {
                    case "turnradioon":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.PlayRadio();
                            }));
                        }
                        break;
                    case "turnradiooff":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.StopRadio();
                            }));
                        }
                        break;
                    case "turnalarmon":
                        {

                        }
                        break;
                    case "turnalarmoff":
                        {

                        }
                        break;

                    case "snooze":
                        {

                        }
                        break;
                    case "decreasevolume":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.ChangeVolume(-1, inference?.Slots);
                            }));
                        }
                        break;
                    case "increasevolume":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.ChangeVolume(1, inference?.Slots);
                            }));
                        }
                        break;
                    case "setvolume":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.ChangeVolume(0, inference?.Slots);
                            }));
                        }
                        break;

                    case "setalarmtime":
                        {

                        }
                        break;
                    case "setradiostation":
                        {

                        }
                        break;
                    case "whatisthetime":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.NormalVolume();
                            }));
                            taskList.Add(SpeakTime());
                        }
                        break;
                    case "whatisthedate":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.NormalVolume();
                            }));
                            taskList.Add(SpeakDate());
                        }
                        break;
                    case "weather":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.NormalVolume();
                            }));
                            taskList.Add(SpeakWeather());
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Console.WriteLine("Didn't understand the command\n");
            }

            taskList.Add(new Task(() =>
            {
                if (AlarmClock.ledRing != null)
                    AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
            }));

            // Run Tasks
            await Task.Run(() =>
            {
                foreach (var t in taskList)
                {
                    t.Start();
                    t.Wait();
                }
            });
        }

        private static Task SpeakTime()
        {
            return new Task(() =>
            {
                string text = $"The time is {DateTime.Now.ToString("hh:mm tt")}";
                SayText(text, null);
                // SpeechSynthesizer synthesizer = GetTTS();
                // if (synthesizer != null)
                //     synthesizer.SpeakTextAsync(text).Wait();
                // else
                //     Console.WriteLine("No SpeechSynthesizer object created");
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
            });
        }

        private static Task SpeakDate()
        {
            return new Task(() =>
            {
                // Check for special dates like Bank Holiday Monday
                string text = $"The date is {DateTime.Now.ToString("dddd, d MMMM yyyy")}";
                SayText(text);
                // SpeechSynthesizer synthesizer = GetTTS();
                // if (synthesizer != null)
                //     synthesizer.SpeakTextAsync(text).Wait();
                // else
                //     Console.WriteLine("No SpeechSynthesizer object created");
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
            });
        }
        private static Task SpeakWeather()
        {
            return new Task(() =>
            {
                // Get Weather forecast for todate

                // Check for special dates like Bank Holiday Monday
                string text = $"The weather for today in Marple is still being devloped. Please check back tomorrow";
                SayText(text);
                // SpeechSynthesizer synthesizer = GetTTS();
                // if (synthesizer != null)
                //     synthesizer.SpeakTextAsync(text).Wait();
                // else
                //     Console.WriteLine("No SpeechSynthesizer object created");
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
            });
        }

        public static void SayText(string text, ILogger logger = null)
        {
            try
            {
                Console.WriteLine($"Speaking : {text}");
                if (File.Exists("/opt/speak.wav"))
                    File.Delete("/opt/speak.wav");

                $"/usr/bin/pico2wave -l=en-GB -w=/opt/speak.wav '{text}' && aplay /opt/speak.wav".Bash(logger);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to say : {text}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static SpeechSynthesizer GetTTS()
        {
            var config = SpeechConfig.FromSubscription("f9e7343e7af348ada02b9c4b1626c823", "uksouth");
            // config.SpeechSynthesisVoiceName = "Ryan"; // Libby, Mia, Sonia, Ryan
            var synthesizer = new SpeechSynthesizer(config);
            return synthesizer;
        }
    }
}
