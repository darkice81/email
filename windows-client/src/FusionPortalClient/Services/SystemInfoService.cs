using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using FusionPortalClient.Models;

namespace FusionPortalClient.Services;

public interface ISystemInfoService
{
    string GetHostname();
    (bool domainJoined, string? domainName) GetDomainInfo();
    List<string> GetLocalIpAddresses();
    List<RunningProgram> GetVisibleRunningPrograms();
}

/// <summary>
/// Collects host-level facts (hostname, domain-join status, local IPs, and
/// the list of processes that have a visible top-level window). Deliberately
/// does not enumerate every OS process - only ones with a window, so the
/// telemetry payload reflects what the player is actually using rather than
/// background services.
/// </summary>
public class SystemInfoService : ISystemInfoService
{
    public string GetHostname() => Environment.MachineName;

    public (bool domainJoined, string? domainName) GetDomainInfo()
    {
        // A machine joined to an Active Directory domain has a UserDomainName
        // that differs from its MachineName (workgroup machines report the
        // machine name as their "domain"). This avoids depending on
        // System.DirectoryServices, which requires an extra Windows feature.
        var domainName = Environment.UserDomainName;
        var isDomainJoined = !string.Equals(domainName, Environment.MachineName,
            StringComparison.OrdinalIgnoreCase);

        return (isDomainJoined, isDomainJoined ? domainName : null);
    }

    public List<string> GetLocalIpAddresses()
    {
        var addresses = new List<string>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var addrInfo in nic.GetIPProperties().UnicastAddresses)
            {
                if (addrInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    addresses.Add(addrInfo.Address.ToString());
                }
            }
        }

        return addresses;
    }

    public List<RunningProgram> GetVisibleRunningPrograms()
    {
        var programs = new List<RunningProgram>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    continue;

                programs.Add(new RunningProgram
                {
                    ProcessName = process.ProcessName,
                    WindowTitle = process.MainWindowTitle
                });
            }
            catch (Exception)
            {
                // Some processes (elevated/system) throw on access; skip them.
            }
            finally
            {
                process.Dispose();
            }
        }

        return programs;
    }
}
