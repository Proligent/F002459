using System;
using System.Diagnostics;
using System.Text;

namespace F002459.Common
{
    public class clsUtil
    {
        /// <summary>
        /// Wait second
        /// </summary>
        /// <param name="dWaitTimeSecond">Second</param>
        public static void Dly(double dWaitTimeSecond)
        {
            long lWaitTime = 0;
            long lStartTime = 0;

            if (dWaitTimeSecond <= 0)
            {
                return;
            }

            lWaitTime = Convert.ToInt64(dWaitTimeSecond * TimeSpan.TicksPerSecond);
            lStartTime = System.DateTime.Now.Ticks;
            while ((System.DateTime.Now.Ticks - lStartTime) < lWaitTime)
            {
                System.Windows.Forms.Application.DoEvents();
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static long StartTimeInTicks()
        {
            return (System.DateTime.Now).Ticks;
        }
        public static double ElapseTimeInSeconds(long StartTimeInTicks)
        {
            return ((System.DateTime.Now).Ticks - StartTimeInTicks) / System.TimeSpan.TicksPerSecond;
        }

        /// <summary>
        /// Convert a string to a byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool StrToByteArray(string str, ref Byte[] b)
        {
            if (str == "")
            {
                return false;
            }

            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                b = encoding.GetBytes(str);
            }
            catch (Exception e)
            {
                string strMsg = e.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert a byte array to a string
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool ByteArrayToStr(byte[] b, ref string str)
        {
            if (b == null)
            {
                return false;
            }
            str = "";

            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                str = encoding.GetString(b);
            }
            catch (Exception e)
            {
                string strMsg = e.Message;
                return false;
            }

            return true;
        }

        public static string ExecuteGetSysProp(string command, int seconds, bool runcmd)
        {
            string output = ""; //Output string
            StringBuilder sbTmp = new StringBuilder();
            if (command != null && !command.Equals(""))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                if (runcmd == true)
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/C " + command;
                }
                else
                {
                    startInfo.FileName = command;
                    startInfo.Arguments = string.Format("10");
                }
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
                try
                {
                    if (process.Start())
                    {
                        if (seconds == 0)
                        {
                            process.WaitForExit();
                        }
                        else
                        {
                            process.WaitForExit(seconds);
                        }
                        output = process.StandardOutput.ReadToEnd();//20200506
                    }
                }
                finally
                {
                    if (process != null)
                        process.Close();
                }
            }
            return output;
        }
        public static String ExecuteGetSysProp(string command)
        {
            string output = ""; //Output string
            StringBuilder sbTmp = new StringBuilder();
            if (command != null && !command.Equals(""))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;
                startInfo.FileName = command;
                startInfo.Arguments = string.Format("10");
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
                try
                {
                    if (process.Start())
                    {
                        output = process.StandardOutput.ReadToEnd();
                    }
                }
                finally
                {
                    if (process != null)
                        process.Close();
                }
            }
            return output;
        }
        public static bool ExcuteCmd(string str_cmd, ref string str_Result, ref string str_msg)
        {
            if (str_cmd == "")
            {
                str_msg = "Invaild paramter.";
                return false;
            }
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";                   // 设定程序名   
                p.StartInfo.Arguments = " /c " + str_cmd;           // 设定程式执行参数 /c是关闭Shell的使用   
                p.StartInfo.UseShellExecute = false;                // 直接启动进程
                p.StartInfo.RedirectStandardInput = true;           // 重定向标准输入
                p.StartInfo.RedirectStandardOutput = true;          // 重定向标准输出   
                p.StartInfo.RedirectStandardError = true;           // 重定向错误输出 
                p.StartInfo.CreateNoWindow = true;                  // 不显示cmd窗口
                p.Start();                                          // 启动      
                string str_Output = "";
                str_Output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                p.Close();
                p.Dispose();
                str_Result = str_Output;
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                str_msg = "Exception:" + str;
                return false;
            }
            return true;
        }
        /// <summary>
        /// Convert Hex String to Int
        /// </summary>
        /// <param name="str_HexString">00000001</param>
        /// <param name="i_Num">1</param>
        /// <returns></returns>
        public static bool HexStringToInt(string str_HexString, ref int i_Num)
        {
            try
            {
                i_Num = Int32.Parse(str_HexString, System.Globalization.NumberStyles.HexNumber);
                //i_Num = Convert.ToInt32(str_HexString, 16);
            }
            catch (Exception e)
            {
                string strMsg = e.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string ToHexString(byte[] bytes)
        {
            try
            {
                string hexString = string.Empty;
                if (bytes != null)
                {
                    StringBuilder strB = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        strB.Append(bytes[i].ToString("X2"));
                    }
                    hexString = strB.ToString();
                }

                return hexString;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
    }

}
