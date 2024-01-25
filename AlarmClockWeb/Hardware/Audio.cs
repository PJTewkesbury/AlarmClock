using System;
using System.IO;
using Un4seen.Bass;

using UnitsNet;

namespace AlarmClock.Hardware
{
    public class Audio : IDisposable
    {
        public const string DefaultRadioUrl = "https://edge-audio-03-gos2.sharp-stream.com/ucbuk.mp3?device=ukradioplayer&=&&___cb=479109455";

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
            Bass.BASS_SetVolume(0.5f);
            Console.WriteLine($"New Volume = {Bass.BASS_GetVolume()}");
        }

        public void Dispose()
        {
            Bass.BASS_Stop();
            Bass.BASS_Free();
        }

        public void SetVolume(float vol=0.5f)
        {
            if (vol < 0.0f)
                vol = 0.0f;
            if (vol > 1.0f)
                vol = 1.0f;
            Bass.BASS_SetVolume(vol);
        }

        public float GetVolume()
        {
            return Bass.BASS_GetVolume();
        }

        int MP3StreamId = 0;
        public void PlayMP3(String file, float volume=0.75f)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"{file} not found.");
                return;
            }
            MP3StreamId = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE | BASSFlag.BASS_SAMPLE_FLOAT);
            if (volume>=0.0f && volume<=1.0f)
                Bass.BASS_ChannelSetAttribute(MP3StreamId, BASSAttribute.BASS_ATTRIB_VOL, volume);
            Bass.BASS_ChannelPlay(MP3StreamId, false);

            Console.WriteLine($"Now Playing {file}");
        }

        public bool IsMusicFilePlaying()
        {
            if (MP3StreamId == 0)
                return false;

            var r = Bass.BASS_ChannelIsActive(MP3StreamId);
            if (r == BASSActive.BASS_ACTIVE_PLAYING)
                return true;
            else 
                return false;
        }

        private int RadioStreamId = 0;
        public void PlayRadio(String url= DefaultRadioUrl, float volume = 0.75f)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine($"No Url specified.");
                return;
            }

            StopRadio();

            RadioStreamId = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_STREAM_STATUS | BASSFlag.BASS_STREAM_AUTOFREE | BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
            
            if (volume >= 0.0f && volume <= 1.0f)
                Bass.BASS_ChannelSetAttribute(RadioStreamId, BASSAttribute.BASS_ATTRIB_VOL, volume);
            Bass.BASS_ChannelPlay(RadioStreamId, false);
            Console.WriteLine($"Now Playing {url} ");
            Console.WriteLine($"Now RadioStreamId = {RadioStreamId}");
        }

        public void StopRadio()
        {
            if (RadioStreamId != 0)
            {                
                Bass.BASS_ChannelSetAttribute(RadioStreamId, BASSAttribute.BASS_ATTRIB_VOL, 0.0f);
                Bass.BASS_ChannelStop(RadioStreamId);
            }
            RadioStreamId = 0;
            Console.WriteLine($"Radio Stopped");
        }

        public bool RadioIsPlaying()
        {
            return (RadioStreamId != 0);
        }

        public void SetRadioVolume(float vol = 0.75f)
        {
            if (RadioStreamId != 0)
            {
                Console.WriteLine("No Radio Stream playing");
                return;
            }

            if (vol < 0.0f)
                vol = 0.0f;
            if (vol > 1.0f)
                vol = 1.0f;

            Bass.BASS_ChannelSetAttribute(RadioStreamId, BASSAttribute.BASS_ATTRIB_VOL, vol);
        }

        public float GetRadioVolume()
        {
            float vol = -1.0f;
            if (RadioStreamId != 0)
                return -1.0f;            

            Bass.BASS_ChannelGetAttribute(RadioStreamId, BASSAttribute.BASS_ATTRIB_VOL, ref vol);
            return vol;
        }

        float UnmutedRadioVol = -1.0f;
        public void MutePlayback()
        {
            Console.WriteLine("Mute Radio volume");
            UnmutedRadioVol = -1.0f;
            if (RadioIsPlaying())
            {                
                UnmutedRadioVol = GetRadioVolume();
                Console.WriteLine($"Radio volume is {UnmutedRadioVol}");
                SetRadioVolume(0.1f);
            }
        }
        public void UnmutePlayback()
        {            
            if (RadioIsPlaying() && UnmutedRadioVol>0.0f)
            {
                Console.WriteLine($"Unmute Radio volume = {UnmutedRadioVol}");
                SetRadioVolume(UnmutedRadioVol);
            }
        }
    }
}
