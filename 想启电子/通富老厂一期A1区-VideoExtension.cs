using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XiangQi.Common;
using XiangQi.Log.Log4net;

namespace XiangQi.Ros.Video
{
    /// <summary>
    /// 视频控制
    /// </summary>
    public class VideoExtension : IDisposable
    {
        Socket udpClient;
        Socket tcpClient;
        private VideoType _videoType;

        private byte[] videoUdpBuffer;
        private byte[] videoTcpBuffer;
        //private string videoReceiveData;

        private readonly byte[] videoValideBuffer = Encoding.UTF8.GetBytes("{GetWindows;-1}");

        /// <summary>
        /// 视频切换到本地
        /// </summary>
        public Action VideoConnectLocalDelegate;

        private readonly string _udpInputIp;
        private string _udpOutputIp;
        private int _udpOutputPort;
        private readonly bool _udpKvm;
        private readonly string _tcpIp;
        private readonly int _tcpPort;
        private readonly int _tcpVideoOutputPort;
        private readonly int _tcpVideoInputPort;
        bool kvmEnable = true;

        readonly System.Timers.Timer _pollingCheckSocketTimer;

        /// <sumreadonly mary>
        /// 
        /// </summary>
        public VideoExtension()
        {
            _pollingCheckSocketTimer = new System.Timers.Timer
            {
                AutoReset = true,
                Enabled = false,
                Interval = 1000
            };
            _pollingCheckSocketTimer.Elapsed += TimerEventProcessor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoType"></param>
        /// <param name="udpInputIp"></param>
        /// <param name="udpOutputIp"></param>
        /// <param name="udpOutputPort"></param>
        /// <param name="udpKvm"></param>
        /// <param name="tcpIp"></param>
        /// <param name="tcpPort"></param>
        /// <param name="tcpVideoOutputPort"></param>
        /// <param name="tcpVideoInputPort"></param>
        public VideoExtension(string localIp, VideoType videoType, string udpInputIp, string udpOutputIp, int udpOutputPort, bool udpKvm,
            string tcpIp, int tcpPort, int tcpVideoOutputPort, int tcpVideoInputPort) : this()
        {
            _videoType = videoType;
            _udpInputIp = udpInputIp;
            _udpOutputIp = udpOutputIp;
            _udpOutputPort = udpOutputPort <= 0 ? 41234 : udpOutputPort;
            _udpKvm = udpKvm;
            _tcpIp = tcpIp;
            _tcpPort = tcpPort <= 0 ? 60090 : tcpPort;
            _tcpVideoOutputPort = tcpVideoOutputPort;
            _tcpVideoInputPort = tcpVideoInputPort;
            switch (_videoType)
            {
                case VideoType.Net:
                    InitUdpSocket(localIp);
                    break;
                case VideoType.Video:
                    TcpConnect();
                    break;
                case VideoType.NetAndVideo:
                    InitUdpSocket(localIp);
                    TcpConnect();
                    break;
                default:
                    break;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="machineIp">远程设备IP</param>
        /// <param name="machinePort">远程设备端口</param>
        public bool VideoChange(string machineIp, int machinePort)
        {
            switch (_videoType)
            {
                case VideoType.Net:
                    return UdpVideoChange(machineIp) == 1;
                case VideoType.Video:
                    return TcpVideoChange(machinePort) == 1;
                case VideoType.NetAndVideo:
                    return TcpUdpVideoChange(machinePort, machineIp) == 1;
                default:
                    break;
            }
            return false;
        }

        #region UDP
        /// <summary>
        /// UDP初始化连接
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void InitUdpSocket(string ip, int port = 8891)
        {
            try
            {
                LogHelper.Info($"初始化绑定UDP连接-{ip}:{port}");
                udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                udpClient.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

                udpClient.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoIp"></param>
        /// <returns></returns>
        private int UdpVideoChange(string videoIp)
        {
            if (string.IsNullOrEmpty(_udpOutputIp))
            {
                LogHelper.Info($"udpOutputIp为空。");
                return -1;
            }

            videoUdpBuffer = Encoding.UTF8.GetBytes("{Connect;" + videoIp + "}"); //{Connect;x.x.x.x}
            var beginTime = DateTime.Now;
            double ts1 = 0;
            int result = 0;
            var remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0); //用来保存发送方的ip和端口号
            var sendEndPoint = new IPEndPoint(IPAddress.Parse(_udpOutputIp), _udpOutputPort);

            while (ts1 <= 4000 && result == 0)
            {
                ts1 = (DateTime.Now - beginTime).TotalMilliseconds;
                Thread.Sleep(2);
                if (!SendUdpMessage(sendEndPoint))
                {
                    return -2;
                }
                Thread.Sleep(2);
                result = ReceiveUdpMessage(remoteIpEndPoint, videoIp);
            }

            LogHelper.Info($"UDP连接{(result == 1? "成功" : "失败")}-{result}-{_udpOutputIp}:{_udpOutputPort}");

            if (_udpKvm)
            {
                Task.Run(() =>
                {
                    ValidUdpKvm(new IPEndPoint(IPAddress.Parse(_udpOutputIp), _udpOutputPort));
                });
            }
            return result;
        }

        /// <summary>
        /// 发送UDP连接信息
        /// </summary>
        private bool SendUdpMessage(EndPoint point)
        {
            try
            {
                LogHelper.Info($"{Encoding.UTF8.GetString(videoUdpBuffer)}---{Encoding.UTF8.GetString(videoValideBuffer)}");
                udpClient.SendTo(videoUdpBuffer, point);
                Thread.Sleep(10);
                udpClient.SendTo(videoValideBuffer, point);                
            }
            catch (Exception ex)
            {                
                LogHelper.Error(ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 处理UDP推送过来的消息
        /// </summary>
        /// <param name="rec"></param>
        private int ReceiveUdpMessage(EndPoint point, string videoIp)
        {            
            byte[] buffer = new byte[1024];
            try
            {
                udpClient.ReceiveTimeout = 5000;
                if (udpClient.Available <= 0) return 0;
                int length = udpClient.ReceiveFrom(buffer, ref point); //接收数据报
                string message = Encoding.UTF8.GetString(buffer, 0, length); //{GetWindows;-1;192.168.2.200,rtsp://192.168.2.200/0,0,0,1920,1080}
                LogHelper.Info($"{message}---{videoIp}");
                return message.Contains(videoIp) ? 1 : 0;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return -3;
            }
        }

        /// <summary>
        /// 验证远程UDP是Kvm模式
        /// </summary>
        private void ValidUdpKvm(EndPoint point)
        {
            LogHelper.Info($"UDP Kvm start");
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0); //用来保存发送方的ip和端口号
            byte[] buffer = new byte[1024];
            kvmEnable = true;
            while (kvmEnable)
            {
                Thread.Sleep(10);
                try
                {
                    udpClient.SendTo(videoValideBuffer, point);
                    udpClient.ReceiveTimeout = 500;
                    if (udpClient.Available <= 0) continue;

                    int length = udpClient.ReceiveFrom(buffer, ref endPoint); //接收数据报
                    string message = Encoding.UTF8.GetString(buffer, 0, length); //{GetWindows;-1;192.168.2.200,rtsp://192.168.2.200/0,0,0,1920,1080}
                    if (message.Contains($"{_udpInputIp},"))
                    {
                        kvmEnable = false;
                        LogHelper.Info($"UDP Kvm成功-开始切换到本地--{_udpInputIp}");
                        VideoConnectLocalDelegate?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    kvmEnable = false;
                    LogHelper.Error(ex);
                }
            }
        }

        #endregion

        #region TCP        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void TimerEventProcessor(object source, EventArgs e)
        {
            var connected = IsConnected();
            if (!connected)
            {
                tcpClient.Close();
                TcpConnect();
            }
        }

        /// <summary>
        /// 检查一个Socket是否可连接
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private bool IsConnected()
        {
            if (tcpClient == null || tcpClient.Connected == false)
            {
                LogHelper.Info("1");
                return false;
            }

            bool blockingState = tcpClient.Blocking;
            try
            {
                byte[] tmp = new byte[1] { 0x00 };
                tcpClient.Blocking = false;
                tcpClient.Send(tmp);
                return true;
            }
            catch (SocketException e)
            {
                // 产生 10035 == WSAEWOULDBLOCK 错误，说明被阻止了，但是还是连接的
                if (e.NativeErrorCode.Equals(10035))
                {
                    return true;
                }
                else
                {
                    LogHelper.Info("2");
                    return false;
                }
            }
            finally
            {
                tcpClient.Blocking = blockingState;    // 恢复状态
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="port"></param>
        private int TcpVideoChange(int tcpVideoInputPort)
        {
            videoTcpBuffer = Encoding.UTF8.GetBytes($"SW {_tcpVideoOutputPort} {tcpVideoInputPort}\r");
            tcpClient.Send(videoTcpBuffer);
            Thread.Sleep(10);
            tcpClient.Send(videoTcpBuffer);
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        private bool TcpConnect()
        {
            tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                LogHelper.Info($"TcpConnect-{_tcpIp}:{_tcpPort}");
                tcpClient.Connect(IPAddress.Parse(_tcpIp), _tcpPort);
                _pollingCheckSocketTimer.Start();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return false;
            }

            return tcpClient.Connected;
        }
        #endregion

        #region UDP&TCP

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpVideoInputPort"></param>
        /// <param name="machineIp"></param>
        /// <returns></returns>
        public int TcpUdpVideoChange(int tcpVideoInputPort, string machineIp)
        {
            var tcpResult = TcpVideoChange(tcpVideoInputPort);
            var udpResult = UdpVideoChange(machineIp);

            if (_udpKvm)
            {
                Task.Run(() =>
                {
                    ValidUdpKvm(new IPEndPoint(IPAddress.Parse(_udpOutputIp), _udpOutputPort));
                });
            }

            return tcpResult | udpResult;
        }

        #endregion

        /// <summary>
        /// 切换到本地视频
        /// </summary>
        public void VideoChangeLocal()
        {
            LogHelper.Info($"开始切换视频到本地");
            if (_videoType == VideoType.Net || _videoType == VideoType.NetAndVideo)
            {
                LogHelper.Info($"UDP-{_udpOutputIp}:{_udpOutputPort}--输入IP:{_udpInputIp}");
                var result = UdpVideoChange(_udpInputIp);
                LogHelper.Info(result == 1 ? "切换视频到本地成功" : $"切换视频到本地失败-{result}");
            }
            if (_videoType == VideoType.Video || _videoType == VideoType.NetAndVideo)
            {
                LogHelper.Info($"TCP-{_tcpIp}:{_tcpPort}--输入端口:{_tcpVideoInputPort}");
                var result = TcpVideoChange(_tcpVideoInputPort);
                LogHelper.Info(result == 1 ? "切换视频到本地成功" : $"切换视频到本地失败-{result}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            LogHelper.Info("清理视频对象");
            kvmEnable = false;
            _pollingCheckSocketTimer.Stop();
            udpClient?.Close();
            udpClient?.Dispose();
            tcpClient?.Close();
            tcpClient?.Dispose();
        }
    }
}
