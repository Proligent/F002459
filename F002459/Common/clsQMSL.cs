using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace F002459
{
    public class clsQMSL
    {
        #region QMSL_MSVC10R.dll

        [DllImport("QMSL_MSVC10R.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        static extern void QLIB_SetLibraryMode(byte useQPST);

        [DllImport("QMSL_MSVC10R.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        static extern UInt32 QLIB_ConnectServer(uint comPort);

        [DllImport("QMSL_MSVC10R.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        static extern byte QLIB_DisconnectServer(UInt32 hResourceContext);

        [DllImport("QMSL_MSVC10R.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        static extern byte QLIB_IsPhoneConnected(UInt32 hResourceContext);

        [DllImport("QMSL_MSVC10R.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        static extern byte QLIB_DownloadQcnFile_V2(UInt32 hResourceContext, string sFileName, string sSPC);

        [DllImport("QMSL_MSVC10R.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        static extern byte QLIB_DIAG_NV_READ_F(UInt32 hResourceContext, ushort itemID, byte[] itemData, int length, ref ushort status);

        [DllImport("QMSL_MSVC10R.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        static extern byte QLIB_BackupNVFromMobileToQCN(UInt32 hResourceContext, string sQCN_Path, ref Int32 iResultCode);

        #endregion

        #region DownloadThread

        public class DownloadThread
        {
            private UInt32 m_hHandle;
            private string m_sQCNFilePath;

            public DownloadThread(UInt32 hHandle, string qcnFilePath)
            {
                this.m_hHandle = hHandle;
                this.m_sQCNFilePath = qcnFilePath;
            }

            public void Main()
            {
                byte bResult = 0;
                bResult = QLIB_DownloadQcnFile_V2(m_hHandle, m_sQCNFilePath, "000000");

            }

        }

        #endregion

        #region Variable

        private bool m_b_ModeSetted = false;
        private object QPSTConnectSyncLocker = new object();

        #endregion

        #region Construct

        public clsQMSL()
        {

        }

        #endregion

        #region Function

        public bool BackupQCN(int iPhoneCOMPort, string strQCNFilePath, ref string strIMEI, ref string strQCNFileName, ref string strErrorMessage)
        {
            strIMEI = "";
            strQCNFileName = "";
            strErrorMessage = "";
            UInt32 hHandle = 0;
            UInt32 iCOMPort = 0;
            byte bResult = 0;
            bool bRes = false;
            string strNVIMEI = "";
            string strQCNFilePathName = "";

            try
            {
                #region Check Input

                if (strQCNFilePath.Length < 1)
                {
                    strErrorMessage = "Invalid QCN path." + strQCNFilePath;
                    return false;
                }
                if (strQCNFilePath.Substring(strQCNFilePath.Length - 1, 1) == "\\")
                {
                    strQCNFilePath = strQCNFilePath.Substring(0, strQCNFilePath.Length - 1);
                }
                if (Directory.Exists(strQCNFilePath) == false)
                {
                    strErrorMessage = "QCN path not exist." + strQCNFilePath;
                    return false;
                }

                #endregion

                #region QLIB_SetLibraryMode

                QLIB_SetLibraryMode(1); // 0:not use QPST;1:use QPST

                #endregion

                #region QLIB_ConnectServer

                iCOMPort = (UInt32)iPhoneCOMPort;
                bRes = false;
                for (int i = 0; i < 10; i++)
                {
                    hHandle = QLIB_ConnectServer(iCOMPort);
                    if (hHandle <= 0)
                    {
                        System.Threading.Thread.Sleep(1000);
                        bRes = false;
                        continue;
                    }
                    else
                    {
                        bRes = true;
                        break;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "QLIB_ConnectServer fail.";
                    return false;
                }

                System.Threading.Thread.Sleep(1000);

                #endregion

                #region QLIB_IsPhoneConnected

                bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bResult = QLIB_IsPhoneConnected(hHandle);
                    if (bResult == 1)
                    {
                        bRes = true;
                        break;
                    }
                    else
                    {
                        bRes = false;
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "QLIB_IsPhoneConnected fail.";
                    return false;
                }

                #endregion

                System.Threading.Thread.Sleep(1000);

                #region QLIB_DIAG_NV_READ_F

                bRes = false;
                ushort uStatus = 0;
                byte[] bIMEI = new byte[10];
                for (int i = 0; i < 3; i++)
                {
                    bResult = QLIB_DIAG_NV_READ_F(hHandle, 550, bIMEI, 10, ref uStatus);
                    if (bResult == 1)
                    {
                        if (uStatus == 0)
                        {
                            bRes = true;
                            break;
                        }
                        else if (uStatus == 5)
                        {
                            // No MEID, Please Check whether WLAN Version
                            bRes = false;
                            break;
                        }
                        else
                        {
                            bRes = false;
                            break;
                        }
                    }
                    else
                    {
                        bRes = false;
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "QLIB_DIAG_NV_READ_F fail.Status:" + uStatus.ToString();
                    return false;
                }

                #endregion

                #region IMEI

                if (ByteToHexStr(bIMEI, ref strNVIMEI) == false)
                {
                    strErrorMessage = "ByteToHexStr IMEI fail.";
                    return false;
                }
                strIMEI = strNVIMEI;
                string strDateTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                strQCNFileName = strDateTime + "-U_" + strNVIMEI + ".XQCN";
                strQCNFilePathName = strQCNFilePath + "\\" + strQCNFileName;

                #endregion

                #region QLIB_DownloadQcnFile_V2

                bRes = false;
                for (int i = 0; i < 3; i++)
                {
                    bResult = QLIB_DownloadQcnFile_V2(hHandle, strQCNFilePathName, "000000");
                    if (bResult == 1)
                    {
                        bRes = true;
                        break;
                    }
                    else
                    {
                        bRes = false;
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "QLIB_DownloadQcnFile_V2 fail.";
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "BackupQCN exception." + strr;
                return false;
            }
            finally
            {
                #region QLIB_DisconnectServer

                if (hHandle > 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        bResult = QLIB_DisconnectServer(hHandle);
                        if (bResult == 1)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                #endregion
            }

            return true;
        }

        public bool QLIBSetLibraryMode()
        {
            string strErrorMessage = "";

            try
            {
                if (m_b_ModeSetted == false)
                {
                    QLIB_SetLibraryMode(1); // 0:not use QPST;1:use QPST

                    m_b_ModeSetted = true;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "QLIBSetLibraryMode exception." + strr;
                return false;
            }

            return true;
        }

        public bool QLIBConnectServer(int iCOMPort, ref UInt32 hHandle, ref string strErrorMessage)
        {
            strErrorMessage = "";
            hHandle = 0;
            bool bRes = false;

            try
            {
                lock (QPSTConnectSyncLocker)
                {
                    try
                    {
                        #region QLIB_ConnectServer

                        bRes = false;
                        for (int i = 0; i < 10; i++)
                        {
                            hHandle = QLIB_ConnectServer((UInt32)iCOMPort);
                            if (hHandle <= 0)
                            {
                                System.Threading.Thread.Sleep(3000);
                                bRes = false;
                                continue;
                            }
                            else
                            {
                                bRes = true;
                                break;
                            }
                        }
                        if (bRes == false)
                        {
                            hHandle = 0;
                            strErrorMessage = "QLIB_ConnectServer fail.";
                            return false;
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        string strr = ex.Message;
                        strErrorMessage = "QLIBConnectServer exception." + strr;
                        return false;
                    }
                    finally
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "QPSTConnectSyncLocker exception.";
                return false;
            }

            return true;
        }

        public bool QLIBIsPhoneConnected(UInt32 hHandle, ref string strErrorMessage)
        {
            strErrorMessage = "";
            byte bResult = 0;
            bool bRes = false;

            try
            {
                #region QLIB_IsPhoneConnected

                if (hHandle <= 0)
                {
                    strErrorMessage = "Invalid handle.";
                    return false;
                }

                bRes = false;
                for (int i = 0; i < 30; i++)
                {
                    bResult = QLIB_IsPhoneConnected(hHandle);
                    if (bResult == 1)
                    {
                        bRes = true;
                        break;
                    }
                    else
                    {
                        bRes = false;
                        System.Threading.Thread.Sleep(3000);
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "QLIB_IsPhoneConnected fail.";
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "QLIBIsPhoneConnected exception." + strr;
                return false;
            }

            return true;
        }

        public bool QLIBDisconnectServer(UInt32 hHandle, ref string strErrorMessage)
        {
            strErrorMessage = "";
            byte bResult = 0;
            bool bRes = false;

            try
            {
                lock (QPSTConnectSyncLocker)
                {
                    try
                    {
                        #region QLIB_DisconnectServer

                        if (hHandle <= 0)
                        {
                            strErrorMessage = "Invalid handle.";
                            return false;
                        }
                        if (hHandle > 0)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                bResult = QLIB_DisconnectServer(hHandle);
                                if (bResult == 1)
                                {
                                    bRes = true;
                                    break;
                                }
                                else
                                {
                                    bRes = false;
                                    Thread.Sleep(3000);
                                    continue;
                                }
                            }
                        }
                        if (bRes == false)
                        {
                            strErrorMessage = "QLIB_DisconnectServer fail.";
                            return false;
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        string strr = ex.Message;
                        strErrorMessage = "QLIBDisconnectServer exception." + strr;
                        return false;
                    }
                    finally
                    {
                        System.Threading.Thread.Sleep(3000);

                        if (FindAtmnServer() == false)
                        {
                            // Wait QPST Server stop
                            System.Threading.Thread.Sleep(30000);
                        }
                        else
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "QPSTConnectSyncLocker exception.";
                return false;
            }

            return true;
        }

        public bool QLIBDIAGNVREADF_IMEI(UInt32 hHandle, ref string strIMEI, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strIMEI = "";
            byte bResult = 0;
            bool bRes = false;
            string strNVIMEI = "";

            try
            {
                if (hHandle <= 0)
                {
                    strErrorMessage = "Invalid handle.";
                    return false;
                }

                #region QLIB_DIAG_NV_READ_F

                bRes = false;
                ushort uStatus = 0;
                byte[] bIMEI = new byte[10];
                for (int i = 0; i < 3; i++)
                {
                    bResult = QLIB_DIAG_NV_READ_F(hHandle, 550, bIMEI, 10, ref uStatus);
                    if (bResult == 1)
                    {
                        if (uStatus == 0)
                        {
                            bRes = true;
                            break;
                        }
                        else if (uStatus == 5)
                        {
                            // No MEID, Please Check whether WLAN Version
                            bRes = false;
                            Thread.Sleep(1000);
                            continue;
                        }
                        else
                        {
                            bRes = false;
                            Thread.Sleep(1000);
                            continue;
                        }
                    }
                    else
                    {
                        bRes = false;
                        Thread.Sleep(1000);
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "QLIB_DIAG_NV_READ_F fail.Status:" + uStatus.ToString();
                    return false;
                }

                #endregion

                if (ByteToHexStr(bIMEI, ref strNVIMEI) == false)
                {
                    strErrorMessage = "ByteToHexStr IMEI fail.";
                    return false;
                }
                strIMEI = strNVIMEI;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "QLIB_DIAG_NV_READ_F exception." + strr;
                return false;
            }

            return true;
        }

        public bool QLIBDownloadQcnFileV2(UInt32 hHandle, string strQCNFilePath, string strQCNFileName, ref string strErrorMessage)
        {
            strErrorMessage = "";
            byte bResult = 0;
            bool bRes = false;
            string strQCNFilePathName = "";

            try
            {
                if (hHandle <= 0)
                {
                    strErrorMessage = "Invalid handle.";
                    return false;
                }
                if (strQCNFilePath.Substring(strQCNFilePath.Length - 1, 1) == "\\")
                {
                    strQCNFilePath = strQCNFilePath.Substring(0, strQCNFilePath.Length - 1);
                }
                if (Directory.Exists(strQCNFilePath) == false)
                {
                    strErrorMessage = "QCN path not exist." + strQCNFilePath;
                    return false;
                }
                strQCNFilePathName = strQCNFilePath + "\\" + strQCNFileName;

                //Warning: It will take much time, if usb cable disconnect, thread block.
                #region QLIB_DownloadQcnFile_V2

                bRes = false;
                DownloadThread myThread = new DownloadThread(hHandle, strQCNFilePathName);
                Thread DLThread = new Thread(new ThreadStart(myThread.Main));

                DLThread.Start();
                bRes = DLThread.Join(360000);   //timeout 360s will return false, success return true.

                #region Old Method

                //for (int i = 0; i < 3; i++)
                //{
                //    bResult = QLIB_DownloadQcnFile_V2(hHandle, strQCNFilePathName, "000000");

                //    if (bResult == 1)
                //    {
                //        bRes = true;
                //        break;
                //    }
                //    else
                //    {
                //        bRes = false;
                //        Thread.Sleep(3000);
                //        continue;
                //    }
                //}

                #endregion

                if (bRes == false)
                {
                    strErrorMessage = "QLIB_DownloadQcnFile_V2 fail.";
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "QLIBDownloadQcnFileV2 exception." + strr;
                return false;
            }

            return true;
        }

        private bool ByteToHexStr(byte[] bByte, ref string strHexStr)
        {
            try
            {
                strHexStr = "";
                string strTemp = "";

                if (bByte.Length < 10)
                {
                    return false;
                }

                strTemp = bByte[1].ToString("X2");
                strHexStr += strTemp.Substring(0, 1);

                strTemp = bByte[2].ToString("X2");
                strHexStr += strTemp.Substring(1, 1) + strTemp.Substring(0, 1);

                strTemp = bByte[3].ToString("X2");
                strHexStr += strTemp.Substring(1, 1) + strTemp.Substring(0, 1);

                strTemp = bByte[4].ToString("X2");
                strHexStr += strTemp.Substring(1, 1) + strTemp.Substring(0, 1);

                strTemp = bByte[5].ToString("X2");
                strHexStr += strTemp.Substring(1, 1) + strTemp.Substring(0, 1);

                strTemp = bByte[6].ToString("X2");
                strHexStr += strTemp.Substring(1, 1) + strTemp.Substring(0, 1);

                strTemp = bByte[7].ToString("X2");
                strHexStr += strTemp.Substring(1, 1) + strTemp.Substring(0, 1);

                strTemp = bByte[8].ToString("X2");
                strHexStr += strTemp.Substring(1, 1) + strTemp.Substring(0, 1);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool FindAtmnServer()
        {
            try
            {
                bool bRes = false;
                string strProcess = "";
                clsExecProcess objprocess = new clsExecProcess();

                // QPST OLE Automation Server
                strProcess = "AtmnServer";
                if (objprocess.FindProcess(strProcess) == false)
                {
                    bRes = false;
                }
                else
                {
                    return true;
                }

                if (bRes == false)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        #endregion
    }
}
