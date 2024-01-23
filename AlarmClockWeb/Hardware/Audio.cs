using System;
using System.IO;
using Un4seen.Bass;

namespace AlarmClock.Hardware
{
    public class Audio : IDisposable
    {
        public Audio()
        {
            BassNet.Registration("pjtewkesbury@live.com", "2X2519342152922");

            // check the version..
            if (Utils.HighWord(Bass.BASS_GetVersion()) != Bass.BASSVERSION)
            {
                Console.WriteLine("Wrong Bass Version!");
                return;
            }
            if (!Bass.BASS_Init(1, 44100, BASSInit.BASS_DEVIDE_DMIX | BASSInit.BASS_DEVICE_16BITS | BASSInit.BASS_DEVICE_STEREO, nint.Zero))
            {
                Console.WriteLine("Failed to init BASS");
                return;
            }
            Console.WriteLine("BASS Audio Init");
            Console.WriteLine($"CurrentVolume = {Bass.BASS_GetVolume()}");
            Bass.BASS_SetVolume(0.8f);
            Console.WriteLine($"New Volume = {Bass.BASS_GetVolume()}");
        }

        public void Dispose()
        {
            Bass.BASS_Stop();
            Bass.BASS_Free();
        }

        public void PlayMP3(String file, float volume=-1.0f)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"{file} not found.");
                return;
            }
            int mp3Stream = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE | BASSFlag.BASS_SAMPLE_FLOAT);
            if (volume>=0.0f && volume<=1.0f)
                Bass.BASS_ChannelSetAttribute(mp3Stream, BASSAttribute.BASS_ATTRIB_VOL, volume);
            Bass.BASS_ChannelPlay(mp3Stream, false);

            Console.WriteLine($"Now Playing {file}");
        }

        private int RadioStreamId = -1;
        public void PlayUrl(String url, float volume = -1.0f)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine($"No Url specified.");
                return;
            }

            StopUrl();

            RadioStreamId = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_STREAM_STATUS | BASSFlag.BASS_STREAM_AUTOFREE | BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
            
            if (volume >= 0.0f && volume <= 1.0f)
                Bass.BASS_ChannelSetAttribute(RadioStreamId, BASSAttribute.BASS_ATTRIB_VOL, volume);
            Bass.BASS_ChannelPlay(RadioStreamId, false);
            Console.WriteLine($"Now Playing {url}");
        }

        public void StopUrl()
        {        
            if (RadioStreamId > 0)
                Bass.BASS_ChannelStop(RadioStreamId);
            RadioStreamId = 0;
            Console.WriteLine($"Radio Stopped");
        }        
    }
}
