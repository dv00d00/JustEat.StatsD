using System;
using System.Buffers;
using System.Net.Sockets;
using System.Text;
#if !NET451
using System.Runtime.InteropServices;
#endif
using JustEat.StatsD.EndpointLookups;

namespace JustEat.StatsD
{
    public class UdpTransport : IStatsDTransport
    {
        private readonly IPEndPointSource _endpointSource;

        public UdpTransport(IPEndPointSource endPointSource)
        {
            _endpointSource = endPointSource ?? throw new ArgumentNullException(nameof(endPointSource));
        }

        public void Send(string metric)
        {
            var rent = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(metric.Length));
            var bytes = Encoding.UTF8.GetBytes(metric, rent);

            var endpoint = _endpointSource.GetEndpoint();

            using (var socket = CreateSocket())
            {
                socket.SendTo(rent, bytes, SocketFlags.None, endpoint);
            }

            ArrayPool<byte>.Shared.Return(rent);
        }

        private static Socket CreateSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

#if !NET451
            // See https://github.com/dotnet/corefx/pull/17853#issuecomment-291371266
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                socket.SendBufferSize = 0;
            }
#else
            socket.SendBufferSize = 0;
#endif

            return socket;
        }
    }
}
