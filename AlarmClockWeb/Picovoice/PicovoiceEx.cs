using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pv;

namespace AlarmClock.Picovoice
{
    public class PicovoiceEx : IDisposable
    {
        private Porcupine _porcupine;
        private Action<int> _wakeWordCallback;
        private Rhino _rhino;
        private Action<Inference> _inferenceCallback;
        private bool _isWakeWordDetected;

        public static PicovoiceEx Create(
          string accessKey,
          List<BuiltInKeyword> wakeWordList,
          Action<int> wakeWordCallback,
          string contextPath,
          Action<Inference> inferenceCallback,
          string porcupineModelPath = null,
          float porcupineSensitivity = 0.5f,
          string rhinoModelPath = null,
          float rhinoSensitivity = 0.5f,
          bool requireEndpoint = true)
        {
            if (wakeWordCallback == null)
                throw new PicovoiceInvalidArgumentException("'wakeWordCallback' cannot be null");
            if (inferenceCallback == null)
                throw new PicovoiceInvalidArgumentException("'inferenceCallback' cannot be null");
            try
            {
                string accessKey1 = accessKey;
                
                Console.WriteLine($"Create Rhino");
                Rhino rhino = Rhino.Create(accessKey, contextPath, rhinoModelPath, rhinoSensitivity, requireEndpoint);

                Console.WriteLine($"Create porcupine : {String.Join(",", wakeWordList)}");                
                Porcupine porcupine = Porcupine.FromBuiltInKeywords(accessKey1, wakeWordList.AsEnumerable<BuiltInKeyword>());                

                if (porcupine.FrameLength != rhino.FrameLength)
                    throw new PicovoiceInvalidArgumentException(string.Format("Porcupine frame length ({0}) and Rhino frame length ({1}) are different", (object)porcupine.FrameLength, (object)rhino.FrameLength));

                if (porcupine.SampleRate != rhino.SampleRate)
                    throw new PicovoiceInvalidArgumentException(string.Format("Porcupine sample rate ({0}) and Rhino sample rate ({1}) are different", (object)porcupine.SampleRate, (object)rhino.SampleRate));

                Console.WriteLine($"Create PicoVoiceEx");
                return new PicovoiceEx(porcupine, wakeWordCallback, rhino, inferenceCallback);
            }
            catch (Exception ex)
            {
                throw PicovoiceEx.MapToPicovoiceException(ex);
            }
        }

        private PicovoiceEx(
          Porcupine porcupine,
          Action<int> wakeWordCallback,
          Rhino rhino,
          Action<Inference> inferenceCallback)
        {
            this._porcupine = porcupine;
            this._wakeWordCallback = wakeWordCallback;
            this._rhino = rhino;
            this._inferenceCallback = inferenceCallback;
            this.FrameLength = porcupine.FrameLength;
            this.SampleRate = porcupine.SampleRate;
            this.PorcupineVersion = porcupine.Version;
            this.RhinoVersion = rhino.Version;
            this.ContextInfo = rhino.ContextInfo;
        }

        public void Dispose()
        {
            if (this._porcupine != null)
            {
                this._porcupine.Dispose();
                this._porcupine = (Porcupine)null;
            }
            if (this._rhino != null)
            {
                this._rhino.Dispose();
                this._rhino = (Rhino)null;
            }
            GC.SuppressFinalize((object)this);
        }

        ~PicovoiceEx() => this.Dispose();

        public void Process(short[] pcm)
        {
            if (pcm == null)
                return; //  throw new PicovoiceInvalidArgumentException("Null audo frame passed to Picovoice");
            if (pcm.Length != this.FrameLength)
                throw new PicovoiceInvalidArgumentException(string.Format("Invalid frame length - expected {0}, received {1}", (object)this.FrameLength, (object)pcm.Length));
            if (this._porcupine == null || this._rhino == null)
                throw new PicovoiceInvalidStateException("Cannot process frame - resources have been released.");
            if (!this._isWakeWordDetected)
            {
                int rc = this._porcupine.Process(pcm);
                this._isWakeWordDetected = (rc >= 0);

                if (!this._isWakeWordDetected)
                    return;

                this._wakeWordCallback(rc);
                if (rc != 0)
                    this._isWakeWordDetected = false;
            }
            else
            {
                if (!this._rhino.Process(pcm))
                    return;
                this._isWakeWordDetected = false;
                this._inferenceCallback(this._rhino.GetInference());
            }
        }

        public int FrameLength { get; private set; }

        public int SampleRate { get; private set; }

        public string Version => "2.1.0";

        public string PorcupineVersion { get; private set; }

        public string RhinoVersion { get; private set; }

        public string ContextInfo { get; private set; }

        private static PicovoiceException MapToPicovoiceException(Exception ex)
        {
            switch (ex)
            {
                case PorcupineActivationException _:
                case RhinoActivationException _:
                    return (PicovoiceException)new PicovoiceActivationException(ex.Message, ex);
                case PorcupineActivationLimitException _:
                case RhinoActivationLimitException _:
                    return (PicovoiceException)new PicovoiceActivationLimitException(ex.Message, ex);
                case PorcupineActivationRefusedException _:
                case RhinoActivationRefusedException _:
                    return (PicovoiceException)new PicovoiceActivationRefusedException(ex.Message, ex);
                case PorcupineActivationThrottledException _:
                case RhinoActivationThrottledException _:
                    return (PicovoiceException)new PicovoiceActivationThrottledException(ex.Message, ex);
                case PorcupineInvalidArgumentException _:
                case RhinoInvalidArgumentException _:
                    return (PicovoiceException)new PicovoiceInvalidArgumentException(ex.Message, ex);
                case PorcupineInvalidStateException _:
                case RhinoInvalidStateException _:
                    return (PicovoiceException)new PicovoiceInvalidStateException(ex.Message, ex);
                case PorcupineIOException _:
                case RhinoIOException _:
                    return (PicovoiceException)new PicovoiceIOException(ex.Message, ex);
                case PorcupineKeyException _:
                case RhinoKeyException _:
                    return (PicovoiceException)new PicovoiceKeyException(ex.Message, ex);
                case PorcupineMemoryException _:
                case RhinoMemoryException _:
                    return (PicovoiceException)new PicovoiceMemoryException(ex.Message, ex);
                case PorcupineRuntimeException _:
                case RhinoRuntimeException _:
                    return (PicovoiceException)new PicovoiceRuntimeException(ex.Message, ex);
                case PorcupineStopIterationException _:
                case RhinoStopIterationException _:
                    return (PicovoiceException)new PicovoiceStopIterationException(ex.Message, ex);
                default:
                    return new PicovoiceException(ex.Message, ex);
            }
        }
    }
}
