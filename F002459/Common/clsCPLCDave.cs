using System;
using System.Text;

namespace F002459
{
    class clsCPLCDave
    {
        #region Variables

        private static libnodave.daveConnection dc;
        private static libnodave.daveInterface di;
        private static libnodave.daveOSserialType fds;

        private bool m_b_Connected = false;
        private const int m_i_Area = 132;

        private readonly object locker = new object();

        #endregion

        #region Enum

        public enum FeedbackStatus : int
        {
            READY = 1,
            BUSY,
            HAVEPRODUCT,
            ERROR
        }

        public enum FeedbackResult : int
        {
            CLEAR = 0,
            SUCCESS = 1,
            FAIL
        }

        public enum BarCodeStatus : int
        {
            OK = 1,
            NG
        }

        #endregion

        #region Connect

        public bool Connect(string strIPAddr, int iPort, ref string strErrorMessage)
        {
            strErrorMessage = "";
            int iRes = -100;

            try
            {
                m_b_Connected = false;

                fds.rfd = libnodave.openSocket(iPort, strIPAddr);
                fds.wfd = fds.rfd;
                di = new libnodave.daveInterface(fds, "IF1", 0, libnodave.daveProtoISOTCP, libnodave.daveSpeed187k);

                di.setTimeout(10000);
                iRes = di.initAdapter();
                if (iRes != 0)
                {
                    m_b_Connected = false;
                    strErrorMessage = "Connect error, init adapter failed!";
                    return false;
                }

                dc = new libnodave.daveConnection(di, 0, 0, 1);
                iRes = dc.connectPLC();
                if (iRes != 0)
                {
                    m_b_Connected = false;
                    strErrorMessage = "Connect error, could not connect PLC!";
                    return false;
                }
                else
                {
                    m_b_Connected = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                m_b_Connected = false;
                strErrorMessage = "Connect exception, connection aborted!";
                return false;
            }
        }

        public bool DisConnect(ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                dc.disconnectPLC();

                di.disconnectAdapter();

                libnodave.closeSocket(fds.rfd);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = string.Format("Disconnect exception, {0}", ex.Message);
                return false;
            }
            finally
            {
                m_b_Connected = false;
            }
        }

        #endregion

        #region Read

