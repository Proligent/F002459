using System;
using System.Diagnostics;

namespace F002459
{
    class clsExecProcess
    {
        #region Variable

        private string m_str_ErrMsg = "";

        #endregion

        #region Property

        public string ErrMsg
        {
            get
            {
                return m_str_ErrMsg;
            }
        }

        #endregion

        #region Construct

        public clsExecProcess()
        {

        }

        #endregion

        #region Function

        public bool ExcuteCmd(string str_cmd)
        {
            // 检查输入参数
            if (str_cmd == "")
            {
                return false;
            }

            try
            {
                // 实例一个Process类
                Process p = new Process();

                // Process类有一个StartInfo属性 
                p.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";   // 设定程序名
                p.StartInfo.Arguments = " /c " + str_cmd;                  // 设定程式执行参数 /c是关闭Shell的使用   
                p.StartInfo.UseShellExecute = false;                   // 直接启动进程
                p.StartInfo.RedirectStandardInput = false;             // 重定向标准输入
                p.StartInfo.RedirectStandardOutput = false;            // 重定向标准输出   
                p.StartInfo.RedirectStandardError = false;             // 重定向错误输出 
                //p.StartInfo.CreateNoWindow = false;                  // 显示cmd窗口
                p.StartInfo.CreateNoWindow = true;                     // 不显示cmd窗口
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                p.Start();

                p.WaitForExit();
                p.Close();
                p.Dispose();
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                return false;
            }

            return true;
        }

        public bool ExcuteCmd(string str_cmd, string str_Result)
        {
            // 检查输入参数
            if (str_cmd == "")
            {
                m_str_ErrMsg = "Invaild paramter.";
                return false;
            }

            try
            {
                // 实例一个Process类
                Process p = new Process();

                // Process类有一个StartInfo属性 
                p.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";                   // 设定程序名   
                p.StartInfo.Arguments = " /c " + str_cmd;           // 设定程式执行参数 /c是关闭Shell的使用   
                p.StartInfo.UseShellExecute = false;                // 直接启动进程
                p.StartInfo.RedirectStandardInput = true;           // 重定向标准输入
                p.StartInfo.RedirectStandardOutput = true;          // 重定向标准输出   
                p.StartInfo.RedirectStandardError = true;           // 重定向错误输出 
                //p.StartInfo.CreateNoWindow = false;               // 显示cmd窗口
                p.StartInfo.CreateNoWindow = true;                  // 不显示cmd窗口

                p.Start();                                          // 启动      

                // 从输出流取得命令执行结果
                string str_Output = "";
                str_Output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                p.Close();
                p.Dispose();

                // 检查值
                if (str_Output.IndexOf(str_Result) == -1)
                {
                    m_str_ErrMsg = "Check return value fail.";
                    return false;
                }

                //string[] sArray = Regex.Split(str_Output, "\r", RegexOptions.IgnoreCase);
                //bool b_SearchResult = false;
                //for (int i = 0; i < sArray.Length; i++)
                //{
                //    if (sArray[i].IndexOf(str_Result) >= 0)
                //    {
                //        b_SearchResult = true;
                //    }
                //}
                //if (b_SearchResult == false)
                //{
                //    return false;
                //}
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                m_str_ErrMsg = "Exception:" + str;
                return false;
            }

            return true;
        }

        public bool ExcuteCmd(string str_cmd, ref string str_Result)
        {
            // 检查输入参数
            if (str_cmd == "")
            {
                m_str_ErrMsg = "Invaild paramter.";
                return false;
            }

            try
            {
                // 实例一个Process类
                Process p = new Process();

                // Process类有一个StartInfo属性 
                p.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";                   // 设定程序名   
                p.StartInfo.Arguments = " /c " + str_cmd;           // 设定程式执行参数 /c是关闭Shell的使用   
                p.StartInfo.UseShellExecute = false;                // 直接启动进程
                p.StartInfo.RedirectStandardInput = true;           // 重定向标准输入
                p.StartInfo.RedirectStandardOutput = true;          // 重定向标准输出   
                p.StartInfo.RedirectStandardError = true;           // 重定向错误输出 
                //p.StartInfo.CreateNoWindow = false;               // 显示cmd窗口
                p.StartInfo.CreateNoWindow = true;                  // 不显示cmd窗口

                p.Start();                                          // 启动      

                // 从输出流取得命令执行结果
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
                m_str_ErrMsg = "Exception:" + str;
                return false;
            }

            return true;
        }

        public bool FindProcess(string str_ProcessName)
        {
            bool bRes = false;

            if (str_ProcessName == "")
            {
                return false;
            }

            try
            {
                //Process[] arrP = Process.GetProcesses();
                Process[] arrP = Process.GetProcessesByName(str_ProcessName);
                foreach (Process p in arrP)
                {
                    if (p.ProcessName == str_ProcessName)
                    {
                        bRes = true;
                    }
                }
            }
            catch
            {
                return false;
            }

            if (bRes == false)
            {
                return false;
            }

            return true;
        }

        public bool KillProcess(string str_ProcessName)
        {
            if (str_ProcessName == "")
            {
                return false;
            }

            try
            {
                //Process[] arrP = Process.GetProcesses();
                Process[] arrP = Process.GetProcessesByName(str_ProcessName);
                foreach (Process p in arrP)
                {
                    if (p.ProcessName == str_ProcessName)
                    {
                        p.Kill();
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion
    }

}
