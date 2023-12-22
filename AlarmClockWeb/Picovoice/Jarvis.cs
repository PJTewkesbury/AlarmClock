using AlarmClock.Picovoice;

using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Pv;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class Jarvis
    {        
        string accessKey = "+qiP3GMh/Jc4x9KY2H5s/I42H4xFi1t/0jAQjs8Jx8ABzwOWzJz46w==";
        string contextPath = @"/Apps/AlarmClock/AlarmClockWeb/Picovoice/AlarmClock_en_raspberry-pi_v3_0_0.rhn";
        string porcupineModelPath = @"/Apps/AlarmClock/AlarmClockWeb/Picovoice/porcupine_params.pv";
        string rhinoModelPath = @"/Apps/AlarmClock/AlarmClockWeb/Picovoice/rhino_params.pv";
        
        ILogger<Jarvis> Log;

        public Jarvis(ILogger<Jarvis> Log, IConfiguration config)
        {
            this.Log = Log;

            this.Log.LogInformation("Jarvis CTOR");
            var cs = config.GetSection("PicoVoice");
            if (cs != null)
            {
                accessKey = cs.GetValue<string>("AccessKey", "+qiP3GMh/Jc4x9KY2H5s/I42H4xFi1t/0jAQjs8Jx8ABzwOWzJz46w==");
                contextPath = cs.GetValue<string>("IntentFile", @"/Apps/AlarmClock/AlarmClockWeb/Picovoice/AlarmClock_en_raspberry-pi_v3_0_0.rhn");
                
                porcupineModelPath = cs.GetValue<string>("porcupineModelPath", @"/Apps/AlarmClock/AlarmClockWeb/Picovoice/porcupine_params.pv");
                rhinoModelPath = cs.GetValue<string>("rhinoModelPath", @"/Apps/AlarmClock/AlarmClockWeb/Picovoice/rhino_params.pv");
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
            PvRecorder recorder = null;
            Picovoice picovoice = null;
            try
            {
                int audioDeviceIndex = -1;    
                List<BuiltInKeyword> wakeWords = new List<BuiltInKeyword>() { BuiltInKeyword.JARVIS, BuiltInKeyword.ALEXA };

                // string porcupineModelPath = Directory.GetCurrentDirectory()+"/bin/Debug/net7.0/linux-arm64/lib/common/porcupine_params.pv";
                Console.WriteLine($"Checking for {porcupineModelPath}");
                if (!File.Exists(porcupineModelPath)){
                    Console.WriteLine($"NOT FOUND  {porcupineModelPath}");
                }

                float porcupineSensitivity = 0.5f;
                // string rhinoModelPath = Directory.GetCurrentDirectory()+"/bin/Debug/net7.0/linux-arm64/lib/common/rhino_params.pv";
                Console.WriteLine($"Checking for {rhinoModelPath}");
                if (!File.Exists(rhinoModelPath)){
                    Console.WriteLine($"NOT FOUND  {rhinoModelPath}");
                }

                float rhinoSensitivity = 0.5f;
                bool requireEndpoint = true;
                
                Console.WriteLine($"PicoVoice Create : {Rhino.DEFAULT_MODEL_PATH}");
                picovoice = Picovoice.Create(
                       accessKey,
                       "/Apps/AlarmClock/AlarmClockWeb/Picovoice/jarvis_raspberry-pi.ppn",
                       wakeWordCallback,
                       contextPath,
                       inferenceCallback,
                       porcupineModelPath,
                       porcupineSensitivity,
                       rhinoModelPath,
                       rhinoSensitivity,
                       1,
                       requireEndpoint);

                Console.WriteLine($"Frame length : {picovoice.FrameLength}");
                Console.WriteLine("PvRecorder Create");
                recorder = PvRecorder.Create(picovoice.FrameLength,audioDeviceIndex );

                Console.WriteLine("PvRecorder Start");
                recorder.Start();
                Console.WriteLine($"Using device: {recorder.SelectedDevice}");
                Console.WriteLine("Listening...");

                if (bUsePicoVoice)
                {
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
                            if (pcm != null)
                                picovoice.Process(pcm);
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
        static void wakeWordCallback()
        {
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

        public static void SayText(string text, ILogger logger=null)
        {
            try{
                Console.WriteLine($"Speaking : {text}");                
                if (File.Exists("/opt/speak.wav"))
                    File.Delete("/opt/speak.wav");
                
                $"/usr/bin/pico2wave -l=en-GB -w=/opt/speak.wav '{text}' && aplay /opt/speak.wav".Bash(logger);
            }
            catch(Exception ex)
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
