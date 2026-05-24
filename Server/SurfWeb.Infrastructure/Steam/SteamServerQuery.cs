using System.Net;
using System.Net.Sockets;
using System.Text;

using SurfWeb.Application.Abstractions;

namespace SurfWeb.Infrastructure.Steam;

public sealed class SteamServerQueryService : ISteamServerQuery
{
    public SteamQueryServerInfo QueryServer(string host, int port, int timeoutMs)
    {
        var info = SteamServerQuery.QueryServer(host, port, timeoutMs);
        return new SteamQueryServerInfo(info.Name, info.Map, info.Players, info.MaxPlayers);
    }

    public IReadOnlyList<SteamQueryPlayerInfo> QueryPlayers(string host, int port, int timeoutMs) =>
        SteamServerQuery.QueryPlayers(host, port, timeoutMs)
            .Select(p => new SteamQueryPlayerInfo(p.Name, p.DurationSeconds))
            .ToList();
}

internal static class SteamServerQuery
{
    internal static SteamServerInfo QueryServer(string ip, int port, int timeout = 3000)
    {
        var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        using var udp = new UdpClient();
        udp.Client.SendTimeout = timeout;
        udp.Client.ReceiveTimeout = timeout;
        udp.Client.ReceiveBufferSize = 4096;

        byte[] request =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69,
            0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
        };
        udp.Send(request, request.Length, endPoint);

        IPEndPoint? remote = null;
        byte[] response;
        try
        {
            response = udp.Receive(ref remote);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            throw new InvalidOperationException("服务器响应超时");
        }

        if (response.Length >= 5 && response[4] == 0x41)
        {
            if (response.Length < 9)
                throw new InvalidOperationException("无效的挑战响应");

            var challenge = new byte[4];
            Buffer.BlockCopy(response, 5, challenge, 0, 4);

            var newRequest = new byte[request.Length + 4];
            Buffer.BlockCopy(request, 0, newRequest, 0, request.Length);
            Buffer.BlockCopy(challenge, 0, newRequest, request.Length, 4);

            udp.Send(newRequest, newRequest.Length, endPoint);
            response = udp.Receive(ref remote);
        }

        if (response.Length < 5 || response[4] != 0x49)
            throw new InvalidOperationException($"无效的服务器响应 (头部: 0x{(response.Length >= 5 ? response[4] : 0):X2})");

        var reader = new ResponseReader(response, 5);
        _ = reader.ReadByte();
        var name = reader.ReadString();
        var map = reader.ReadString();
        _ = reader.ReadString();
        _ = reader.ReadString();
        var players = reader.ReadInt16();
        var maxPlayers = reader.ReadInt16();

        return new SteamServerInfo(name, map, players, maxPlayers);
    }

    internal static List<SteamPlayerInfo> QueryPlayers(string ip, int port, int timeout = 3000)
    {
        var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        using var udp = new UdpClient();
        udp.Client.SendTimeout = timeout;
        udp.Client.ReceiveTimeout = timeout;
        udp.Client.ReceiveBufferSize = 4096;

        var challenge = GetChallengeValue(udp, endPoint);

        var request = new byte[9];
        request[0] = 0xFF;
        request[1] = 0xFF;
        request[2] = 0xFF;
        request[3] = 0xFF;
        request[4] = 0x55;
        Buffer.BlockCopy(challenge, 0, request, 5, 4);

        udp.Send(request, request.Length, endPoint);

        IPEndPoint? remote = null;
        var response = udp.Receive(ref remote);

        if (response.Length < 5 || response[4] != 0x44)
            throw new InvalidOperationException($"无效的玩家列表响应 (头部: 0x{(response.Length >= 5 ? response[4] : 0):X2})");

        var players = new List<SteamPlayerInfo>();
        var reader = new ResponseReader(response, 5);

        var playerCount = reader.ReadByte();
        for (var i = 0; i < playerCount; i++)
        {
            _ = reader.ReadByte();
            var name = reader.ReadString();
            _ = reader.ReadInt32();
            var duration = reader.ReadFloat();
            players.Add(new SteamPlayerInfo(name, duration));
        }

        return players;
    }

    private static byte[] GetChallengeValue(UdpClient udp, IPEndPoint endPoint)
    {
        byte[] challengeRequest = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
        udp.Send(challengeRequest, challengeRequest.Length, endPoint);

        IPEndPoint? remote = null;
        var response = udp.Receive(ref remote);

        if (response.Length < 9 || response[4] != 0x41)
            throw new InvalidOperationException("获取挑战值失败");

        var challenge = new byte[4];
        Buffer.BlockCopy(response, 5, challenge, 0, 4);
        return challenge;
    }

    private sealed class ResponseReader(byte[] data, int start = 0)
    {
        private int _index = start;

        public byte ReadByte()
        {
            CheckBounds(1);
            return data[_index++];
        }

        public short ReadInt16()
        {
            CheckBounds(2);
            var value = (short)(data[_index] | (data[_index + 1] << 8));
            _index += 2;
            return value;
        }

        public int ReadInt32()
        {
            CheckBounds(4);
            var value = data[_index] | (data[_index + 1] << 8) |
                        (data[_index + 2] << 16) | (data[_index + 3] << 24);
            _index += 4;
            return value;
        }

        public float ReadFloat()
        {
            CheckBounds(4);
            var value = BitConverter.ToSingle(data, _index);
            _index += 4;
            return value;
        }

        public string ReadString()
        {
            if (_index >= data.Length)
                return string.Empty;

            var start = _index;
            while (_index < data.Length && data[_index] != 0)
                _index++;

            var result = Encoding.UTF8.GetString(data, start, _index - start);
            _index++;
            return result;
        }

        private void CheckBounds(int bytesRequired)
        {
            if (_index + bytesRequired > data.Length)
                throw new InvalidOperationException("响应数据不完整");
        }
    }

    internal sealed record SteamServerInfo(string Name, string Map, short Players, short MaxPlayers);

    internal sealed record SteamPlayerInfo(string Name, float DurationSeconds);
}
