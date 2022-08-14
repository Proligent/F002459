using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace F002459
{
    class clsIPMAC
    {
        #region DLL

        [DllImport("Iphlpapi.dll")]
        private static extern Int32 SendARP(Int32 dest, Int32 host, ref Int64 mac, ref Int32 length);
        [DllImport("Ws2_32.dll")]
        private static extern Int32 inet_addr(string ip);

        #endregion

        #region Constructor

        public clsIPMAC()
        {
        }

        #endregion

        #region Function

        public bool CheckIP(string str_IP, ref string str_ErrorMessage)
        {
            bool bRes = true;
            int iTmp = 0;
            string[] ipSplit = str_IP.Split('.');
            if (ipSplit.Length < 4 || string.IsNullOrEmpty(ipSplit[0]) ||
                string.IsNullOrEmpty(ipSplit[1]) ||
                string.IsNullOrEmpty(ipSplit[2]) ||
                string.IsNullOrEmpty(ipSplit[3]))
            {
                bRes = false;
            }
            else
            {
                for (int i = 0; i < ipSplit.Length; i++)
                {
                    if (!int.TryParse(ipSplit[i], out iTmp) || iTmp < 0 || iTmp > 255)
                    {
                        bRes = false;
                        break;
                    }
                }
            }

            if (bRes == false)
            {
                str_ErrorMessage = "Failed to check IP.";
                return false;
            }

            return true;
        }

        public bool GetRemoteMAC(string str_RemoteIP, ref string str_MAC, ref string str_ErrorMessage)
        {
            StringBuilder macAddress = new StringBuilder();

            try
            {
                Int32 remote = inet_addr(str_RemoteIP);
                Int64 macInfo = new Int64();
                Int32 length = 6;
                int res = SendARP(remote, 0, ref macInfo, ref length);
                if (res != 0)
                {
                    str_ErrorMessage = "Failed to SendARP ";
                    return false;
                }
                if (length != 6)
                {
                    str_ErrorMessage = "Failed to check MAC length.";
                    return false;
                }

                string temp = Convert.ToString(macInfo, 16).PadLeft(12, '0').ToUpper();
                int x = 12;
                for (int i = 0; i < 6; i++)
                {
                    macAddress.Append(temp.Substring(x - 2, 2));
                    x -= 2;
                }

                str_MAC = macAddress.ToString();
            }
            catch (Exception err)
            {
                str_ErrorMessage = err.Message;
                return false;
            }

            return true;
        }

        public bool PingIP(string str_IP, ref string str_ErrorMessage)
        {
            Ping ping = null;
            try
            {
                ping = new Ping();
                PingReply pingresult = ping.Send(str_IP, 1000);
                if (pingresult.Status.ToString() != "Success")
                {
                    str_ErrorMessage = pingresult.Status.ToString();
                    return false;
                }
            }
            finally
            {
                if (ping != null)
                {
                    // 2.0 下ping 的一个bug，需要显示转型后释放 
                    IDisposable disposable = ping;
                    disposable.Dispose();
                    ping.Dispose();
                }
            }

            return true;
        }

        #endregion
    }
}