        private bool ReadByte(int iReadBlock, int iReadAddress, int iBytesToRead, ref string strResult, ref string strErrorMessage)
        {
            strResult = "";
            strErrorMessage = "";

            byte[] bArr = new byte[1025];
            int iRes = -100;
            byte bRead = 0;

            try
            {
                if (m_b_Connected)
                {
                    iRes = dc.readBytes(m_i_Area, iReadBlock, iReadAddress, iBytesToRead, bArr);
                    if (iRes != 0)
                    {
                        strErrorMessage = string.Format("Read Value Error!");
                        return false;
                    }

                    bRead = ConvertByteInToByte(bArr[0]);
                    strResult = Convert.ToString(bRead);

                    return true;
                }
                else
                {
                    strErrorMessage = string.Format("Read Value Error, PLC doesn't connect!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = string.Format("Read Value exception, {0}", ex.Message);
                return false;
            }
        }

        private bool ReadString(int iReadBlock, int iReadAddress, int iBytesToRead, ref string strResult, ref string strErrorMessage)
        {
            strResult = "";
            strErrorMessage = "";
            byte[] bArr = new byte[1025];
            int iRes = -100;
            byte bFirst = 0;
            byte bSecond = 0;

            try
            {
                if (m_b_Connected)
                {
                    iRes = dc.readManyBytes(m_i_Area, iReadBlock, iReadAddress, iBytesToRead, bArr);
                    if (iRes != 0)
                    {
                        strErrorMessage = string.Format("Read Value Error!");
                        return false;
                    }

                    bFirst = bArr[0];
                    bSecond = bArr[1];
                    if (bFirst >= bSecond)
                    {
                        strResult = Encoding.ASCII.GetString(bArr, 2, iBytesToRead);
                    }
                    else
                    {
                        strErrorMessage = string.Format("Read Error, data format is error!");
                        return false;
                    }

                    return true;
                }
                else
                {
                    strErrorMessage = string.Format("Read Value Error, PLC doesn't connect!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = string.Format("Read Value exception, {0}", ex.Message);
                return false;
            }
        }

        private byte ConvertByteInToByte(byte bFirstByte)
        {
            return bFirstByte;
        }

        #endregion

        #region Write

        private bool WriteByte(int iWriteBlock, int iWriteAddress, int iBytesToWrite, byte bWrite, ref string strErrorMessage)
        {
            strErrorMessage = "";
            byte[] bArr = new byte[1025];
            int iRes = -100;

            try
            {
                if (m_b_Connected)
                {
                    bArr[0] = bWrite;
                    iRes = dc.writeBytes(m_i_Area, iWriteBlock, iWriteAddress, iBytesToWrite, bArr);
                    if (iRes != 0)
                    {
                        strErrorMessage = string.Format("Write Value Error!");
                        return false;
                    }

                    return true;
                }
                else
                {
                    strErrorMessage = string.Format("Write Value Error, PLC doesn't connect!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = string.Format("Write Value exception, {0}", ex.Message);
                return false;
            }
        }

        private bool WriteString(int iWriteBlock, int iWriteAddress, int iBytesToWrite, string strData, ref string strErrorMessage)
        {
            strErrorMessage = "";
            byte[] bArr = new byte[1025];
            int iRes = -100;

            // Max Length is 100
            if (strData.Length <= 0 || strData.Length >= 100)
            {
                strErrorMessage = string.Format("Invalid data,max length 100!");
                return false;
            }

            try
            {
                if (m_b_Connected)
                {
                    byte[] bCmd = Encoding.ASCII.GetBytes(strData);
                    for (int i = 0; i < strData.Length; i++)
                    {
                        bArr[i] = bCmd[i];
                    }
                    iRes = dc.writeBytes(m_i_Area, iWriteBlock, iWriteAddress, iBytesToWrite, bArr);
                    if (iRes != 0)
                    {
                        strErrorMessage = string.Format("Write Value Error!");
                        return false;
                    }

                    return true;
                }
                else
                {
                    strErrorMessage = string.Format("Write Value Error, PLC doesn't connect!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = string.Format("Write Value exception, {0}", ex.Message);
                return false;
            }
        }

        #endregion

        #region PC To PLC

        // B0	看门狗	          0-255累加:PLC2S内发现数值不变则通讯中断
        public bool WatchDog(int iBlock, string strCmd, ref string strErrorMessage)
        {
            try
            {
                lock (locker)
                {
                    strErrorMessage = "";
                    int iWriteAddress = 0; // B0
                    int iBytesToWrite = 1;
                    byte bWrite = 0;

                    try
                    {
                        bWrite = Convert.ToByte(strCmd);
                        if (WriteByte(iBlock, iWriteAddress, iBytesToWrite, bWrite, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        strErrorMessage = string.Format("WatchDog exception, {0}", ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Lock Exception:" + ex.Message;
                return false;
            }
        }

        // B1	反馈状态	       1：准备就绪    2:忙碌   3:有产品   4：出现故障
        public bool FeedbackCurrentStatus(int iBlock, FeedbackStatus enumFeedbackStatus, ref string strErrorMessage)
        {
            try
            {
                lock (locker)
                {
                    strErrorMessage = "";
                    string strResult = "";
                    int iStatus = 0;
                    int iWriteAddress = 1; // B1 
                    int iBytesToWrite = 1;

                    try
                    {
                        iStatus = (int)enumFeedbackStatus;
                        if (WriteByte(iBlock, iWriteAddress, iBytesToWrite, (byte)iStatus, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        if (ReadByte(iBlock, iWriteAddress, iBytesToWrite, ref strResult, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        if (iStatus.ToString() == strResult)
                        {
                            return true;
                        }
                        else
                        {
                            strErrorMessage = "Failed to check write and read!";
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        strErrorMessage = string.Format("FeedbackCurrentStatus exception, {0}", ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Lock Exception:" + ex.Message;
                return false;
            }
        }

        // B2	执行命令结果	  1：完成  2：失败 
        public bool FeedbackRunResult(int iBlock, FeedbackResult enumFeedbackResult, ref string strErrorMessage)
        {
            try
            {
                lock (locker)
                {
                    strErrorMessage = "";
                    string strResult = "";
                    int iResult = 0;
                    int iWriteAddress = 2; // B2
                    int iBytesToWrite = 1;

                    try
                    {
                        iResult = (int)enumFeedbackResult;
                        if (WriteByte(iBlock, iWriteAddress, iBytesToWrite, (byte)iResult, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        if (ReadByte(iBlock, 2, 1, ref strResult, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        if (iResult.ToString() == strResult)
                        {
                            return true;
                        }
                        else
                        {
                            strErrorMessage = "Failed to check write and read!";
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        strErrorMessage = string.Format("FeedbackRunResult exception, {0}", ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Lock Exception:" + ex.Message;
                return false;
            }
        }

        // B4-103	失败代码[100]	长度为100的字符串
        public bool FeedbackErrorCode(int iBlock, string strErrorCode, ref string strErrorMessage)
        {
            try
            {
                lock (locker)
                {
                    strErrorMessage = "";
                    int iWriteAddress = 4; // B4
                    int iBytesToWrite = 100;

                    // Max Length is 100
                    if (strErrorCode.Length <= 0)
                    {
                        strErrorMessage = string.Format("Invalid error code,length is 0!");
                        return false;
                    }
                    if (strErrorCode.Length > 100)
                    {
                        strErrorCode = strErrorCode.Substring(0, 100);
                    }

                    try
                    {
                        if (WriteString(iBlock, iWriteAddress, iBytesToWrite, strErrorCode, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        strErrorMessage = string.Format("FeedbackErrorCode exception, {0}", ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Lock Exception:" + ex.Message;
                return false;
            }
        }

        #endregion

        #region PLC To PC

        // B0	命令	1：开始测试
        public bool ReadCommandStartTest(int iBlock, ref string strErrorMessage)
        {
            try
            {
                lock (locker)
                {
                    strErrorMessage = "";
                    string strResult = "";
                    int iReadAddress = 0; // B0
                    int iBytesToRead = 1;

                    try
                    {
                        strErrorMessage = "";
                        if (ReadByte(iBlock, iReadAddress, iBytesToRead, ref strResult, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        if (strResult != "1")
                        {
                            strErrorMessage = "Read result is not 1.";
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        strErrorMessage = string.Format("ReadCommandStartTest exception, {0}", ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Lock Exception:" + ex.Message;
                return false;
            }
        }

        // B104   0：无产品    1: 有产品   
        public bool ReadProductExist(int iBlock, ref string strErrorMessage)
        {
            try
            {
                lock (locker)
                {
                    strErrorMessage = "";
                    string strResult = "";
                    int iReadAddress = 104; // B104
                    int iBytesToRead = 1;

                    try
                    {
                        strErrorMessage = "";
                        if (ReadByte(iBlock, iReadAddress, iBytesToRead, ref strResult, ref strErrorMessage) == false)
                        {
                            return false;
                        }

                        if (strResult != "1")
                        {
                            strErrorMessage = "Read result is not 1.";
                            return false;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        strErrorMessage = string.Format("ReadProductExist exception, {0}", ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Lock Exception:" + ex.Message;
                return false;
            }
        }

        #endregion
    }

}
