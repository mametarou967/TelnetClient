using TelnetClient.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace TelnetClient.Model.SerialCom
{
    public class TelNetClient
    {
        private string hostname;
        Task serialComTask;
        private CancellationTokenSource cancellationTokenSource;
        // private SerialPort serialPort;
        ConcurrentQueue<byte> sendQueue = new ConcurrentQueue<byte>();
        ILogWriteRequester logWriteRequester;
        Action<byte[]> dataReceiveAction;

        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private StreamReader streamReader;
        private StreamWriter streamWriter;

        public bool IsCommunicating
        {
            get
            {
                if (tcpClient == null) return false;
                return tcpClient.Connected;
            }
        }


        public TelNetClient(
            string hostname,
            Action<byte[]> dataReceiveAction,
            ILogWriteRequester logWriteRequester)
        {
            this.hostname = "192.168.2.87";
            this.dataReceiveAction = dataReceiveAction;
            this.logWriteRequester = logWriteRequester;
        }

        public bool StartCom()
        {
            if (String.IsNullOrEmpty(hostname))
            {
                logWriteRequester.WriteRequest(
                LogLevel.Error,
                $"hostnameが空白やnullのため通信を開始することができません");
                return false;
            }

            try
            {
                tcpClient = new TcpClient(hostname,23);
                networkStream = tcpClient.GetStream();
                streamReader = new StreamReader(networkStream, Encoding.ASCII);
                streamWriter = new StreamWriter(networkStream, Encoding.ASCII);
            }
            catch (Exception ex)
            {
                logWriteRequester.WriteRequest(
                LogLevel.Error,
                $"[例外-ﾒｯｾｰｼﾞ] " + $"{ex.Message} ");
                logWriteRequester.WriteRequest(
                LogLevel.Error,
                $"[例外-ｱﾌﾟﾘｹｰｼｮﾝ] " + $"{ex.Source} ");
                logWriteRequester.WriteRequest(
                LogLevel.Error,
                $"[例外-ｽﾀｯｸﾄﾚｰｽ] " + $"{ex.StackTrace} ");
                logWriteRequester.WriteRequest(
                LogLevel.Error,
                $"[例外-ﾒｿｯﾄﾞ] " + $"{ex.TargetSite} ");

                return false;
            }

            logWriteRequester.WriteRequest(
            LogLevel.Info,
            $"[TCP-TELNET-START1] " +
            $"LocalEndPoint:{tcpClient.Client.LocalEndPoint.ToString()} ");

            logWriteRequester.WriteRequest(
            LogLevel.Info,
            $"[TCP-TELNET--START2] " +
            $"LocalEndPoint:{tcpClient.Client.RemoteEndPoint.ToString()} ");

            cancellationTokenSource = new CancellationTokenSource();

            serialComTask = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var sb = new StringBuilder();

                    while(streamReader.Peek() != -1)
                    {
                        sb.Append(streamReader.Read());
                    }

                    var response = sb.ToString();

                    if ((response != null) && (response.Count() > 0))
                    {
                        // byte[] receivedBytes = new byte[serialPort.BytesToRead];
                        // streamReader.Read(receivedBytes, 0, receivedBytes.Length);

                        // 受信データバイナリログ出力
                        logWriteRequester.WriteRequest(LogLevel.Debug, "[RCV] " + string.Join(" ", response));

                        // dataReceiveAction?.Invoke(new byte[] { }); // 仮に0詰め
                    }

                    var sendList = new List<byte>();
                    // sendQueueを調べてデータがあればserialPortに送信する
                    while (!sendQueue.IsEmpty)
                    {
                        if (sendQueue.TryDequeue(out byte data))
                        {
                            sendList.Add(data);
                        }
                    }

                    if (sendList.Count != 0)
                    {
                        streamWriter.WriteLine("aaa", 0, "aaa".Count());

                        // 送信データバイナリログ出力
                        logWriteRequester.WriteRequest(LogLevel.Debug, "[SND] " + string.Join(" ", "aaa"));
                    }

                    await Task.Delay(100); // Adjust the delay duration as needed
                }
            });

            return true;
        }

        public bool StopCom()
        {
            bool result = true;

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                try
                {
                    var waitResult = serialComTask.Wait(5000);
                    if (!waitResult) result = false;
                }
                catch
                {
                }
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            if (tcpClient != null && tcpClient.Connected)
            {
                logWriteRequester.WriteRequest(
                LogLevel.Info,
                $"[TCP-TELNET-STOP1] " +
                $"LocalEndPoint:{tcpClient.Client.LocalEndPoint.ToString()} ");

                logWriteRequester.WriteRequest(
                LogLevel.Info,
                $"[TCP-TELNET-STOP2] " +
                $"LocalEndPoint:{tcpClient.Client.RemoteEndPoint.ToString()} ");

                streamWriter.Close();
                streamReader.Close();
                networkStream.Close();
                tcpClient.Close();
                tcpClient.Close();
                tcpClient.Dispose();
                tcpClient = null;
            }

            return result;
        }

        public void Send(IEnumerable<byte> datas)
        {
            datas.ToList().ForEach((data) => sendQueue.Enqueue(data));
        }
    }
}
