using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApp_NTP
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello, World!");
            string ntpServer = "ntp.aliyun.com";
            Console.Write("请输入ntp服务器地址");
            ntpServer = Console.ReadLine();
            while (!IsIpAddress(ntpServer) && !IsDomainName(ntpServer))
            {
                Console.WriteLine("输入的地址不是合法的IP地址或域名，请重新输入");
                ntpServer = Console.ReadLine();
            }
            while (!string.IsNullOrWhiteSpace(ntpServer))
            {
                const int ntpPort = 123;
                // NTP 协议版本 4 客户端模式的请求头
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; // LI=0, VN=3, Mode=3 (Client) -> 二进制 00 011 011 -> 十六进制 0x1B

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    // 设置超时，防止无限等待
                    socket.ReceiveTimeout = 30 * 1000;
                    socket.SendTimeout = 3000;

                    // 解析服务器地址并连接
                    var ipAddress = IPAddress.None;
                    if (IsDomainName(ntpServer))
                    {
                        ipAddress = System.Net.Dns.GetHostEntry(ntpServer).AddressList[0];

                    }
                    else
                    {
                        ipAddress = IPAddress.Parse(ntpServer);
                    }
                    socket.Connect(ipAddress, ntpPort);

                    // 发送请求
                    socket.Send(ntpData);

                    // 接收响应
                    socket.Receive(ntpData);
                }
                // 解析服务器返回的时间
                // 最关键的时间戳位于缓冲区的 [40] 到 [47] 的位置（8字节，64位）
                ulong intPart = BinaryPrimitives.ReadUInt32BigEndian(ntpData.AsSpan(40));
                ulong fractPart = BinaryPrimitives.ReadUInt32BigEndian(ntpData.AsSpan(44));

                // NTP 时间是从 1900年1月1日 开始的
                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

                // 转换为 .NET DateTime (UTC)
                // DateTime.UnixEpoch 是 1970-01-01，需要加上 1900 到 1970 之间的秒数差（2208988800秒）
                var networkDateTime = DateTime.UnixEpoch.AddMilliseconds(milliseconds)
                                                       .AddSeconds(-2208988800);
                var localTime = networkDateTime.ToLocalTime(); // 转换为本地时间
                Console.WriteLine($"NTP 服务器时间 (UTC): {networkDateTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"NTP 服务器时间 (Local): {localTime:yyyy-MM-dd HH:mm:ss}");
                _ = Console.ReadLine();
            }

        }
        public static bool IsIpAddress(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var hostType = Uri.CheckHostName(input);
            return hostType == UriHostNameType.IPv4 || hostType == UriHostNameType.IPv6;
        }

        public static bool IsDomainName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var hostType = Uri.CheckHostName(input);
            return hostType == UriHostNameType.Dns;
        }
    }
}
