using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;

public static class PingUtility
{
    /// <summary>
    /// 传入 IPAddress 直接测（推荐）
    /// </summary>
    public static async Task<int> GetPing(IPAddress ip, int timeoutMs = 800)
    {
        if (ip == null) return -1;

        try
        {
            // 如果是 IPv6 里包着 IPv4（::ffff:1.2.3.4），转成真正 IPv4
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            using (var ping = new System.Net.NetworkInformation.Ping())
            {
                PingReply reply = await ping.SendPingAsync(ip, timeoutMs);
                return reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : -1;
            }
        }
        catch (Exception)
        {
            // 这里别疯狂 LogWarning，否则房间多会把主线程日志卡爆
            return -1;
        }
    }

    /// <summary>
    /// 传入字符串（IP/域名）也行：优先解析到 IPv4，再测
    /// </summary>
    public static async Task<int> GetPing(string hostOrIp, int timeoutMs = 800)
    {
        if (string.IsNullOrWhiteSpace(hostOrIp)) return -1;

        try
        {
            IPAddress ip;
            if (!IPAddress.TryParse(hostOrIp, out ip))
            {
                var list = await Dns.GetHostAddressesAsync(hostOrIp);

                // 优先 IPv4
                ip = Array.Find(list, a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                     ?? Array.Find(list, a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);

                if (ip == null) return -1;
            }

            return await GetPing(ip, timeoutMs);
        }
        catch (Exception)
        {
            return -1;
        }
    }
}