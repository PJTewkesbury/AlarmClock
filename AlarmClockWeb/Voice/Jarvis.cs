using AlarmClock.Hardware;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Pv;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.Voice
{
    public class Jarvis : IDisposable
    {
        string accessKey = "+qiP3GMh/Jc4x9KY2H5s/I42H4xFi1t/0jAQjs8Jx8ABzwOWzJz46w==";
        string contextPath = @"resources/AlarmClock_en_raspberry-pi_v3_0_0.rhn";
        string porcupineModelPath = @"resources/porcupine_params.pv";
        string rhinoModelPath = @"resources/rhino_params.pv";
        string cheetahModelPath = @"resources/cheetah_params.pv";
        string wakeWordPath1 = @"resources/jarvis_raspberry-pi.ppn";
        string wakeWordPath2 = @"resources/alexa_raspberry-pi.ppn";

        ILogger<Jarvis> Log;
        PvRecorder recorder = null;
        float rhinoSensitivity = 0.5f;
        bool requireEndpoint = true;
        Porcupine porcupine = null;
        Rhino rhino = null;
        Cheetah cheetah = null;

        public Jarvis(ILogger<Jarvis> Log, IConfiguration config)
        {
            this.Log = Log;            

            this.Log.LogInformation("Jarvis CTOR");
            var cs = config.GetSection("PicoVoice");
            if (cs != null)
            {
                accessKey = cs.GetValue("AccessKey", "+qiP3GMh/Jc4x9KY2H5s/I42H4xFi1t/0jAQjs8Jx8ABzwOWzJz46w==");
                contextPath = cs.GetValue("IntentFile", @"resources/AlarmClock_en_raspberry-pi_v3_0_0.rhn");
                porcupineModelPath = cs.GetValue("porcupineModelPath", @"resources/porcupine_params.pv");
                rhinoModelPath = cs.GetValue("rhinoModelPath", @"resources/rhino_params.pv");
                cheetahModelPath = cs.GetValue("cheetahModelPath", @"resources/cheetah_params.pv");
                wakeWordPath1 = cs.GetValue("wakeWordPath1", "resources/jarvis_raspberry-pi.ppn");
                wakeWordPath2 = cs.GetValue("wakeWordPath2", "resources/alexa_raspberry-pi.ppn");

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

        DateTime transcriptionStartTime;
        public void Run()
        {
            bool bUsePicoVoice = true;            
            try
            {
                int audioDeviceIndex = -1;
                List<BuiltInKeyword> wakeWords = new List<BuiltInKeyword>() { BuiltInKeyword.JARVIS, BuiltInKeyword.ALEXA };

                Console.WriteLine($"Checking for {porcupineModelPath}");
                if (!File.Exists(porcupineModelPath))
                {
                    Console.WriteLine($"NOT FOUND  {porcupineModelPath}");
                }

                Console.WriteLine($"Checking for {rhinoModelPath}");
                if (!File.Exists(rhinoModelPath))
                {
                    Console.WriteLine($"NOT FOUND  {rhinoModelPath}");
                }

                try
                {                    
                    //porcupine = Porcupine.FromBuiltInKeywords(accessKey, wakeWords);

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
                    
                    cheetah = Cheetah.Create(accessKey, cheetahModelPath, 2, true);

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
                    int WakeWordUsed = -1;
                    string transcript = "";
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

                            // Check for cancellation
                            // Program.cancellationToken.ThrowIfCancellationRequested();
                            if (Program.cancellationToken.IsCancellationRequested)
                                return;

                            short[] pcm = recorder.Read();

                            try
                            {
                                if (!_isWakeWordDetected)
                                {
                                    int rc = porcupine.Process(pcm);
                                    _isWakeWordDetected = rc >= 0;
                                    if (_isWakeWordDetected)
                                    {
                                        wakeWordCallback(rc);
                                        WakeWordUsed = rc;
                                        transcript = "";
                                    }
                                }
                                else
                                {
                                    bool isFinalized=false;
                                    // if (WakeWordUsed == 0)
                                    {
                                        isFinalized = rhino.Process(pcm);
                                        if (isFinalized)
                                        {
                                            _isWakeWordDetected = false;
                                            Inference inference = rhino.GetInference();
                                            inferenceCallback(inference);
                                        }
                                    } 
                                    // else if (WakeWordUsed == 1) 
                                    {
                                        if (String.IsNullOrEmpty(transcript))
                                            transcriptionStartTime = DateTime.Now;
                                        
                                        CheetahTranscript result = cheetah.Process(pcm);
                                        if (!string.IsNullOrEmpty(result.Transcript))
                                        {
                                            transcript += result.Transcript;
                                            // Console.WriteLine(" Transcript part :" + result.Transcript);
                                        }

                                        result.IsEndpoint = (!String.IsNullOrEmpty(transcript) && DateTime.Now.Subtract(transcriptionStartTime).TotalSeconds > 5);                                        

                                        if (result.IsEndpoint || isFinalized)
                                        {
                                            CheetahTranscript finalTranscriptObj = cheetah.Flush();
                                            _isWakeWordDetected = false;
                                            transcript += finalTranscriptObj.Transcript;
                                            
                                            Console.WriteLine(" Transcript :" + finalTranscriptObj.Transcript);

                                            if (isFinalized == false)
                                            {
                                                Task.Run(() =>
                                                {
                                                    AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
                                                    AlarmClock.NormalVolume();
                                                });
                                            };
                                        }
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
            if (porcupine != null)
            {
                porcupine.Dispose();
                porcupine = null;
            }
            if (rhino != null)
            {
                rhino.Dispose();
                rhino = null;
            }
            GC.SuppressFinalize(this);
        }

        ~Jarvis() => Dispose();

        static void wakeWordCallback(int wakeWordIndex)
        {
            string s = "Unknown";
            switch (wakeWordIndex)
            {
                case 0: s = "Jarvis"; break;
                case 1: s = "Alexa"; break;
            }
            Console.WriteLine($"WakeWord Detected : {s}");

            Task.Run(() =>
            {
                AlarmClock.QuiteVolume();
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisWake);
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisListen); // Should be listening 
            });         
        }

        static async void inferenceCallback(Inference inference)
        {
            List<Task> taskList = new List<Task>();

            AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaThinking);
            bool AddAlexaEnd = true;

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
                
                switch (inference.Intent.ToLower())
                {
                    case "lights":
                        {
                            taskList.Add(new Task(() =>
                            {                                
                                if (AlarmClock.ledRing.LedLitCount==0)
                                    AlarmClock.ledRing.SetAllLEDsToColor(System.Drawing.Color.FromArgb(255, 255, 255));
                                else
                                    AlarmClock.ledRing.SetAllLEDsToColor(System.Drawing.Color.FromArgb(0, 0, 0));
                            }));
                            AddAlexaEnd = false;
                        }
                        break;
                    case "lightson":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.ledRing.SetAllLEDsToColor(System.Drawing.Color.FromArgb(255, 255, 255));
                            }));
                            AddAlexaEnd = false;
                        }
                        break;
                    case "lightsoff":
                        {
                            taskList.Add(new Task(() =>
                            {
                                AlarmClock.ledRing.SetAllLEDsToColor(System.Drawing.Color.FromArgb(0, 0, 0));
                            }));
                            AddAlexaEnd = false;
                        }
                        break;
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
                            AlarmClock.alarmClockState.AlarmEnabled = true;                            
                            taskList.Add(new Task(() => {                                
                                // SayText($"The alarm time is now on", null);
                                AlarmClock.audio.PlayMP3("Sounds/ful/ful_ui_wakesound_touch.wav", WaitUntilComplete: true);
                            
                            }));
                        }
                        break;
                    case "turnalarmoff":
                        {
                            AlarmClock.alarmClockState.AlarmEnabled = false;
                            taskList.Add(new Task(() => {
                                // SayText($"The alarm time is now off", null);
                                AlarmClock.audio.PlayMP3("Sounds/ful/ful_ui_wakesound_touch.wav",WaitUntilComplete:true);
                            }));
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
                            taskList.Add(new Task(() =>
                            {
                                int hour = ParseNumber(inference.Slots["hour"]);
                                if (inference.Slots.ContainsKey("ampm") && inference.Slots["ampm"] == "PM")
                                    hour += 12;
                                int minute = 0;
                                if (inference.Slots.ContainsKey("minute"))
                                    minute = ParseNumber(inference.Slots["minute"]);
                                AlarmClock.SetAlarmTime(hour, minute);

                                string text = $"The alarm time is now set to {hour} {minute}";
                                SayText(text, null);

                                // AlarmClock.audio.PlayMP3("Sounds/ful/ful_ui_wakesound_touch.wav", WaitUntilComplete: true);
                            }));
                        }
                        break;
                    case "setradiostation":
                        {

                        }
                        break;
                    case "whatisthetime":
                        {                            
                            taskList.Add(SpeakTime());
                        }
                        break;
                    case "whatisthedate":
                        {
                            taskList.Add(SpeakDate());
                        }
                        break;
                    case "weather":
                        {                         
                            taskList.Add(SpeakWeather());
                        }
                        break;
                    case "quit":
                        {
                            AlarmClock.audio.PlayMP3("./Sounds/ful/ful_system_alerts_melodic_01_short.wav",WaitUntilComplete:true);
                            Program.Shutdown();
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Console.WriteLine("Didn't understand the command");
                taskList.Add( new Task(() => {
                    SayText("Sorry, I didn't understand that.");
                }));                
            }
            
            taskList.Add(new Task(() =>
            {
                if (AddAlexaEnd)
                    AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);

                AlarmClock.NormalVolume();
            }));

            // Run Tasks
            await Task.Run(() =>
            {
                foreach (var t in taskList)
                {
                    if (Program.cancellationToken.IsCancellationRequested)
                        return;

                    t.Start();
                    t.Wait();
                }
            });
        }

        private static int ParseNumber(string text)
        {
            switch(text.ToLower().Trim())
            {
                case "one": return 1;
                case "two": return 2;
                case "three": return 3;
                case "four": return 4;
                case "five": return 5;
                case "six": return 6;                
                case "seven": return 7;
                case "eight": return 8;
                case "nine": return 9;
                case "ten": return 10;
                case "eleven": return 11;
                case "twelve": return 12;
                case "fifteen": return 15;
                case "twenty": return 20;
                case "twenty five": return 25;
                case "thirty": return 30;
                case "thirty five": return 35;
                case "fourty": return 40;
                case "fourty five": return 45;
                case "fifty": return 50;
                case "fifty five": return 55;
            }
            int v = -1;
            int.TryParse(text, out v);
            return v;
        }

        private static Task SpeakTime()
        {
            return new Task(() =>
            {
                string text = $"The time is {DateTime.Now.ToString("hh:mm tt")}";
                SayText(text, null);                
            });
        }

        private static Task SpeakDate()
        {
            return new Task(() =>
            {            
                // Check for special dates like Bank Holiday Monday
                string text = $"The date is {DateTime.Now.ToString("dddd, d MMMM yyyy")}";
                SayText(text);                
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
            });
        }

        public static void SayText(string text, ILogger logger = null)
        {
            try
            {
                Console.WriteLine($"Speaking : {text}");

                if (File.Exists("/opt/speak.wav"))
                    File.Delete("/opt/speak.wav");

                // Create speech file
                $"/usr/bin/pico2wave -l=en-GB -w=/opt/speak.wav '{text}'".Bash(logger);

                if (Program.cancellationToken.IsCancellationRequested)
                    return;

                AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaSpeaking);

                // Play speech file
                AlarmClock.audio.PlayMP3("/opt/speak.wav", WaitUntilComplete:true);

                if (Program.cancellationToken.IsCancellationRequested)
                    return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to say : {text}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
