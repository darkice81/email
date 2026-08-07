using System.Net.NetworkInformation;

namespace FusionPortalClient.Services;

public interface IVpnDetectionService
{
    (bool detected, List<string> adapterNames) DetectVpn();
}

/// <summary>
/// Heuristic-only VPN detection. There is no reliable, false-positive-free
/// way to answer "is a VPN running" from user-mode code, so this flags
/// interfaces that *look like* a VPN client's virtual adapter and lets a
/// human/compliance workflow decide what to do with a positive result.
/// See docs/API_CONTRACT.md and README "VPN detection caveats".
/// </summary>
public class VpnDetectionService : IVpnDetectionService
{
    private static readonly string[] KnownVpnAdapterPatterns =
    {
        "openvpn", "tap-windows", "wireguard", "wintun", "anyconnect",
        "cisco vpn", "globalprotect", "forticlient", "pulse secure",
        "nordlynx", "nordvpn", "expressvpn", "protonvpn", "tunnelbear",
        "zscaler", "checkpoint vpn", "surfshark", "mullvad", "ivpn",
        "sonicwall", "vpn client"
    };

    public (bool detected, List<string> adapterNames) DetectVpn()
    {
        var flagged = new List<string>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
            {
                flagged.Add(nic.Description);
                continue;
            }

            var description = nic.Description.ToLowerInvariant();
            var name = nic.Name.ToLowerInvariant();

            if (KnownVpnAdapterPatterns.Any(pattern =>
                    description.Contains(pattern) || name.Contains(pattern)))
            {
                flagged.Add(nic.Description);
            }
        }

        return (flagged.Count > 0, flagged);
    }
}
