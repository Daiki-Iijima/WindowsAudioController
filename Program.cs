using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsVolumeController
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new TcpServer();

            //  受信イベント設定
            server.OnReceive += ChangeVolume;

            //  接続待機開始
            server.StartServer(8888);
        }

        private static void ChangeVolume(string volumeStr)
        {
            //オーディオデバイスを見つけるためのEnumeratorのインスタンス化 
            NAudio.CoreAudioApi.MMDeviceEnumerator MMDE = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            //状態やステータスに関係なく、すべてのデバイスを取得する
            NAudio.CoreAudioApi.MMDeviceCollection DevCol = MMDE.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.DeviceState.Active);

            try
            {
                float volume = float.Parse(volumeStr);

                foreach (NAudio.CoreAudioApi.MMDevice dev in DevCol)
                {
                    Console.WriteLine("=== " + "デバイス : " + dev.DeviceFriendlyName + "===");

                    try
                    {
                        //  マスターボリュームを取得
                        dev.AudioEndpointVolume.MasterVolumeLevelScalar = volume;

                        //  デバイスのミキサー内の音量調整
                        //  音を出力しているプロセス数
                        int sessionCount = dev.AudioSessionManager.Sessions.Count;

                        for (int i = 0; i < sessionCount; i++)
                        {
                            //  プロセスIDを取得
                            var processId = dev.AudioSessionManager.Sessions[i].GetProcessID;

                            //  プロセスIDからプロセス名を取得
                            var dat = Process.GetProcessById((int)processId);

                            Console.WriteLine(processId + ":" + dat.ProcessName);

                            //  プロセス単位のボリュームを設定
                            //  1 : マスターボリュームと同じ
                            dev.AudioSessionManager.Sessions[i].SimpleAudioVolume.Volume = 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Do something with exception when an audio endpoint could not be muted 
                    }
                }

            }
            catch (System.FormatException ex)
            {
                Console.WriteLine("正しい値を入力してください");
            }
            catch
            {

            }
        }

    }
}
