namespace SummarizeApi;

public static class Utils
{
    public static async Task<bool> IsValidExternalUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        try
        {
            var hostAddresses = await System.Net.Dns.GetHostAddressesAsync(uri.Host);
            foreach (var addr in hostAddresses)
            {
                if (
                    System.Net.IPAddress.IsLoopback(addr)
                    || addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                        && (
                            addr.GetAddressBytes()[0] == 10
                            || (
                                addr.GetAddressBytes()[0] == 172
                                && addr.GetAddressBytes()[1] >= 16
                                && addr.GetAddressBytes()[1] <= 31
                            )
                            || addr.GetAddressBytes()[0] == 192 && addr.GetAddressBytes()[1] == 168
                            || ( // metadata endpoint
                                addr.GetAddressBytes()[0] == 169
                                && addr.GetAddressBytes()[1] == 254
                                && addr.GetAddressBytes()[2] == 169
                                && addr.GetAddressBytes()[3] == 254
                            )
                        )
                    || addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && addr.IsIPv6LinkLocal
                )
                {
                    return false;
                }
            }
        }
        catch
        {
            return false;
        }
        return true;
    }
}
