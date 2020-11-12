using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class TcpServer
{
    public Action<string> OnReceive;
    public Action OnConnected;
    private NetworkStream networkStream;
    
    public void StartServer(int port,Action<string,string> action)
    {
        //ListenするIPアドレス
        string ipString = GetIP();

        if (ipString == null)
        {
            Console.WriteLine("IPAdressを取得できませんでした。");
            return;
        }

        System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(ipString);

        action?.Invoke(ipString,port.ToString());

        //ホスト名からIPアドレスを取得する時は、次のようにする
        //string host = "localhost";
        //System.Net.IPAddress ipAdd =
        //    System.Net.Dns.GetHostEntry(host).AddressList[0];
        //.NET Framework 1.1以前では、以下のようにする
        //System.Net.IPAddress ipAdd =
        //    System.Net.Dns.Resolve(host).AddressList[0];


        //TcpListenerオブジェクトを作成する
        System.Net.Sockets.TcpListener listener =
            new System.Net.Sockets.TcpListener(ipAdd, port);

        //Listenを開始する
        listener.Start();
        Console.WriteLine("Listenを開始しました({0}:{1})",
            ((System.Net.IPEndPoint)listener.LocalEndpoint).Address,
            ((System.Net.IPEndPoint)listener.LocalEndpoint).Port);

        //接続要求があったら受け入れる
        System.Net.Sockets.TcpClient client = listener.AcceptTcpClient();
        Console.WriteLine("クライアント({0}:{1})と接続しました",
            ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address,
            ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port);

        //  接続成功イベント発火
        OnConnected?.Invoke();

        //NetworkStreamを取得
        networkStream = client.GetStream();

        //読み取り、書き込みのタイムアウトを10秒にする
        //デフォルトはInfiniteで、タイムアウトしない
        //(.NET Framework 2.0以上が必要)
        //ns.ReadTimeout = 10000;
        //ns.WriteTimeout = 10000;

        //クライアントから送られたデータを受信する
        System.Text.Encoding enc = System.Text.Encoding.UTF8;
        bool disconnected = false;
        System.IO.MemoryStream ms = new System.IO.MemoryStream();
        byte[] resBytes = new byte[256];
        int resSize = 0;
        int readedSize = 0;
        for (; ; )
        {
            //データの一部を受信する
            resSize = networkStream.Read(resBytes, 0, resBytes.Length);
            //Readが0を返した時はクライアントが切断したと判断
            if (resSize == 0)
            {
                disconnected = true;
                Console.WriteLine("クライアントが切断しました。");
                break;
            }
            //受信したデータを蓄積する
            ms.Write(resBytes, 0, resSize);
            //まだ読み取れるデータがあるか、データの最後が\nでない時は、

            if (!networkStream.DataAvailable || resBytes[resSize - 1] == '\n')
            {
                //受信したデータを文字列に変換
                string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.Dispose();
                ms = new System.IO.MemoryStream();

                //末尾の\nを削除
                resMsg = resMsg.TrimEnd('\n');
                OnReceive?.Invoke(resMsg);

                if (!disconnected)
                {
                    //クライアントにデータを送信する
                    //クライアントに送信する文字列を作成
                    //string sendMsg = resMsg.Length.ToString();
                    //文字列をByte型配列に変換
                    //byte[] sendBytes = enc.GetBytes(sendMsg + '\n');
                    //データを送信する
                    //networkStream.Write(sendBytes, 0, sendBytes.Length);
                    //Console.WriteLine("返信:" + sendMsg);
                }
            }
        }

        ms.Close();

        //閉じる
        networkStream.Close();
        client.Close();
        Console.WriteLine("クライアントとの接続を閉じました。");

        //リスナを閉じる
        listener.Stop();
        Console.WriteLine("Listenerを閉じました。");


        //  再処理するか
        //Console.WriteLine("再度サーバーを起動しますか？(Y/N)");
        //var s = Console.ReadLine();

        //if (s == "Y" || s == "y")
            StartServer(port, action);
    }

    //クライアントにデータを送信する
    public void SendData(string msg)
    {
        //エンコード指定
        System.Text.Encoding enc = System.Text.Encoding.UTF8;
        //クライアントに送信する文字列を作成
        string sendMsg = msg.Length.ToString();
        //文字列をByte型配列に変換
        byte[] sendBytes = enc.GetBytes(msg + '\n');
        //データを送信する
        networkStream.Write(sendBytes, 0, sendBytes.Length);
    }

    private string GetIP()
    {
        // ホスト名を取得する
        string hostname = Dns.GetHostName();
        string retIpAdder = "";
        // ホスト名からIPアドレスを取得する
        IPAddress[] adrList = Dns.GetHostAddresses(hostname);
        foreach (IPAddress address in adrList)
        {
            if (address.ToString().Contains("192.168.0."))
                return address.ToString();
        }

        return null;
    }
}