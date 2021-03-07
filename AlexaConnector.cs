using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock
{
    /// <summary>
    /// https://stackoverflow.com/questions/27842300/c-to-c-sharp-mono-memory-mapped-files-shared-memory-in-linux
    /// This class will use memory mapped file to allow alexa AVS SampleApp to talk to the AlarmClock.
    /// Alexa SampleApp can send messages when it is listening, thinking, talking etc and we can then control the
    /// LEDRing, volume, MPD etc.
    /// </summary>
    public class AlexaConnector : IDisposable
    {
        private MemoryMappedFile mmf;
        private Stream stream;
        public Subject<AlexaMessage> MessageQueue { get; set; }
        private AutoResetEvent autoEvent;
        private Timer timer;    

        public AlexaConnector()
        {
            mmf = MemoryMappedFile.CreateFromFile("/tmp/AlarmClock", FileMode.OpenOrCreate, "/tmp/AlarmClock");
            stream = mmf.CreateViewStream();

            MessageQueue = new Subject<AlexaMessage>();

            autoEvent = new AutoResetEvent(false);
            timer = new Timer(this.CheckStatus, autoEvent, 100, 100);          
        }

        public void CheckStatus(Object stateInfo)
        {
            // stream.Position = 0;
            int data = stream.ReadByte();
            if (data == -1)
                return;

            AlexaMessage msg = new AlexaMessage((byte)data);            
            MessageQueue.OnNext(msg);
        }

        public void Dispose()
        {
            if (stream != null)
                stream.Dispose();
            stream = null;

            if (mmf != null)
                mmf.Dispose();
            mmf = null;
        }
    }

    public enum enumAlexaMessageId { None=0, Listening=1, Thinking=2, Speaking=3, Finished=4, MuteMic=5, UnmuteMic=6, Notification=7 };

    public class AlexaMessage
    {
        public byte MessageId { get; set; }               

        public enumAlexaMessageId AlexaMessageType
        {
            get
            {
                return (enumAlexaMessageId)MessageId;
            }
        }
        public AlexaMessage(byte data)
        {
            MessageId = data;
        }
    }
}
