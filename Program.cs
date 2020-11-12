using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;

namespace WindowsVolumeController
{
    [JsonObject]
    class VolumeData
    {
        public string DeviceName { get; set; }

        [JsonIgnore]
        public NAudio.CoreAudioApi.AudioEndpointVolume MasterVolume;

        public float DeviceVolume { get { return MasterVolume.MasterVolumeLevelScalar; } }

        public List<VolumeChannelData> VolumeChannel = new List<VolumeChannelData>();

    }
    [JsonObject]
    class VolumeChannelData
    {
        public uint ID { get; set; }
        public string ChannelName { get; set; }
        public float ChannelVolume { get { return volume.Volume; } }

        [JsonIgnore]
        public NAudio.CoreAudioApi.SimpleAudioVolume volume { get; set; }
    }



    class Program
    {
        private static TcpServer server;
        private static VolumeData volumeData;

        static void Main(string[] args)
        {

            server = new TcpServer();

            //  受信イベント設定
            server.OnReceive += CheckReceiveValue;
            server.OnConnected += () =>
            {

                if (app != null)
                {
                    app.Close();
                    app.Dispose();
                    app = null;
                }

            };

            //  接続待機開始
            server.StartServer(8888, (ip, port) => ShowQRForm(ip + "," + port));

        }
        static Form app = null;

        public static void ShowQRForm(string str)
        {
            app = new Form();
            app.FormBorderStyle = FormBorderStyle.None;

            // PictureBoxの中心に画像を表示するように設定
            var pic = new PictureBox();
            pic.SizeMode = PictureBoxSizeMode.CenterImage;
            pic.Image = CreateQRCode(str);

            Console.WriteLine();

            pic.Location = new Point(0, 0);
            pic.Size = new Size(app.Size.Width - app.PreferredSize.Width, app.Size.Height - app.PreferredSize.Height);
            app.Controls.Add(pic);


            app.Show();


            //  ディスプレイの高さ
            int displayHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            //  ディスプレイの幅
            int displayWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;

            //  ウィンドウ幅
            int appWidth = app.Size.Width;
            //  ウィンドウ高さ
            int appHeight = app.Size.Height;

            //  配置する位置
            int setLocationWidth = displayWidth - appWidth;
            int setLocationHeight = displayHeight - appHeight;

            //  アプリウィンドウを動かす
            app.Location = new Point(setLocationWidth, setLocationHeight);


        }

        private static Bitmap CreateQRCode(string writeStr)
        {
            BarcodeWriter qrcode = new BarcodeWriter
            {
                // 出力するコードの形式をQRコードに選択
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.QrCode.QrCodeEncodingOptions
                {
                    // QRコードの信頼性
                    ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
                    // 日本語を表示したい場合シフトJISを指定
                    //CharacterSet = "Shift_JIS",
                    // デフォルト
                    CharacterSet = "ISO-8859-1",
                    // QRコードのサイズ決定
                    Height = 250,
                    Width = 250,
                    // QRコード周囲の余白の大きさ
                    Margin = 0
                }
            };
            // QRコードを出力
            return qrcode.Write(writeStr);
        }

        //  受信した値を振り分け
        private static void CheckReceiveValue(string getMessage)
        {
            if (getMessage == "GET_VOLUME")
            {
                volumeData = GetVolumeState();
                var sendData = JsonConvert.SerializeObject(volumeData);
                server.SendData(sendData);

                Console.WriteLine(sendData);
            }
            else
            {
                var getData = getMessage.Split(',');

                try
                {
                    if (getData[0] == volumeData.DeviceName)
                        volumeData.MasterVolume.MasterVolumeLevelScalar = float.Parse(getData[1]);

                    foreach (var data in volumeData.VolumeChannel)
                    {
                        if (getData[0] == data.ChannelName)
                            data.volume.Volume = float.Parse(getData[1]);
                    }
                }
                catch
                {
                    //  送られてくる値が早すぎて処理しきれていない為に起こるエラーな気がする
                    //Debug.WriteLine("エラー:" + getMessage);
                }

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
