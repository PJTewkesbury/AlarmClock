using AlarmClock.Picovoice;

using Microsoft.CognitiveServices.Speech;

using Pv;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class Jarvis
    {
        public void Run()
        {
            bool bUsePicoVoice = true;
            PvRecorder recorder = null;
            PicovoiceEx picovoice = null;
            try
            {
                int audioDeviceIndex = -1;
                string accessKey = "+qiP3GMh/Jc4x9KY2H5s/I42H4xFi1t/0jAQjs8Jx8ABzwOWzJz46w==";
                List<BuiltInKeyword> wakeWords = new List<BuiltInKeyword>() { BuiltInKeyword.JARVIS, BuiltInKeyword.ALEXA };                    
                string contextPath = @"/Apps/AlarmClock/Picovoice/AlarmClock_en_raspberry-pi_v2_1_0.rhn";                

                string porcupineModelPath = null;
                float porcupineSensitivity = 0.5f;
                string rhinoModelPath = null;
                float rhinoSensitivity = 0.5f;
                bool requireEndpoint = true;

                Console.WriteLine("PicoVoice Create");
                picovoice = PicovoiceEx.Create(
                       accessKey,
                       wakeWords,
                       wakeWordCallback,
                       contextPath,
                       inferenceCallback,
                       porcupineModelPath,
                       porcupineSensitivity,
                       rhinoModelPath,
                       rhinoSensitivity,
                       requireEndpoint);

                Console.WriteLine("PvRecorder Create");
                recorder = PvRecorder.Create(audioDeviceIndex, picovoice.FrameLength);

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
                            if (AlarmClock.ledRing.LedLitCount > 0 && picovoice._isWakeWordDetected == false)
                            {
                                Task.Run(() => {
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
                        }
                        // System.Threading.Thread.Yield();
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
        static void wakeWordCallback(int rc)
        {
            Console.WriteLine($"[wake word] : {(rc == 0 ? "Jarvis" : "Alexa")}");
            Task.Run(() =>
            {
                AlarmClock.MpcQuiteVolume();
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisWake);
                // AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaThinking); // Should be listening            
            });
        }

        static void inferenceCallback(Inference inference)
        {
            if (inference.IsUnderstood)
            {
                if (AlarmClock.ledRing != null)
                {
                    Task.Run(() => AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaSpeaking));
                }

                Console.WriteLine("{");
                Console.WriteLine($"  intent : '{inference.Intent}'");
                Console.WriteLine("  slots : {");
                foreach (KeyValuePair<string, string> slot in inference.Slots)
                    Console.WriteLine($"    {slot.Key} : '{slot.Value}'");
                Console.WriteLine("  }");
                Console.WriteLine("}\n");

                AlarmClock.MpcNormalVolume();                

                switch (inference.Intent.ToLower())
                {
                    case "turnradioon":
                        {
                            Task.Run(() =>
                            {
                                AlarmClock.PlayRadio();
                                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
                            });
                        }
                        break;
                    case "turnradiooff":
                        {
                            Task.Run(() =>
                            {
                                AlarmClock.StopRadio();
                                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
                            });
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
                            Task.Run(() =>
                            {                             
                                AlarmClock.ChangeVolume(-1, inference?.Slots);
                                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
                            });
                        }
                        break;
                    case "increasevolume":
                        {
                            Task.Run(() =>
                            {                             
                                AlarmClock.ChangeVolume(1, inference?.Slots);
                                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
                            });
                        }
                        break;
                    case "setvolume":
                        {
                            Task.Run(() =>
                            {
                                AlarmClock.ChangeVolume(0, inference?.Slots);
                                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
                            });
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
                            AlarmClock.MpcNormalVolume();
                            SpeakTime();
                        }
                        break;
                    case "whatisthedate":
                        {
                            AlarmClock.MpcNormalVolume();
                            SpeakDate();
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
            
            if (AlarmClock.ledRing != null)
            {
                Task.Run(() =>
                {
                    AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);                    
                });
            }                        
        }

        private static void SpeakTime()
        {
            Task.Run(() => { 
                string text = $"The time is {DateTime.Now.ToString("hh:mm tt")}";
                Console.WriteLine($"Speaking Time : {text}");
                SpeechSynthesizer synthesizer = GetTTS();
                if (synthesizer != null)
                    synthesizer.SpeakTextAsync(text).Wait();
                else
                    Console.WriteLine("No SpeechSynthesizer object created");
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
            });
        }

        private static void SpeakDate()
        {
            Task.Run(() =>
            {
                // Check for special dates like Bank Holiday Monday
                string text = $"The date is {DateTime.Now.ToString("dddd, d MMMM yyyy")}";
                Console.WriteLine($"Speaking Date : {text}");
                SpeechSynthesizer synthesizer = GetTTS();
                if (synthesizer != null)
                    synthesizer.SpeakTextAsync(text).Wait();
                else
                    Console.WriteLine("No SpeechSynthesizer object created");
                AlarmClock.ledRing.PlayAnimation(AlarmClock.JarvisEnd);
            });
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
