// using MQTTnet;
// using MQTTnet.Client.Connecting;
// using MQTTnet.Client.Disconnecting;
// using MQTTnet.Client.Options;
// using MQTTnet.Client.Receiving;
// using MQTTnet.Extensions.ManagedClient;
// using MQTTnet.Formatter;
// using MQTTnet.Protocol;
// using MQTTnet.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    /// <summary>
    /// https://github.com/SeppPenner/MQTTnet.TestApp.WinForm/blob/master/src/MQTTnet.TestApp.WinForm/Form1.cs
    /// </summary>
    // public class MQTT
    // {
    //     private IManagedMqttClient mqttClient;

    //     public Subject<String> MQTTMessagesRecevied { get; set; } = null;

    //     public void Init(string Server, int port=1883, string Username="", string Password="")
    //     {
    //         var mqttFactory = new MqttFactory();

    //         var tlsOptions = new MqttClientTlsOptions
    //         {
    //             UseTls = false,
    //             IgnoreCertificateChainErrors = true,
    //             IgnoreCertificateRevocationErrors = true,
    //             AllowUntrustedCertificates = true
    //         };

    //         var options = new MqttClientOptions
    //         {
    //             ClientId = "AlarmClockPublisher",
    //             ProtocolVersion = MqttProtocolVersion.V311,
    //             ChannelOptions = new MqttClientTcpOptions
    //             {
    //                 Server = Server,
    //                 Port = port,
    //                 TlsOptions = tlsOptions
    //             }
    //         };

    //         if (options.ChannelOptions == null)
    //         {
    //             throw new InvalidOperationException();
    //         }

    //         options.Credentials = new MqttClientCredentials
    //         {
    //             Username = Username,
    //             Password = Encoding.UTF8.GetBytes(Password)
    //         };

    //         options.CleanSession = true;
    //         options.KeepAlivePeriod = TimeSpan.FromSeconds(5);
    //         this.mqttClient = mqttFactory.CreateManagedMqttClient();
    //         this.mqttClient.UseApplicationMessageReceivedHandler(this.HandleReceivedApplicationMessage);
    //         this.mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
    //         this.mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);

    //         MQTTMessagesRecevied = new Subject<string>();

    //         Task.Run(async () =>
    //         {
    //             await this.mqttClient.StartAsync(
    //                 new ManagedMqttClientOptions
    //                 {
    //                     ClientOptions = options
    //                 });
    //         });
    //     }

    //     public async void SendMessage(string Topic, string messageString)
    //     {
    //         try
    //         {
    //             var payload = Encoding.UTF8.GetBytes(messageString);
    //             var message = new MqttApplicationMessageBuilder().WithTopic(Topic.Trim()).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag().Build();

    //             if (this.mqttClient != null)
    //             {
    //                 await this.mqttClient.PublishAsync(message);
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             MQTTMessagesRecevied.OnNext($"Error Occurred : {ex.Message}");
    //             Console.WriteLine($"Error Occurred : {ex.Message}");
    //         }
    //     }

    //     private void OnConnected(MqttClientConnectedEventArgs x)
    //     {
    //         MQTTMessagesRecevied.OnNext("OnConnected");
    //         mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(AlarmClock.Topic).Build());
    //     }

    //     private void OnDisconnected(MqttClientDisconnectedEventArgs x)
    //     {
    //         MQTTMessagesRecevied.OnNext("OnDisconnected");
    //     }

    //     private void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs x)
    //     {
    //         var item = $"HandleReceivedApplicationMessage Timestamp: {DateTime.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
    //         Console.WriteLine(item);

    //         MQTTMessagesRecevied.OnNext(x.ApplicationMessage.ConvertPayloadToString());
    //     }
    // }
}
