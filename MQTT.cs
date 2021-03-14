using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace AlarmClock
{
    /// <summary>
    /// https://github.com/SeppPenner/MQTTnet.TestApp.WinForm/blob/master/src/MQTTnet.TestApp.WinForm/Form1.cs
    /// </summary>
    public class MQTT
    {
        /// <summary>
        /// The managed publisher client.
        /// </summary>
        private IManagedMqttClient managedMqttClientPublisher;

        /// <summary>
        /// The managed subscriber client.
        /// </summary>
        private IManagedMqttClient managedMqttClientSubscriber;

        /// <summary>
        /// The MQTT server.
        /// </summary>
        private IMqttServer mqttServer;

        /// <summary>
        /// The port.
        /// </summary>
        private string port = "1883";

        public Subject<String> MQTTMessagesRecevied { get; set; } = null;

        public MQTT()
        {            
        }


        public async void Init()
        {
            var mqttFactory = new MqttFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = "ClientPublisher",
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "192.168.0.18",
                    Port = 1883,
                    TlsOptions = tlsOptions
                }
            };

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            options.Credentials = new MqttClientCredentials
            {
                Username = "",
                Password = Encoding.UTF8.GetBytes("")
            };

            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(5);
            this.managedMqttClientPublisher = mqttFactory.CreateManagedMqttClient();
            this.managedMqttClientPublisher.UseApplicationMessageReceivedHandler(this.HandleReceivedApplicationMessage);
            this.managedMqttClientPublisher.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnPublisherConnected);
            this.managedMqttClientPublisher.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnPublisherDisconnected);

            this.managedMqttClientSubscriber = mqttFactory.CreateManagedMqttClient();
            this.managedMqttClientSubscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnSubscriberConnected);
            this.managedMqttClientSubscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnSubscriberDisconnected);
            this.managedMqttClientSubscriber.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(this.OnSubscriberMessageReceived);

            MQTTMessagesRecevied = new Subject<string>();

            await Task.Run(async () =>
            {
                await this.managedMqttClientPublisher.StartAsync(
                    new ManagedMqttClientOptions
                    {
                        ClientOptions = options
                    });

                await this.managedMqttClientSubscriber.StartAsync(
                    new ManagedMqttClientOptions
                    {
                        ClientOptions = options
                    });
            });
        }


        public async void SendMessage(string Topic, string messageString)
        {
            try
            {
                var payload = Encoding.UTF8.GetBytes(messageString);
                var message = new MqttApplicationMessageBuilder().WithTopic(Topic.Trim()).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag().Build();

                if (this.managedMqttClientPublisher != null)
                {
                    await this.managedMqttClientPublisher.PublishAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occurred : {ex.Message}");
            }
        }

        private static void OnPublisherConnected(MqttClientConnectedEventArgs x)
        {
            Console.WriteLine($"OnPublisherConnected");
        }

        private static void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
        {
            Console.WriteLine($"OnPublisherDisconnected");
        }



        private static void OnSubscriberConnected(MqttClientConnectedEventArgs x)
        {
            Console.WriteLine($"OnSubscriberConnected");
        }


        private static void OnSubscriberDisconnected(MqttClientDisconnectedEventArgs x)
        {
            Console.WriteLine($"OnSubscriberDisconnected");
        }

        private void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs x)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
            Console.WriteLine($"HandleReceivedApplicationMessage {item}");

            MQTTMessagesRecevied.OnNext(item);
        }

        private void OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
            Console.WriteLine($"OnSubscriberMessageReceived {item}");

            MQTTMessagesRecevied.OnNext(item);
        }
    }
}
