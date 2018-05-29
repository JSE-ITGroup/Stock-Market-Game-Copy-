using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net;
using Renci.SshNet;
using log4net;

namespace StockMarketGamePriceUpload
{

    public enum ShhAuthenticationMethod
    {
        Publickey = 0,
        Password = 1,
        KeyboardInteractive = 2
    }

    public class SshWrapper
    {
        private static readonly ILog log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static SshClient client = null;

        public static bool Connect(Dictionary<string, object> sshPropertyDictionary)
        {
            //GET SSH Setings

            //sshPropertyDictionary = new Dictionary<string, object>();
            var sshAuthenticationMethod = (ShhAuthenticationMethod)0;

            //sshPropertyDictionary["SshAuthenticationMethod"];
            var sshHost = sshPropertyDictionary.ContainsKey("SshHost") ? (string)sshPropertyDictionary["SshHost"] : "";
            var sshPort = sshPropertyDictionary.ContainsKey("SshPort") ? (int)Convert.ToInt16(sshPropertyDictionary["SshPort"]) : 0;
            var sshUserName = sshPropertyDictionary.ContainsKey("SshUserName") ? (string)sshPropertyDictionary["SshUserName"] : "";
            var sshPassword = sshPropertyDictionary.ContainsKey("SshPassword")
                ? (string)sshPropertyDictionary["SshPassword"]
                : "";
            var sshLocalForwardPort = sshPropertyDictionary.ContainsKey("SshLocalForwardPort")
                ? (int)Convert.ToInt16(sshPropertyDictionary["SshLocalForwardPort"])
                : 0;
            var sshRemotePort = sshPropertyDictionary.ContainsKey("SshRemotePort")
                ? (int)Convert.ToInt16(sshPropertyDictionary["SshRemotePort"])
                : 0;
            var sshRemoteHost = sshPropertyDictionary.ContainsKey("SshRemoteHost")
                ? (string)sshPropertyDictionary["SshRemoteHost"]
                : "";
            var sshLocalForwardHost = sshPropertyDictionary.ContainsKey("SshLocalForwardHost")
                ? (string)sshPropertyDictionary["SshLocalForwardHost"]
                : "";

            var sshPrivatekeyPath = sshPropertyDictionary.ContainsKey("SshPrivateKeyFile")
                ? (string)sshPropertyDictionary["SshPrivateKeyFile"]
                : "";
            var requireLocalForwarding = sshPropertyDictionary.ContainsKey("requireLocalForwarding")
                ? sshPropertyDictionary["requireLocalForwarding"].ToString().Equals("Y")
                    ? true
                    : false
                : false;



            sshPrivatekeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sshPrivatekeyPath);

            log.Info(string.Format("SSH Authentication Method :{0}", sshAuthenticationMethod));



            if (sshAuthenticationMethod == ShhAuthenticationMethod.KeyboardInteractive)
            {

            }
            else if (sshAuthenticationMethod == ShhAuthenticationMethod.Password)
            {

                client = new SshClient(sshHost, Convert.ToInt32(sshPort), sshUserName, sshPassword);
            }

            else if (sshAuthenticationMethod == ShhAuthenticationMethod.Publickey)
            {
                if (!File.Exists(sshPrivatekeyPath))
                {
                    log.Error(string.Format("Unable to Locate Private in give path: {0}", sshPrivatekeyPath));
                    return false;
                }
                var keyFile = new PrivateKeyFile(sshPrivatekeyPath);
                var keyFiles = new[] { keyFile };
                client = new SshClient(sshHost, sshUserName, keyFiles);
            }

            if (client != null)
            {
                client.Connect();
                ForwardedPortLocal localport = null;
                if (requireLocalForwarding)
                {
                    localport = new ForwardedPortLocal(sshLocalForwardHost, Convert.ToUInt32(sshLocalForwardPort),
                        sshRemoteHost,
                        Convert.ToUInt32(sshRemotePort));
                }
                if (client.IsConnected)
                {
                    if (requireLocalForwarding)
                    {
                        client.AddForwardedPort(localport);
                        localport.Start();
                    }

                    log.Info(string.Format("SSH Tunnel Established, Connected to :{0}", sshHost));

                    return true;
                }
                else
                {

                    log.Info(string.Format("An SSH Tunnel Connect was not Established with Host :{0}", sshHost));
                    return false;
                }


            }
            else
            {
                log.Info(string.Format("Unable to Connect SSH Tunnel Host :{0}", sshHost));
                return false;
            }
            return false;
        }

        public static string ExecuteCommand(string cmd)
        {
            log.Info(string.Format("Attemping to execute command :{0}", cmd));

            if (client == null || !client.IsConnected)
            {
                return "SSH Client Not Connected";
            }
            string results = null;

            try
            {
                results = client.CreateCommand(cmd).Execute();

            }
            catch (Exception exp)
            {
                log.Error(exp);
                throw;
            }

            log.Info(string.Format("Execution Reply: \t{0}", results));

            return results;
        }

        public static string TerminalExecute(string cmd, string password)
        {


            IDictionary<Renci.SshNet.Common.TerminalModes, uint> modes = new Dictionary<Renci.SshNet.Common.TerminalModes, uint>();

            modes.Add(Renci.SshNet.Common.TerminalModes.ECHO, 53);

            ShellStream shellStream = client.CreateShellStream("xterm", 280, 24, 800, 600, 1024, modes);
            var output = shellStream.Expect(new Regex(@"[$>]"));
            log.Info(string.Format("Execution Reply: \t{0}", output));


            log.Info(string.Format("Sending Text :{0}", cmd));
            shellStream.WriteLine(cmd);
            output = shellStream.Expect(new Regex(@"([$#>:])"));
            log.Info(string.Format("Execution Reply: \t{0}", output));

            log.Info(string.Format("Sending Text:{0}", password));
            shellStream.WriteLine(password);
            output = shellStream.Expect(new Regex(@"([$#>:])"));
            log.Info(string.Format("Execution Reply: \t{0}", output));


            output = shellStream.Read();
            log.Info(string.Format("End Execution Reply: \t{0}", output));
            return output;

        }

        public static void Disconnect()
        {
            if (client != null && client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }
}
