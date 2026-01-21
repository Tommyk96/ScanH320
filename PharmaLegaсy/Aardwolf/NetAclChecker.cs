using System;
using System.Diagnostics;
using System.Globalization;
//using System.Management.Automation;

namespace Aardwolf
{
    public static class NetAclChecker
    {
        public static void AddAddress(string address)
        {
            AddAddress(address, Environment.UserDomainName, Environment.UserName);
        }

        public static void AddAddress(string address, string domain, string user)
        {
            string args = string.Format(CultureInfo.InvariantCulture, @"http add urlacl url={0} user={1}\{2}", address, domain, user);

            ProcessStartInfo psi = new ProcessStartInfo("netsh", args);
            psi.Verb = "runas";
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = true;

            Process.Start(psi).WaitForExit();
        }

        // netsh advfirewall firewall add rule name="WhIdSrv Inbount" protocol=TCP dir=in localport=8088 action=allow
        public static void AddPort(string port)
        {
            string args = string.Format(CultureInfo.InvariantCulture, @"netsh advfirewall firewall add rule name=WhIdSrv_Inbount protocol=TCP dir=in localport={0} action=allow", port);

            ProcessStartInfo psi = new ProcessStartInfo("netsh", args);
            psi.Verb = "runas";
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = true;
            Process.Start(psi).WaitForExit();
        }
        /*
          netsh advfirewall firewall add rule name="WhIdSrv Inbount" protocol=TCP dir=in localport=8088 action=allow
     
      protocol=TCP dir=in localport=80 security=authdynenc
      action=allow
         using System.Management.Automation;
...
private void OpenPort(int port)
{
    var powershell = PowerShell.Create();
    var psCommand = $"New-NetFirewallRule -DisplayName \"<rule description>\" -Direction Inbound -LocalPort {port} -Protocol TCP -Action Allow";
    powershell.Commands.AddScript(psCommand);
    powershell.Invoke();
} */
    }
}