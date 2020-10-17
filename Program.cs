using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WindowsVolumeController
{
    [JsonObject]
    class VolumeData
    {
        public string DeviceName { get; set; }

        [JsonIgnore]
        public NAudio.CoreAudioApi.AudioEndpointVolume MasterVolume;

        public List<VolumeChannelData> VolumeChannel = new List<VolumeChannelData>();

    }
    [JsonObject]
    class VolumeChannelData
    {
        public uint ID { get; set; }
        public string ChannelName { get; set; }

        [JsonIgnore]
        public NAudio.CoreAudioApi.SimpleAudioVolume volume { get; set; }
    }



    class Program
    {
        private static TcpServer server;

        static void Main(string[] args)
        {

            server = new TcpServer();

            //  受信イベント設定
            server.OnReceive += CheckReceiveValue;

            //  接続待機開始
            server.StartServer(8888);
        }

        //  受信した値を振り分け
        private static void CheckReceiveValue(string getMessage)
        {
            if (getMessage == "GET_VOLUME")
            {
                var data = GetVolumeState();
                var sendData = JsonConvert.SerializeObject(data);
                server.SendData(sendData);

                Console.WriteLine(sendData);
            }
        }

        private static void ChangeVolume(string volumeStr)
        {
            var data = GetVolumeState();

            Console.WriteLine(data);
        }


        private static VolumeData GetVolumeState()
        {
            //オーディオデバイスを見つけるためのEnumeratorのインスタンス化 
            NAudio.CoreAudioApi.MMDeviceEnumerator MMDE = new NAudio.CoreAudioApi.MMDeviceEnumerator();

            //  今使用されているデバイスを取得する
            var device = MMDE.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Console);

            VolumeData volumeData = new VolumeData();

            try
            {
                //  デバイス名を取得
                volumeData.DeviceName = device.DeviceFriendlyName;

                try
                {
                    //  マスターボリュームを取得
                    volumeData.MasterVolume = device.AudioEndpointVolume;

                    //  デバイスのミキサー内の音量調整
                    //  音を出力しているプロセス数
                    int sessionCount = device.AudioSessionManager.Sessions.Count;

                    for (int i = 0; i < sessionCount; i++)
                    {
                        //  プロセスIDを取得
                        var processId = device.AudioSessionManager.Sessions[i].GetProcessID;

                        //  すでに取得したIDの場合スキップする
                        bool checkFlag = false;
                        foreach (var channel in volumeData.VolumeChannel)
                        {
                            if (channel.ID == processId) { checkFlag = true; break; }
                        }

                        if (checkFlag) continue;

                        //  プロセスIDからプロセス名を取得
                        var process = Process.GetProcessById((int)processId);
                        var processName = process.ProcessName;

                        volumeData.VolumeChannel.Add(new VolumeChannelData
                        {
                            ID = processId,
                            ChannelName = processName,
                            volume = device.AudioSessionManager.Sessions[i].SimpleAudioVolume
                        });

                        //  プロセス単位のボリュームを設定
                        //  1 : マスターボリュームと同じ
                        var localVolume = device.AudioSessionManager.Sessions[i].SimpleAudioVolume.Volume;

                        Console.WriteLine(processName + ":" + localVolume);

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("エラー:" + ex);
                }

            }
            catch (System.FormatException ex)
            {
                Console.WriteLine("正しい値を入力してください");
            }
            catch
            {

            }

            return volumeData;
        }
    }
}
