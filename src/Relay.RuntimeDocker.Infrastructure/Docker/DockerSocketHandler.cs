using System.Net.Sockets;

namespace Relay.RuntimeDocker.Infrastructure.Docker;

internal static class DockerSocketHandler
{
    public static SocketsHttpHandler Create(string socketPath) => new()
    {
        ConnectCallback = async (_, cancellationToken) =>
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };
}
