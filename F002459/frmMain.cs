using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using F002459.Common;
using F002459.Forms;
using F002459.Properties;
using System.Diagnostics;
using Newtonsoft.Json;
using SmartFactory.ExternalDLL;

namespace F002459
{
    public partial class frmMain : Form
    {
        #region Structure

        private struct OptionData
        {
            // Option
            public string TestMode;
            public string DAQDevice;
            public string Area_Location;
            public string MES_Enable;
            public string MES_Station;
            public string PLCIP;
            public string PLCPort;
            public int DB_Slot1_ReadDB;
            public int DB_Slot1_WriteDB;
            public int DB_Slot2_ReadDB;
            public int DB_Slot2_WriteDB;
            public int DB_Slot3_ReadDB;
            public int DB_Slot3_WriteDB;
            public int DB_Slot4_ReadDB;
            public int DB_Slot4_WriteDB;

            // Setup
            public string ADBDeviceName;
            public string QDLoaderPortName;
            public string DeviceAddress_Panel1;
            public string DeviceAddress_Panel2;
            public string DeviceAddress_Panel3;
            public string DeviceAddress_Panel4;        
        }

        private struct ModelOption
        {
            // QCN
            public string QCNFilePath;
            public string QCNFileSize;

            // Matrix
            public int MatrixWWANPos;

            // MDCS
            public string MDCSEnable;
            public string MDCSURL;
            public string MDCSDeviceName;
            public string MDCSPreStationResultCheck;
            public string MDCSPreStationDeviceName;
            public string MDCSPreStationVarName;
            public string MDCSPreStationVarValue;
        }

        private struct UnitDeviceInfo
        {
            public string Panel;    
            public string SN;
            public string SKU;
            public string Model;
            public string EID;
            public string WorkOrder;
            public string Status;
            public string PhysicalAddress;
        }

        #endregion

        #region Variable

        private bool m_bCollapse = true;
        private string m_PreStationDeviceName = "";
        private OptionData m_st_OptionData = new OptionData();
        private Dictionary<string, UnitDeviceInfo> m_dic_UnitDevice = new Dictionary<string, UnitDeviceInfo>();
        private Dictionary<string, ModelOption> m_dic_ModelOption = new Dictionary<string, ModelOption>();
        private Dictionary<string, TestSaveData> m_dic_TestSaveData = new Dictionary<string, TestSaveData>();
        private Dictionary<string, bool> m_dic_TestStatus = new Dictionary<string, bool>();      // true:Running false:Not Running
        private Dictionary<string, UInt32> m_dic_TestHandle = new Dictionary<string, UInt32>();  // true:Running false:Not Running
        private Dictionary<string, string> m_dic_COMPort = new Dictionary<string, string>();

        private const string PANEL_1 = "1";
        private const string PANEL_2 = "2";
        private const string PANEL_3 = "3";
        private const string PANEL_4 = "4";
        private const string STATUS_CONNECTED = "Connected";
        private const string STATUS_DISCONNECTED = "Not Connected";
        private const string STATUS_ONGOING = "Ongoing";
        private const string STATUS_SUCCESSED = "PASS";
        private const string STATUS_FAILED = "FAIL";

        private clsQMSL m_cls_QMSL = new clsQMSL();
        private static readonly object QPSTSyncLocker = new object();
        private static readonly object m_obj_SaveLogLocker = new object();

        private clsCPLCDave m_obj_PLC = null;
        private System.Threading.Timer m_timer_WatchDog = null;
        private bool m_b_PLCRuning = false;
        private int m_i_WatchDog = 0;
        
        #endregion

        #region Form

        public frmMain()
        {
            InitializeComponent();
            CollapseMenu(true);

            // Form
            this.Text = string.Empty;
            this.ControlBox = false;
            this.DoubleBuffered = true;
            this.MaximizedBounds = Screen.PrimaryScreen.WorkingArea;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev;

            if (InitRun() == false)
            {
                return;
            }

            if (m_st_OptionData.TestMode == "1")
            {
                //lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + " [Auto Test] "   + m_st_MCFData.SKU + " " + m_st_MESData.EID + " " + m_st_MESData.WorkOrder;
                lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + "[Auto Test]";
            }
            else
            {
                //lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + " [Manual Test] " + m_st_MCFData.SKU + " " + m_st_MESData.EID + " " + m_st_MESData.WorkOrder;
                lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + "[Manual Test]";
            }

            return;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            bgFlashWorkerCancel();
            KillQPST();

            if (m_st_OptionData.TestMode == "1")
            {
                PLCRelease();
            }
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            this.lblTitleBar.Width = this.Width - 105;
        }

        #endregion

        #region Event

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                // Maximized
                this.WindowState = FormWindowState.Maximized;
                this.btnMaximize.Image = Resources.normal_16;
                if (m_bCollapse)
                {
                    this.panelMenu.Width = 80;
                }
                else
                {
                    this.panelMenu.Width = 250;
                }

                this.panelLog.Height = 160;
            }
            else
            {
                // Normal
                this.WindowState = FormWindowState.Normal;
                this.btnMaximize.Image = Resources.maximize_16;
                if (m_bCollapse)
                {
                    this.panelMenu.Width = 80;
                }
                else
                {
                    this.panelMenu.Width = 200;
                }

                this.panelLog.Height = 145;
            }
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void lblTitleBar_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Only can use mouse right button, because left button event conflict with MouseDown event.
            if (e.Button == MouseButtons.Right)
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    this.WindowState = FormWindowState.Maximized;
                    this.btnMaximize.Image = Resources.normal_16;
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                    this.btnMaximize.Image = Resources.maximize_16;
                }
            }
        }

        private void lblTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.panelMenu.Width > 200)
            {
                this.panelMenu.Width = 200;
            }
            this.panelLog.Height = 145;

            Win32.ReleaseCapture();
            Win32.SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private void picBoxLogo_Click(object sender, EventArgs e)
        {
            m_bCollapse = true;
            CollapseMenu(true);
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            m_bCollapse = false;
            CollapseMenu(false);
        }
 
        private void panelDesktop_Resize(object sender, EventArgs e)
        {
            int InnerHeight = this.panelUnits.Height - (this.panelUnits.Padding.Top + this.panelUnits.Padding.Bottom);
            int InnerWidth = this.panelUnits.Width - (this.panelUnits.Padding.Left + this.panelUnits.Padding.Right);

            this.panelUnitTop.Height = InnerHeight / 2;
            this.panelUnitTop.Width = InnerWidth / 2;
            this.panelUnitBottom.Height = InnerHeight / 2;
            this.panelUnitBottom.Width = InnerWidth / 2;

            this.panelUnit1.Width = InnerWidth / 2;
            this.panelUnit1.Height = InnerHeight / 2;

            this.panelUnit2.Width = InnerWidth / 2;
            this.panelUnit2.Height = InnerHeight / 2;

            this.panelUnit3.Width = InnerWidth / 2;
            this.panelUnit3.Height = InnerHeight / 2;

            this.panelUnit4.Width = InnerWidth / 2;
            this.panelUnit4.Height = InnerHeight / 2;
        }

        #endregion

        #region Override

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams paras = base.CreateParams;
                paras.ExStyle |= 0x02000000;
                return paras;
            }
        }

        #endregion

        #region Menu

        private void Exit_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PortMapping_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_st_OptionData.TestMode == "1")
            {
                timerAutoTest.Enabled = false;
                if (m_timer_WatchDog != null)
                {
                    m_timer_WatchDog.Dispose();
                    m_timer_WatchDog = null;
                }
            }
            else
            {
                timerMonitor.Enabled = false;
                timerDeviceConnect.Enabled = false;
            }

            frmSetupUSB frm = new frmSetupUSB();
            frm.ShowDialog();
        }

        private void deleteCOMNameArbiterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DeleteCOMNameArbiterReg() == false)
            {
                MessageBox.Show("Delete COM Name Arbiter Reg Fail.");
            }
            else
            {
                MessageBox.Show("Delete COMName Arbiter Reg Successfully.");
            }
        }

        private void hWSerEmulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (HWSerNumEmulationReg() == false)
            {
                MessageBox.Show("HWSerNumEmulation Reg Fail.");
            }
            else
            {
                MessageBox.Show("HWSerNumEmulation Reg Successfully.");
            }
        }

        private void killQPSTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KillQPST();
        }

        #endregion

        #region Timer

        private void timerMonitorRun_Tick(object sender, EventArgs e)
        {
            timerMonitor.Enabled = false;

            try
            {
                lock (QPSTSyncLocker)
                {
                    MonitorDeviceByPhysicalAddress(PANEL_1);
                    Dly(1);

                    MonitorDeviceByPhysicalAddress(PANEL_2);
                    Dly(1);

                    MonitorDeviceByPhysicalAddress(PANEL_3);
                    Dly(1);

                    MonitorDeviceByPhysicalAddress(PANEL_4);
                    Dly(1);
                }
            }
            catch (Exception ex)
            {
                DisplayMessage("timerMonitorRun Exception:" + ex.Message);
                return;
            }
            finally
            {
                timerMonitor.Enabled = true;
            }

            return;
        }

        private void timerMonitorDeviceConnect_Tick(object sender, EventArgs e)
        {
            timerDeviceConnect.Enabled = false;

            try
            {
                MonitorDeviceConnected(PANEL_1);
                Dly(1);

                MonitorDeviceConnected(PANEL_2);
                Dly(1);

                MonitorDeviceConnected(PANEL_3);
                Dly(1);

                MonitorDeviceConnected(PANEL_4);
                Dly(1);
            }
            catch (Exception ex)
            {
                DisplayMessage("timerMonitorDeviceConnect Exception:" + ex.Message);
                return;
            }
            finally
            {
                timerDeviceConnect.Enabled = true;
            }

            return;
        }

        private void timerKillProcess_Tick(object sender, EventArgs e)
        {
            //try
            //{
            //    timerKillProcess.Enabled = false;

            //    lock (QPSTSyncLocker)
            //    {
            //        if (m_dic_TestStatus[PANEL_1] == false && m_dic_TestStatus[PANEL_2] == false && m_dic_TestStatus[PANEL_3] == false && m_dic_TestStatus[PANEL_4] == false)
            //        {
            //            //if (FindQPST() == true)
            //            //{
            //            //    DisplayMessage("Kill QPST process.");
            //            //    KillQPST();
            //            //    Dly(5);
            //            //}
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    string strr = ex.Message;
            //    DisplayMessage("timerKillProcess Exception:" + strr);
            //    return;
            //}
            //finally
            //{
            //    timerKillProcess.Enabled = true;
            //}

            return;
        }

        private void timerClearReg_Tick(object sender, EventArgs e)
        {
            //try
            //{
            //    timerClearReg.Enabled = false;

            //    if (m_dic_TestStatus[PANEL_1] == false && m_dic_TestStatus[PANEL_2] == false && m_dic_TestStatus[PANEL_3] == false && m_dic_TestStatus[PANEL_4] == false)
            //    {
            //        //DisplayMessage("Clear COM port arbiter reg.");
            //        //DeleteCOMNameArbiterReg();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    string strr = ex.Message;
            //    DisplayMessage("timerClearReg Exception:" + strr);
            //    return;
            //}
            //finally
            //{
            //    timerClearReg.Enabled = true;
            //}

            return;
        }

        private void timerAutoTest_Tick(object sender, EventArgs e)
        {
            timerAutoTest.Enabled = false;

            try
            {
                AutoTest(PANEL_1);
                Dly(1);
                AutoTest(PANEL_2);
                Dly(1);
                AutoTest(PANEL_3);
                Dly(1);
                AutoTest(PANEL_4);
                Dly(1);
            }
            catch (Exception ex)
            {
                DisplayMessage("timerAutoTest Exception:" + ex.Message);
                return;
            }
            finally
            {
                timerAutoTest.Enabled = true;
            }

            return;
        }

        #endregion

        #region Function

        #region Monitor

        private bool MonitorDeviceByPhysicalAddress(string strPanel)
        {
            try
            {
                string strPhysicalAddress = "";
                string strErrorMessage = "";
                string strDeviceName = "";
                USBEnumerator usbEnum = new USBEnumerator();          
                List<string> m_List_SN = new List<string>();          
                m_List_SN.Clear();
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                strDeviceName = m_st_OptionData.ADBDeviceName;

                if (usbEnum.GetSerianNumberByUSBDevicePhysicalAddress(strPhysicalAddress, strDeviceName, ref m_List_SN, ref strErrorMessage) == false)
                {
                    DisplayMessage(strErrorMessage);
                    return false;
                }
                if (m_List_SN.Count == 1)
                {
                    #region Count=1

                    string strSN = m_List_SN[0].Trim().ToString();
                    if (CheckSN(strSN) == false)
                    {
                        DisplayMessage("Panel " + strPanel + " invalid SN." + strSN);
                        return false;
                    }
                    if (m_dic_UnitDevice[strPanel].SN == strSN)
                    {
                        // 同一个产品
                        string strStatus = m_dic_UnitDevice[strPanel].Status;
                        if (strStatus == "0")
                        {
                            // 未连接
                        }
                        else if (strStatus == "1")
                        {
                            // 连接上，进行中
                        }
                        else if (strStatus == "P")
                        {
                            // 成功，不可以重新开始（只能更换USB口）
                        }
                        else if (strStatus == "F")
                        {
                            // 失败，不可以重新开始（只能更换USB口）
                            DisplayMessage("Panel " + strPanel + " change one unit to test.");
                        }
                    }
                    else
                    {
                        m_dic_COMPort[strPanel] = "";

                        // 不同产品
                        #region STATUS_CONNECTED

                        UnitDeviceInfo stUnit = new UnitDeviceInfo();
                        stUnit.Panel = strPanel;
                        stUnit.PhysicalAddress = strPhysicalAddress;
                        stUnit.SN = strSN;
                        stUnit.Status = "1";
                        m_dic_UnitDevice[strPanel] = stUnit;

                        DisplayUnit(strPanel, strSN, Color.White);
                        DisplayUnitStatus(strPanel, STATUS_CONNECTED, Color.MediumSpringGreen);

                        #endregion

                        RunFlashWorker(strPanel);
                    }

                    #endregion
                }
                else
                {
                    m_dic_COMPort[strPanel] = "";
                    if (m_List_SN.Count > 1)
                    {
                        DisplayMessage("Panel " + strPanel + " mmonitor more unit:" + m_List_SN.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Monitor device SN Exception:" + strr);
                return false;
            }

            return true;
        }

        private bool MonitorDeviceConnected(string strPanel)
        {
            try
            {
                #region Monitor Connected/Disconnected

                if (MonitorPortConnected(strPanel) == false)
                {
                    string strStatus = m_dic_UnitDevice[strPanel].Status;
                    if (strStatus == "P" || strStatus == "F")
                    {
                        // 测试完成
                    }
                    else if (strStatus == "1")
                    {
                        m_dic_COMPort[strPanel] = "";
                        //m_dic_TestStatus[strPanel] = false;
                        DisplayUnitLog(strPanel, "Unit disconnect ......");
                    }
                    else
                    {
                        UnitDeviceInfo stUnit = new UnitDeviceInfo();
                        stUnit.Panel = m_dic_UnitDevice[strPanel].Panel;
                        stUnit.PhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                        stUnit.SN = m_dic_UnitDevice[strPanel].SN;
                        stUnit.Status = "0";
                        m_dic_UnitDevice[strPanel] = stUnit;

                        DisplayUnit(strPanel, m_dic_UnitDevice[strPanel].SN, Color.White);
                        DisplayUnitStatus(strPanel, STATUS_DISCONNECTED, Color.Orange);
                    }
                }
                else
                {
                }

                #endregion
            }
            catch (Exception ex)
            {
                DisplayMessage("Monitor device connected Exception:" + ex.Message);
                return false;
            }

            return true;
        }

        private bool MonitorADBConnected(string strPanel)
        {
            try
            {
                USBEnumerator usbEnum = new USBEnumerator();
                string strPhysicalAddress = "";
                string strErrorMessage = "";
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                bool bRes = false;
                bRes = usbEnum.CheckADBConnectByUSBDevicePhysicalAddress(strPhysicalAddress, ref strErrorMessage);
                if (bRes == false)
                {
                    // 连接失败
                    return false;
                }
                else
                {
                    // 连接成功
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Monitor ADB connected Exception:" + strr);
                return false;
            }

            return true;
        }

        private bool MonitorPortConnected(string strPanel)
        {
            try
            {
                USBEnumerator usbEnum = new USBEnumerator();
                string strPhysicalAddress = "";
                string strErrorMessage = "";
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                bool bRes = false;
                string strPortName = m_st_OptionData.QDLoaderPortName;
                bRes = usbEnum.CheckPortConnectByUSBDevicePhysicalAddress(strPhysicalAddress, strPortName, ref strErrorMessage);
                if (bRes == false)
                {
                    // 连接失败
                    return false;
                }
                else
                {
                    // 连接成功
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Monitor QDLoader connected Exception:" + strr);
                return false;
            }

            return true;
        }

        private bool MonitorDeviceByPhysicalAddress_AutoTest(string strPanel, ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";
                string strDeviceName = "";
                string strPhysicalAddress = "";
                USBEnumerator usbEnum = new USBEnumerator();
                List<string> m_List_SN = new List<string>();

                m_List_SN.Clear();
                strDeviceName = m_st_OptionData.ADBDeviceName;
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;

                UnitDeviceInfo stUnit1 = new UnitDeviceInfo();
                stUnit1.Panel = strPanel;
                stUnit1.PhysicalAddress = strPhysicalAddress;
                stUnit1.SN = "";
                stUnit1.SKU = "";
                stUnit1.Model = "";
                stUnit1.EID = "";
                stUnit1.WorkOrder = "";
                stUnit1.Status = "0";
                m_dic_UnitDevice[strPanel] = stUnit1;

                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = usbEnum.GetSerianNumberByUSBDevicePhysicalAddress(strPhysicalAddress, strDeviceName, ref m_List_SN, ref strErrorMessage);
                    if (bRes == false)
                    {
                        strErrorMessage = "Failed to monitor device get SN." + strErrorMessage;
                        bRes = false;
                        Dly(3);
                        continue;
                    }

                    if (m_List_SN.Count == 0)
                    {
                        strErrorMessage = "Failed to monitor device.";
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                    else if (m_List_SN.Count == 1)
                    {
                        #region RunTest

                        string strSN = m_List_SN[0].Trim().ToString();
                        if (CheckSN(strSN) == false)
                        {
                            strErrorMessage = "Failed to check SN: " + strSN;
                            bRes = false;
                            Dly(1);
                            continue;
                        }

                        m_dic_COMPort[strPanel] = "";

                        UnitDeviceInfo stUnit = new UnitDeviceInfo();
                        stUnit.Panel = strPanel;
                        stUnit.PhysicalAddress = strPhysicalAddress;
                        stUnit.SN = strSN;
                        stUnit.Status = "1";
                        m_dic_UnitDevice[strPanel] = stUnit;

                        DisplayUnit(strPanel, strSN, Color.White);
                        DisplayUnitStatus(strPanel, STATUS_CONNECTED, Color.MediumSpringGreen);

                        RunFlashWorker(strPanel);   // Start RunTest

                        strErrorMessage = "";
                        bRes = true;    
                        break;

                        #endregion
                    }
                    else
                    {
                        strErrorMessage = "Failed to monitor device SN count:" + m_List_SN.Count.ToString();
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                }

                if (bRes == false)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Failed to monitor device exception:" + ex.Message; 
                return false;
            }

            return true;
        }

        private bool MonitorDeviceByPhysicalAddress_AutoTest_New(string strPanel, ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";

                string strDeviceName = "";
                string strPhysicalAddress = "";
                USBEnumerator usbEnum = new USBEnumerator();
                List<string> m_List_SN = new List<string>();

                m_List_SN.Clear();
                strDeviceName = m_st_OptionData.ADBDeviceName;
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;

                UnitDeviceInfo stUnit1 = new UnitDeviceInfo();
                stUnit1.Panel = strPanel;
                stUnit1.PhysicalAddress = strPhysicalAddress;
                stUnit1.SN = "";
                stUnit1.Status = "0";
                m_dic_UnitDevice[strPanel] = stUnit1;

                #region Get Serial Number

                bool bRes = false;
                string strSN = "";

                for (int i = 0; i < 20; i++)
                {
                    bRes = usbEnum.GetSerianNumberByUSBDevicePhysicalAddress(strPhysicalAddress, strDeviceName, ref m_List_SN, ref strErrorMessage);
                    if (bRes == false)
                    {
                        strErrorMessage = "Failed to monitor device get SN." + strErrorMessage;
                        bRes = false;
                        Dly(3);
                        continue;
                    }
                    else
                    {
                        if (m_List_SN.Count == 0)
                        {
                            strErrorMessage = "Failed to get SN, SN is empty.";
                            bRes = false;
                            Dly(3);
                            continue;
                        }

                        if (m_List_SN.Count == 1)
                        {
                            #region Check SN

                            strSN = m_List_SN[0].Trim().ToString();
                            if (CheckSN(strSN) == false)
                            {
                                strErrorMessage = "Failed to check SN: " + strSN;
                                bRes = false;
                                Dly(1);
                                continue;
                            }
                            else
                            {
                                bRes = true;
                                break;
                            }

                            #endregion
                        }
                    }
                }

                if (bRes == false)
                {
                    strErrorMessage = "Failed to get SN, SN count: " + m_List_SN.Count.ToString();
                    return false;
                }

                #endregion

                #region RunTest

                m_dic_COMPort[strPanel] = "";

                UnitDeviceInfo stUnit = new UnitDeviceInfo();
                stUnit.Panel = strPanel;
                stUnit.PhysicalAddress = strPhysicalAddress;
                stUnit.SN = strSN;
                stUnit.Status = "1";
                m_dic_UnitDevice[strPanel] = stUnit;

                DisplayUnit(strPanel, strSN, Color.White);
                DisplayUnitStatus(strPanel, STATUS_CONNECTED, Color.MediumSpringGreen);

                RunFlashWorker(strPanel);   // Start RunTest

                bRes = true;

                #endregion

                if (bRes == false)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Failed to monitor device exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        #endregion

        #region TestItem

        private bool RunFlashWorker(string strPanel)
        {
            try
            {
                if (strPanel == PANEL_1)
                {
                    if (!bgFlashWorker1.IsBusy)
                    {
                        bgFlashWorker1.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker1.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker1.IsBusy)
                        {
                            bgFlashWorker1.RunWorkerAsync();
                        }
                    }
                }
                else if (strPanel == PANEL_2)
                {
                    if (!bgFlashWorker2.IsBusy)
                    {
                        bgFlashWorker2.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker2.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker2.IsBusy)
                        {
                            bgFlashWorker2.RunWorkerAsync();
                        }
                    }
                }
                else if (strPanel == PANEL_3)
                {
                    if (!bgFlashWorker3.IsBusy)
                    {
                        bgFlashWorker3.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker3.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker3.IsBusy)
                        {
                            bgFlashWorker3.RunWorkerAsync();
                        }
                    }
                }
                else if (strPanel == PANEL_4)
                {
                    if (!bgFlashWorker4.IsBusy)
                    {
                        bgFlashWorker4.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker4.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker4.IsBusy)
                        {
                            bgFlashWorker4.RunWorkerAsync();
                        }
                    }
                }
                else
                {
                    DisplayMessage("Failed to flash worker invalid pannel.");
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Failed to exception." + strr);
                return false;
            }

            return true;
        }

        private bool CancelFlashWorker(string strPanel)
        {
            try
            {
                if (strPanel == PANEL_1)
                {
                    if (bgFlashWorker1.IsBusy)
                    {
                        bgFlashWorker1.CancelAsync();
                    }
                }
                else if (strPanel == PANEL_2)
                {
                    if (bgFlashWorker2.IsBusy)
                    {
                        bgFlashWorker2.CancelAsync();
                    }
                }
                else if (strPanel == PANEL_3)
                {
                    if (bgFlashWorker3.IsBusy)
                    {
                        bgFlashWorker3.CancelAsync();
                    }
                }
                else if (strPanel == PANEL_4)
                {
                    if (bgFlashWorker4.IsBusy)
                    {
                        bgFlashWorker4.CancelAsync();
                    }
                }
                else
                {
                    DisplayMessage("Failed to flash worker invalid pannel.");
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Failed to exception." + strr);
                return false;
            }

            return true;
        }

        private bool RunTest(string strPanel)
        {
            bool bRes = true;
            bool bUpdateMDCS = true;
            bool bUploadMES = false;

            try
            {
                m_dic_TestStatus[strPanel] = true;

                string strErrorMessage = "";
                double dTotalTestTime = 0;
                long startTime = clsUtil.StartTimeInTicks();

                this.Invoke((MethodInvoker)delegate { ClearUnitLog(strPanel); });
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Start Test Time:" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); });

                #region Init

                this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_ONGOING, Color.YellowGreen); });
                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.Status = "1";
                m_dic_UnitDevice[strPanel] = stUnit;
       
                InitMDCSData(strPanel);
                InitModelOptionData(strPanel);

                #endregion

                #region Get SKU Property

                if (bRes == true)
                {
                    bRes = TestGetSKU(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to get SKU property." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Get WorkOrder Property
                // Remark: WorkOrder sometimes maybe not written in property, get from scansheet.
                if (bRes == true)
                {
                    if (m_st_OptionData.MES_Enable == "1")
                    {
                        bRes = TestGetWorkOrder(strPanel, ref strErrorMessage);
                        if (bRes == false)
                        {
                            bUpdateMDCS = false;
                            bRes = false;
                            strErrorMessage = "Failed to get WorkOrder property." + strErrorMessage;
                        }
                        else
                        {
                            bUpdateMDCS = true;
                            bRes = true;
                        }
                    }        
                }

                #endregion

                #region Get EID Property

                if (bRes == true)
                {
                    if (m_st_OptionData.MES_Enable == "1")
                    {
                        bRes = TestGetEID(strPanel, ref strErrorMessage);
                        if (bRes == false)
                        {
                            bUpdateMDCS = false;
                            bRes = false;
                            strErrorMessage = "Failed to get EID property." + strErrorMessage;
                        }
                        else
                        {
                            bUpdateMDCS = true;
                            bRes = true;
                        }
                    }            
                }

                #endregion

                #region Read Model_Option.ini

                if (bRes == true)
                {
                    bRes = TestReadModelOption(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to read Model_Option.ini." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Check MES Data

                if (bRes == true)
                {
                    bRes = TestCheckMESData(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to check MES data." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Test Check Pre Station Result

                if (bRes == true)
                {
                    bRes = TestCheckPreStationResult(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to check pre station result." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Test QCN Backup

                if (bRes == true)
                {
                    // bRes = true;              
                    bRes = TestQCNBackup(strPanel, ref strErrorMessage);

                    if (bRes == false)
                    {
                        bRes = false;
                        bUpdateMDCS = true;
                        strErrorMessage = "Failed to test QCN backup." + strErrorMessage;
                    }
                    else
                    {
                        strErrorMessage = "";
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region MDCS Data

                dTotalTestTime = clsUtil.ElapseTimeInSeconds(startTime);

                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];

                objSaveData.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData.TestRecord.SN = m_dic_UnitDevice[strPanel].SN;
                objSaveData.TestRecord.Model = m_dic_UnitDevice[strPanel].Model;
                objSaveData.TestRecord.SKU = m_dic_UnitDevice[strPanel].SKU;
                objSaveData.TestRecord.TestTotalTime = dTotalTestTime;

                if (bRes == true)
                {
                    objSaveData.TestResult.TestPassed = true;
                    objSaveData.TestResult.TestFailCode = 0;
                    objSaveData.TestResult.TestFailMessage = "";
                    objSaveData.TestResult.TestStatus = "";
                }
                else
                {
                    objSaveData.TestResult.TestPassed = false;
                    objSaveData.TestResult.TestFailCode = 2050;
                    objSaveData.TestResult.TestFailMessage = strErrorMessage;
                    objSaveData.TestResult.TestStatus = "";
                }

                m_dic_TestSaveData[strPanel] = objSaveData;

                #endregion

                #region Save MDCS

                if (m_dic_ModelOption[strPanel].MDCSEnable == "1")
                {
                    if (bUpdateMDCS == true)
                    {
                        bool bSaveMDCS = false;
                        bSaveMDCS = SaveMDCS(strPanel, objSaveData);
                        if (bSaveMDCS == false)
                        {
                            bSaveMDCS = SaveMDCS(strPanel, objSaveData);
                        }

                        if (bSaveMDCS == false)
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Save MDCS Fail."); });
                            if (objSaveData.TestResult.TestPassed == true)
                            {
                                objSaveData.TestResult.TestPassed = false;
                                objSaveData.TestResult.TestFailCode = 2050;
                                objSaveData.TestResult.TestFailMessage = "Failed to save MDCS.";
                                objSaveData.TestResult.TestStatus = "";
                                m_dic_TestSaveData[strPanel] = objSaveData;
                            }
                            bRes = false;
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Save MDCS Successfully."); });
                        }

                        #region Obsolete

                        if (bSaveMDCS == false)
                        {
                            bRes = false;
                        }

                        #endregion
                    }
                }

                #endregion

                #region Upload MES

                if (m_st_OptionData.MES_Enable == "1")
                {
                    string strTempErrorMsg = "";
                    bool bPassFailFlag = false;
                    string strEID = m_dic_UnitDevice[strPanel].EID;
                    string strStation = m_st_OptionData.MES_Station;
                    string strWorkOrder = m_dic_UnitDevice[strPanel].WorkOrder;
                    string strSN = m_dic_UnitDevice[strPanel].SN;

                    if (objSaveData.TestResult.TestPassed == true)
                    {
                        bPassFailFlag = true;
                    }
                    else
                    {
                        bPassFailFlag = false;
                    }

                    bUploadMES = MESUploadData(strEID, strStation, strWorkOrder, strSN, bPassFailFlag, ref strTempErrorMsg);
                    if (bUploadMES == false)
                    {
                        bUploadMES = MESUploadData(strEID, strStation, strWorkOrder, strSN, bPassFailFlag, ref strTempErrorMsg);
                    }
                    if (bUploadMES == false)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Upload MES Fail." + strTempErrorMsg); });

                        if (objSaveData.TestResult.TestPassed == true)
                        {
                            objSaveData.TestResult.TestPassed = false;
                            objSaveData.TestResult.TestFailCode = 2050;
                            objSaveData.TestResult.TestFailMessage = "Failed to upload MES.";
                            objSaveData.TestResult.TestStatus = "";
                            m_dic_TestSaveData[strPanel] = objSaveData;
                        }
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Upload MES Successfully."); });
                    }

                    if (bUploadMES == false)
                    {
                        bRes = false;
                    }
                }

                #endregion
              
                #region Save Test Report

                this.Invoke((MethodInvoker)delegate { SaveUnitTestReport(strPanel); });

                #endregion

                #region Test End

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "End Test Time:" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); });
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Total Test Time:" + dTotalTestTime.ToString() + " s."); });

                if (strErrorMessage != "")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Error Message:" + strErrorMessage); });
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;

                #region TestSaveData

                bRes = false;
                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];
                if (objSaveData.TestResult.TestPassed == true)
                {
                    objSaveData.TestResult.TestPassed = false;
                    objSaveData.TestResult.TestFailCode = 2050;
                    objSaveData.TestResult.TestFailMessage = "RunTest Exception." + strr;
                    objSaveData.TestResult.TestStatus = "";
                    m_dic_TestSaveData[strPanel] = objSaveData;
                }

                #endregion

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "RunTest Exception:" + strr); });
                DisplayMessage("RunTest Exception:" + strr);
                return false;
            }
            finally
            {
                #region STATUS_SUCCESSED / STATUS_FAILED

                if (bRes == true)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_SUCCESSED, Color.Green); });
                    UnitDeviceInfo stUnit1 = m_dic_UnitDevice[strPanel];
                    stUnit1.Status = "P";
                    m_dic_UnitDevice[strPanel] = stUnit1;
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_FAILED, Color.Red); });
                    UnitDeviceInfo stUnit2 = m_dic_UnitDevice[strPanel];
                    stUnit2.Status = "F";
                    m_dic_UnitDevice[strPanel] = stUnit2;
                }

                #endregion

                #region Auto Test

                if (m_st_OptionData.TestMode == "1")
                {
                    string strTempErrorMessage = "";

                    if (FeedbackResult(strPanel, ref strTempErrorMessage) == false)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "FeedbackResult Fail:" + strTempErrorMessage); });
                        SavePLCLogFile("FeedbackResult Fail:" + strTempErrorMessage);
                    }

                    m_dic_COMPort[strPanel] = "";   //Clear ComPort Record When Disconnect.

                    #region Obsolete

                    //if (FeedbackStatus(strPanel, clsCPLCDave.FeedbackStatus.HAVEPRODUCT, ref strTempErrorMessage) == false)
                    //{
                    //    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "FeedbackStatus HAVEPRODUCT Fail:" + strTempErrorMessage); });
                    //    SavePLCLogFile("FeedbackStatus HAVEPRODUCT Fail:" + strTempErrorMessage);
                    //}

                    #endregion
                }

                #endregion

                m_dic_TestStatus[strPanel] = false;
            }

            return true;
        }

        private bool TestGetSKU(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Get SKU Property."); });

                string strSKU = "";
                string strModel = "";
                if (GetSKUProperty(strPanel, ref strSKU, ref strErrorMessage) == false)
                {
                    return false;
                }

                int index = strSKU.IndexOf("-");
                strModel = strSKU.Substring(0, index);

                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.SKU = strSKU;
                stUnit.Model = strModel;
                m_dic_UnitDevice[strPanel] = stUnit;

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get SKU: " + strSKU + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetSKU Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestGetWorkOrder(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Get WorkOrder Property."); });

                string strWorkOrder = "";
                if (GetWorkOrderProperty(strPanel, ref strWorkOrder, ref strErrorMessage) == false)
                {
                    return false;
                }

                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.WorkOrder = strWorkOrder;
                m_dic_UnitDevice[strPanel] = stUnit;

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get WorkOrder: " + strWorkOrder + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetWorkOrder Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestGetEID(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Get EID Property."); });

                string strEID = "";
                if (GetEIDProperty(strPanel, ref strEID, ref strErrorMessage) == false)
                {
                    return false;
                }

                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.EID = strEID;
                m_dic_UnitDevice[strPanel] = stUnit;

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get EID: " + strEID + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetEID Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestReadModelOption(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strOptionFileName = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Read ModelOption.ini file."); });

                string strSKU = m_dic_UnitDevice[strPanel].SKU;
                string strModel = m_dic_UnitDevice[strPanel].Model;

                if (strModel == "UL")
                {
                    strModel = "EDA56";
                    strOptionFileName = Application.StartupPath + "\\" + strModel + "\\" + "UL_Option.ini";
                }
                else
                {
                    strOptionFileName = Application.StartupPath + "\\" + strModel + "\\" + strModel + "_Option.ini";
                }

                string filename = Path.GetFileName(strOptionFileName);
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Model_Option:" + filename); });
   
                #region Model_Option.ini
       
                if (ReadModelOptionFile(strPanel, strOptionFileName, ref strErrorMessage) == false)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Failed to read model option.ini."); });
                    return false;
                }

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Read ModelOption.ini file success." + "\r\n"); });

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetEID Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestCheckMESData(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                string EID = m_dic_UnitDevice[strPanel].EID;
                string StationName = m_st_OptionData.MES_Station;
                string WorkOrder = m_dic_UnitDevice[strPanel].WorkOrder;

                if (m_st_OptionData.MES_Enable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check MES Data."); });

                    if (MESCheckData(EID, StationName, WorkOrder, ref strErrorMessage) == false)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Failed to check MES data." + "\r\n"); });
                        return false;
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Check MES data successful." + "\r\n"); });
                    }
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to check MES data." + "\r\n"); });
                }

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestCheckMESData Exception:" + ex.Message;
                return false;
            }
        }

        private bool GetSKUProperty(string strPanel, ref string strSKU, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strSKU = "";

            try
            {
                bool bRes = false;
                bool bFlag = false;
                string strResult = "";
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "GetSKU.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN;
                for (int i = 0; i < 5; i++)
                {
                    strSKU = "";
                    strResult = "";           
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        strErrorMessage = "Execute bat to get SKU fail.";
                        bFlag = false;
                        Dly(10);          
                        continue;
                    }

                    // Truncate SKU      
                    int index = strResult.IndexOf("SKU:");
                    if (index != -1)
                    {
                        strResult = strResult.Substring(index + 4);
                        index = strResult.IndexOf("*");
                        strSKU = strResult.Substring(0, index);
                    }

                    if (string.IsNullOrWhiteSpace(strSKU) || strSKU.IndexOf("-") == -1)
                    {
                        strErrorMessage = "Get SKU format error !!!";
                        bFlag = false;   
                        Dly(10); 
                        continue;
                    }

                    bFlag = true;
                    break;
                }

                if (bFlag == false)
                {
                    strErrorMessage = "Get SKU property fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Get SKU exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool GetWorkOrderProperty(string strPanel, ref string strWorkOrder, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strWorkOrder = "";

            try
            {
                bool bRes = false;
                bool bFlag = false;
                string strResult = "";
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "GetWorkOrder.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN;
                for (int i = 0; i < 5; i++)
                {
                    strResult = "";
                    strWorkOrder = "";
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        strErrorMessage = "Execute bat to get WorkOrder fail.";
                        bFlag = false;
                        Dly(10);
                        continue;
                    }

                    // Truncate WorkOrder   
                    int index = strResult.IndexOf("WorkOrder:");
                    if (index != -1)
                    {
                        strResult = strResult.Substring(index + 10);
                        index = strResult.IndexOf("*");
                        strWorkOrder = strResult.Substring(0, index);
                    }

                    if (string.IsNullOrWhiteSpace(strWorkOrder))
                    {
                        strErrorMessage = "Get WorkOrder is empty !!!";
                        bFlag = false;       
                        Dly(10);
                        continue;
                    }

                    bFlag = true;
                    break;
                }

                if (bFlag == false)
                {
                    strErrorMessage = "Get WorkOrder property fail.";
                    return false;
                }         
            }
            catch (Exception ex)
            {
                strErrorMessage = "Get WorkOrder exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool GetEIDProperty(string strPanel, ref string strEID, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strEID = "";

            try
            {
                bool bRes = false;
                bool bFlag = false;
                string strResult = "";
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "GetEID.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN;
                for (int i = 0; i < 5; i++)
                {
                    strEID = "";
                    strResult = "";
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        strErrorMessage = "Execute bat to get EID fail.";
                        bFlag = false;
                        Dly(10);
                        continue;              
                    }

                    int index = strResult.IndexOf("EID:");
                    if (index != -1)
                    {
                        strResult = strResult.Substring(index + 4);
                        index = strResult.IndexOf("*");
                        strEID = strResult.Substring(0, index);
                    }

                    if (string.IsNullOrWhiteSpace(strEID))
                    {
                        strErrorMessage = "Get EID is empty !!!";
                        bFlag = false;      
                        Dly(10);
                        continue;                   
                    }

                    bFlag = true;
                    break;
                }

                if (bFlag == false)
                {
                    strErrorMessage = "Get EID property fail.";
                    return false;
                }      
            }
            catch (Exception ex)
            {
                strErrorMessage = "Get strEID exception:" + ex.Message;
                return false;
            }

            return true;
        }

        #endregion

        private bool TestCheckPreStationResult(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string MDCSPreStationResultCheck = m_dic_ModelOption[strPanel].MDCSPreStationResultCheck;
            string MDCSURL = m_dic_ModelOption[strPanel].MDCSURL;
            string MDCSPreStationDeviceName = m_dic_ModelOption[strPanel].MDCSPreStationDeviceName;
            string MDCSPreStationVarName = m_dic_ModelOption[strPanel].MDCSPreStationVarName;
            string MDCSPreStationVarValue = m_dic_ModelOption[strPanel].MDCSPreStationVarValue;

            try
            {
                if (MDCSPreStationResultCheck == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check Pre Station Result."); });

                    #region Check Multi-PreStation Result

                    string str_SN = m_dic_UnitDevice[strPanel].SN;
                    if (str_SN == "")
                    {
                        strErrorMessage = "Invalid SN." + str_SN;
                        return false;
                    }

                    string PreStationDeviceNames = MDCSPreStationDeviceName;
                    string[] MDCSDeviceNames = PreStationDeviceNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    if (MDCSDeviceNames.Length == 0)
                    {
                        strErrorMessage = "Pre station device name count is 0";
                        return false;
                    }

                    bool bRet = false;
                    for (int i = 0; i < 2; i++)
                    {
                        foreach (string PreStationName in MDCSDeviceNames)
                        {
                            bRet = false;
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Start Check Pre Station: " + PreStationName.Trim()); });

                            bRet = CheckSinglePreStationResult(strPanel, str_SN, PreStationName.Trim(), ref strErrorMessage);
                            if (bRet == true)
                            {
                                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Check Pre Station: " + PreStationName.Trim() + " Result Success"); });
                                m_PreStationDeviceName = PreStationName;
                                break;
                            }
                            else
                            {
                                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Check Pre Station: " + PreStationName.Trim() + " Result Fail"); });
                                m_PreStationDeviceName = "";
                                continue;
                            }
                        }

                        if (bRet == true)
                        {
                            break;
                        }

                        Dly(2);
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Loop Again to Check Pre Station. "); });
                    }

                    if (bRet == false)
                    {
                        return false;
                    }
                  
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check Pre Station Result successful." + "\r\n"); });

                    #endregion

                    #region Check Manual BIT Result (Obsolete)

                    //bRes = false;
                    //String cmd = "";
                    //String result = "";
                    //for (int i = 0; i < 3; i++)
                    //{
                    //    cmd = "adb -s " + str_SN + " kill-server";
                    //    result = clsUtil.ExecuteGetSysProp(cmd, 100, true);
                    //}
                    //for (int i = 0; i < 3; i++)
                    //{
                    //    cmd = "adb -s " + str_SN + " shell getprop persist.sys.BITAutoRlt";
                    //    result = "";
                    //    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get BIT manual Result."); });
                    //    result = clsUtil.ExecuteGetSysProp(cmd, 100, true);
                    //    if (result != null && result.Length > 0 && result.Contains("true"))
                    //    {
                    //        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Getprop persist.sys.BITAutoRlt true!"); });
                    //        bRes = true;
                    //        strErrorMessage = "";
                    //        break;
                    //    }
                    //    else
                    //    {
                    //        bRes = false;
                    //        strErrorMessage = "Check Manual BIT result failed!";
                    //        Dly(1);
                    //        continue;
                    //    }
                    //}
                    //if (bRes == false)
                    //{
                    //    strErrorMessage = "Check Manual BIT result failed!";
                    //    return false;
                    //}

                    #endregion
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to Check Pre Station."); });
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestCheckPreStationResult Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestQCNBackup(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";
            bool bRes = false;
            string strSN = m_dic_UnitDevice[strPanel].SN;
            string strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
            string strCOMPort = "";
            int iCOMPort = 0;
            string strQCNFilePath = m_dic_ModelOption[strPanel].QCNFilePath;
            string strQCNFileName = "";
            string strIMEI = "";
            string strQCNFileSize = "";
            UInt32 hHandle = 0;

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test QCN backup."); });

                #region IsWWAN

                DisplayMessage("Check SKU WWAN flag.");
                if (IsWWAN(strPanel) == false)
                {
                    bRes = true;
                    strErrorMessage = "";
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "SKU is WLAN,skiped."); });
                    return true;
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "SKU is WWAN."); });
                }

                #endregion

                #region Get COMPort

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get Diagnostics 9091 COM port."); });
                for (int i = 0; i < 5; i++)
                {
                    if (GetCOMPort(strPanel, ref strCOMPort, ref strErrorMessage) == false)
                    {
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "COM port:" + strCOMPort); });
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to get COM port.";
                    return false;
                }
                iCOMPort = int.Parse(strCOMPort);
                Dly(5);  // ???

                #endregion

                #region Check COMPort

                if (m_dic_COMPort[PANEL_1] == strCOMPort || m_dic_COMPort[PANEL_2] == strCOMPort || m_dic_COMPort[PANEL_3] == strCOMPort || m_dic_COMPort[PANEL_4] == strCOMPort)
                {
                    bRes = false;
                    strErrorMessage = "Exist same COM port." + strCOMPort;
                    return false;
                }
                m_dic_COMPort[strPanel] = strCOMPort;

                #endregion

                #region Backup QCN

                #region QLIBSetLibraryMode

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB SetLibraryMode."); });
                if (m_cls_QMSL.QLIBSetLibraryMode() == false)
                {
                    hHandle = 0;
                    bRes = false;
                    strErrorMessage = "Failed to QLIB SetLibraryMode.";
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB SetLibraryMode successfully."); });

                #endregion

                #region QLIBConnectServer

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB ConnectServer."); });
                for (int i = 0; i < 3; i++)
                {
                    if (m_cls_QMSL.QLIBConnectServer(iCOMPort, ref hHandle, ref strErrorMessage) == false)
                    {
                        Dly(5);
                        hHandle = 0;
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
                    bRes = false;
                    strErrorMessage = "Failed to QLIB ConnectServer.";
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB ConnectServer successfully."); });

                Dly(0.1);

                #endregion

                #region QLIBIsPhoneConnected

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB IsPhoneConnected."); });
                if (m_cls_QMSL.QLIBIsPhoneConnected(hHandle, ref strErrorMessage) == false)
                {
                    bRes = false;
                    strErrorMessage = "Failed to QLIB IsPhoneConnected.";
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB IsPhoneConnected successfully."); });

                Dly(0.1);

                #endregion

                #region QLIBDIAGNVREADF_IMEI

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB DIAGNVREADF IMEI."); });
                if (m_cls_QMSL.QLIBDIAGNVREADF_IMEI(hHandle, ref strIMEI, ref strErrorMessage) == false)
                {
                    bRes = false;
                    strErrorMessage = "Failed to QLIB DIAGNVREADF IMEI.";
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB DIAGNVREADF IMEI successfully."); });

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "IMEI:" + strIMEI); });
                string strDateTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                strQCNFileName = strDateTime + "-U_" + strIMEI + ".XQCN";
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QCN file name:" + strQCNFileName); });

                Dly(0.1);

                #endregion

                #region QLIBDownloadQcnFileV2
                // Note: if DUT disconnect when do that, this pannel will block.
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB DownloadQcnFileV2."); });
                if (m_cls_QMSL.QLIBDownloadQcnFileV2(hHandle, strQCNFilePath, strQCNFileName, ref strErrorMessage) == false)
                {
                    bRes = false;
                    strErrorMessage = "Failed to QLIB DownloadQcnFileV2.";
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB DownloadQcnFileV2 successfully."); });

                #endregion

                #endregion

                #region IMEI

                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];
                objSaveData.TestRecord.IMEI = strIMEI;
                m_dic_TestSaveData[strPanel] = objSaveData;

                #endregion

                #region Check QCN File Size

                if (GetFileSize(strQCNFilePath + "\\" + strQCNFileName, ref strQCNFileSize) == false)
                {
                    bRes = false;
                    strErrorMessage = "Failed to get QCN file size.";
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QCN file size:" + strQCNFileSize + " kb."); });
                if (int.Parse(strQCNFileSize) < int.Parse(m_dic_ModelOption[strPanel].QCNFileSize))
                {
                    bRes = false;
                    strErrorMessage = "Failed to check QCN file size." + strQCNFileSize;
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Check QCN file size " + strQCNFileSize + " > " + m_dic_ModelOption[strPanel].QCNFileSize); });

                #endregion

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test QCN backup successfully."); });
                bRes = true;

                return true;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestQCNBackup Exception:" + strr;
                return false;
            }
            finally
            {
                #region QLIBDisconnectServer

                if (hHandle > 0)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB DisconnectServer."); });
                    string strTempErrorMessage = "";
                    if (m_cls_QMSL.QLIBDisconnectServer(hHandle, ref strTempErrorMessage) == false)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB DisconnectServer failed." + strTempErrorMessage); });
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "QLIB DisconnectServer successfully." + "\r\n"); });
                    }
                }

                #endregion

                #region Delete QCN file

                if (hHandle > 0)
                {
                    if (bRes == false)
                    {
                        if (DeleteQCNFile(strQCNFilePath, strQCNFileName) == false)
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Delete QCN file failed."); });
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Delete QCN file successfully."); });
                        }
                    }
                }

                #endregion
            }
        }

        private bool DeleteQCNFile(string strFilePath, string strFileName)
        {
            try
            {
                string strQCNFilePath = "";
                bool bRes = false;
                string str_LocalPath = Application.StartupPath + "\\Data" + "\\" + "QCNFile_FAIL";

                strQCNFilePath = strFilePath;
                if (strQCNFilePath.Substring(strQCNFilePath.Length - 1, 1) == "\\")
                {
                    strQCNFilePath = strQCNFilePath.Substring(0, strQCNFilePath.Length - 1);
                }
                if (Directory.Exists(strQCNFilePath) == false)
                {
                    return false;
                }

                if (strFileName == "")
                {
                    // File not exist
                    return true;
                }

                for (int i = 0; i < 5; i++)
                {
                    if (File.Exists(strQCNFilePath + "\\" + strFileName) == true)
                    {
                        #region Copy

                        if (System.IO.Directory.Exists(str_LocalPath) == false)
                        {
                            System.IO.Directory.CreateDirectory(str_LocalPath);
                            Dly(1);
                        }
                        File.Copy(strQCNFilePath + "\\" + strFileName, str_LocalPath + "\\" + strFileName);

                        #endregion

                        #region Delete

                        File.Delete(strQCNFilePath + "\\" + strFileName);
                        Dly(0.5);
                        if (File.Exists(strQCNFilePath + "\\" + strFileName) == true)
                        {
                            bRes = false;
                            break;
                        }
                        else
                        {
                            bRes = true;
                            break;
                        }

                        #endregion
                    }
                    else
                    {
                        // Wait file exist
                        bRes = false;
                        Dly(1);
                        continue;
                    }
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

        private bool GetFileSize(string strFilePath, ref string strFileSize)
        {
            strFileSize = "";

            try
            {
                if (File.Exists(strFilePath) == false)
                {
                    return false;
                }

                FileInfo fi = new FileInfo(strFilePath);
                strFileSize = (fi.Length / 1024).ToString();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckSN(string strSN)
        {
            if (strSN.Length != 10)
            {
                return false;
            }

            if (strSN == "0000000000")
            {
                return false;
            }

            #region Obsolete

            //20129B5745
            //if (strSN.Substring(5, 1) != "B")
            //{
            //    return false;
            //}

            #endregion

            return true;
        }

        private bool GetCOMPort(string strPanel, ref string strCOMPort, ref string strErrorMessage)
        {
            try
            {
                strCOMPort = "";
                strErrorMessage = "";
                string strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;

                USBEnumerator usbEnum = new USBEnumerator();
                List<string> m_List_Port = new List<string>();
                bool bRes = false;
                for (int i = 0; i < 10; i++)
                {
                    m_List_Port.Clear();
                    bRes = usbEnum.GetPortByUSBDevicePhysicalAddress(strPhysicalAddress, ref m_List_Port, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(2);
                        continue;
                    }
                    if (m_List_Port.Count != 1)
                    {
                        bRes = false;
                        Dly(2);
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to COM port." + m_List_Port.Count.ToString();
                    return false;
                }
                strCOMPort = m_List_Port[0];

                #region Clear COM Port inuse status

                int iCOMPort = int.Parse(strCOMPort);
                if (iCOMPort > 99)
                {
                    if (DeleteCOMNameArbiterReg() == false)
                    {
                        strErrorMessage = "Failed to clear COM Port inuse status.";
                        return false;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "GetCOMPort Exception.";
                return false;
            }

            return true;
        }

        private bool KillQPST()
        {
            try
            {
                // QPST OLE Automation Server
                clsExecProcess objprocess = new clsExecProcess();
                string strProcess = "AtmnServer";
                if (objprocess.KillProcess(strProcess) == false)
                {
                    return false;
                }

                // QPST Server
                strProcess = "QPSTServer";
                if (objprocess.KillProcess(strProcess) == false)
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

        private bool FindQPST()
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

                // QPST Server
                strProcess = "QPSTServer";
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

        #region BackgroundWorker

        #region bgFlashWorker1

        private BackgroundWorker bgFlashWorker1 = new BackgroundWorker();

        private void bgFlashWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_1);
        }

        private void bgFlashWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_1, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_1, "BackgroundWorker Error.");
            }
            else
            {
                //DisplayUnitLog(PANEL_1, "BackgroundWorker Completed.");
            }
        }

        #endregion

        #region bgFlashWorker2

        private BackgroundWorker bgFlashWorker2 = new BackgroundWorker();

        private void bgFlashWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_2);
        }

        private void bgFlashWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_2, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_2, "BackgroundWorker Error.");
            }
            else
            {
                //DisplayUnitLog(PANEL_2, "BackgroundWorker Completed.");
            }
        }

        #endregion

        #region bgFlashWorker3

        private BackgroundWorker bgFlashWorker3 = new BackgroundWorker();

        private void bgFlashWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_3);
        }

        private void bgFlashWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_3, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_3, "BackgroundWorker Error.");
            }
            else
            {
                //DisplayUnitLog(PANEL_3, "BackgroundWorker Completed.");
            }
        }

        #endregion

        #region bgFlashWorker4

        private BackgroundWorker bgFlashWorker4 = new BackgroundWorker();

        private void bgFlashWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_4);
        }

        private void bgFlashWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_4, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_4, "BackgroundWorker Error.");
            }
            else
            {
                //DisplayUnitLog(PANEL_4, "BackgroundWorker Completed.");
            }
        }

        #endregion

        private void InitBackgroundworker()
        {
            // Initialize backgroundWorker1
            bgFlashWorker1.DoWork += new DoWorkEventHandler(bgFlashWorker1_DoWork);
            bgFlashWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker1_RunWorkerCompleted);
            bgFlashWorker1.WorkerReportsProgress = false;
            bgFlashWorker1.WorkerSupportsCancellation = true;

            // Initialize backgroundWorker2
            bgFlashWorker2.DoWork += new DoWorkEventHandler(bgFlashWorker2_DoWork);
            bgFlashWorker2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker2_RunWorkerCompleted);
            bgFlashWorker2.WorkerReportsProgress = false;
            bgFlashWorker2.WorkerSupportsCancellation = true;

            // Initialize backgroundWorker3
            bgFlashWorker3.DoWork += new DoWorkEventHandler(bgFlashWorker3_DoWork);
            bgFlashWorker3.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker3_RunWorkerCompleted);
            bgFlashWorker3.WorkerReportsProgress = false;
            bgFlashWorker3.WorkerSupportsCancellation = true;

            // Initialize backgroundWorker4
            bgFlashWorker4.DoWork += new DoWorkEventHandler(bgFlashWorker4_DoWork);
            bgFlashWorker4.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker4_RunWorkerCompleted);
            bgFlashWorker4.WorkerReportsProgress = false;
            bgFlashWorker4.WorkerSupportsCancellation = true;

            return;
        }

        private void bgFlashWorkerCancel()
        {
            try
            {
                if (bgFlashWorker1 != null)
                {
                    bgFlashWorker1.CancelAsync();
                }

                if (bgFlashWorker2 != null)
                {
                    bgFlashWorker2.CancelAsync();
                }

                if (bgFlashWorker3 != null)
                {
                    bgFlashWorker3.CancelAsync();
                }

                if (bgFlashWorker4 != null)
                {
                    bgFlashWorker4.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        #endregion

        #region PLC

        private bool PLCConnect()
        {
            try
            {
                m_obj_PLC = null;
                m_obj_PLC = new clsCPLCDave();

                string strPLCIP = m_st_OptionData.PLCIP;
                int iPLCPort = int.Parse(m_st_OptionData.PLCPort);
                string strErrorMessage = "";
                if (m_obj_PLC.Connect(strPLCIP, iPLCPort, ref strErrorMessage) == false)
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

        private bool PLCDisConnect()
        {
            try
            {
                if (m_obj_PLC != null)
                {
                    string strErrorMessage = "";
                    if (m_obj_PLC.DisConnect(ref strErrorMessage) == false)
                    {
                        return false;
                    }
                }
                m_obj_PLC = null;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private void PLCRelease()
        {
            m_b_PLCRuning = false;

            if (m_timer_WatchDog != null)
            {
                m_timer_WatchDog.Dispose();
                m_timer_WatchDog = null;
            }

            if (m_obj_PLC != null)
            {
                PLCDisConnect();
            }
        }

        private bool FeedbackResult(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_WriteDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_WriteDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_WriteDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_WriteDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];
                if (objSaveData.TestResult.TestPassed == true)
                {
                    #region FeedbackRunResult SUCCESS

                    bool bRes = false;
                    for (int i = 0; i < 20; i++)
                    {
                        bRes = m_obj_PLC.FeedbackRunResult(iDB, clsCPLCDave.FeedbackResult.SUCCESS, ref strErrorMessage);
                        if (bRes == false)
                        {
                            Dly(0.2);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (bRes == false)
                    {
                        return false;
                    }

                    #endregion
                }
                else
                {
                    #region FeedbackErrorCode

                    bool bRes = false;
                    string strErrorCode = "";
                    strErrorCode = "[QCN_" + m_st_OptionData.Area_Location + strPanel + "]:";
                    strErrorCode += "(" + objSaveData.TestRecord.SN + ")";
                    strErrorCode += objSaveData.TestResult.TestFailMessage;
                    for (int i = 0; i < 20; i++)
                    {
                        bRes = m_obj_PLC.FeedbackErrorCode(iDB, strErrorCode, ref strErrorMessage);
                        if (bRes == false)
                        {
                            Dly(0.2);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (bRes == false)
                    {
                        return false;
                    }

                    #endregion

                    #region FeedbackRunResult FAIL

                    bRes = false;
                    for (int i = 0; i < 20; i++)
                    {
                        bRes = m_obj_PLC.FeedbackRunResult(iDB, clsCPLCDave.FeedbackResult.FAIL, ref strErrorMessage);
                        if (bRes == false)
                        {
                            Dly(0.2);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (bRes == false)
                    {
                        return false;
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool ClearLastRunResult(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_WriteDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_WriteDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_WriteDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_WriteDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                #region Clear Last Run Result

                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = m_obj_PLC.FeedbackRunResult(iDB, clsCPLCDave.FeedbackResult.CLEAR, ref strErrorMessage);
                    if (bRes == false)
                    {
                        Dly(0.2);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (bRes == false)
                {
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool FeedbackStatus(string strPanel, clsCPLCDave.FeedbackStatus enumStatus, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_WriteDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_WriteDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_WriteDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_WriteDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                #region FeedbackCurrentStatus

                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = m_obj_PLC.FeedbackCurrentStatus(iDB, enumStatus, ref strErrorMessage);
                    if (bRes == false)
                    {
                        Dly(0.2);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (bRes == false)
                {
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool WaitForTestSingal(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_ReadDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_ReadDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_ReadDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_ReadDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                #region ReadCommandStartTest

                bool bRes = false;
                for (int i = 0; i < 1; i++)
                {
                    bRes = m_obj_PLC.ReadCommandStartTest(iDB, ref strErrorMessage);
                    if (bRes == false)
                    {
                        Dly(0.5);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (bRes == false)
                {
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckProductExist(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_ReadDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_ReadDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_ReadDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_ReadDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                #region ReadCommandStartTest

                bool bRes = false;
                for (int i = 0; i < 2; i++)
                {
                    bRes = m_obj_PLC.ReadProductExist(iDB, ref strErrorMessage);
                    if (bRes == true)
                    {
                        Dly(0.5);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                return bRes;

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }
        }

        private void Thread_Timer_WatchDog(object obj)
        {
            try
            {
                if (m_b_PLCRuning == true)
                {
                    string strErrorMessage = "";

                    m_i_WatchDog %= 255;

                    if (m_obj_PLC.WatchDog(m_st_OptionData.DB_Slot1_WriteDB, m_i_WatchDog.ToString(), ref strErrorMessage) == false)
                    {
                        DisplayMessage("WatchDog Fail:" + strErrorMessage);
                        SavePLCLogFile("WatchDog Fail:" + strErrorMessage);

                        if (FeedbackStatus(PANEL_1, clsCPLCDave.FeedbackStatus.ERROR, ref strErrorMessage) == false)
                        {
                            DisplayMessage("FeedbackStatus ERROR Fail:" + strErrorMessage);
                            SavePLCLogFile("FeedbackStatus ERROR Fail:" + strErrorMessage);
                        }
                    }

                    m_i_WatchDog++;
                }
            }
            catch (Exception exx)
            {
                string strr = exx.Message;
                return;
            }

            return;
        }

        #endregion

        #region MDCS

        private bool InitMDCSData(string strPanel)
        {
            try
            {
                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];

                objSaveData.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData.TestRecord.SN = "";
                objSaveData.TestRecord.Model = "";
                objSaveData.TestRecord.SKU = "";
                objSaveData.TestRecord.IMEI = "";
                objSaveData.TestRecord.TestTotalTime = 0;

                objSaveData.TestResult.TestPassed = true;
                objSaveData.TestResult.TestFailCode = 0;
                objSaveData.TestResult.TestFailMessage = "";
                objSaveData.TestResult.TestStatus = "";

                m_dic_TestSaveData[strPanel] = objSaveData;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool InitModelOptionData(string strPanel)
        {
            try
            {
                ModelOption objModelOption = m_dic_ModelOption[strPanel];

                objModelOption.QCNFilePath = "";
                objModelOption.QCNFileSize = "";
                objModelOption.MatrixWWANPos = 0;

                objModelOption.MDCSEnable = "";
                objModelOption.MDCSURL = "";
                objModelOption.MDCSDeviceName = "";
                objModelOption.MDCSPreStationResultCheck = "";
                objModelOption.MDCSPreStationDeviceName = "";
                objModelOption.MDCSPreStationVarName = "";
                objModelOption.MDCSPreStationVarValue = "";

                m_dic_ModelOption[strPanel] = objModelOption;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool SaveMDCS(string strPanel, TestSaveData objSaveData)
        {
            try
            {
                if (m_dic_ModelOption[strPanel].MDCSEnable == "1")
                {
                    string str_ErrorMessage = "";
                    clsMDCS obj_SaveMDCS = new clsMDCS();
                    obj_SaveMDCS.ServerName = m_dic_ModelOption[strPanel].MDCSURL;
                    obj_SaveMDCS.DeviceName = m_dic_ModelOption[strPanel].MDCSDeviceName;
                    obj_SaveMDCS.UseModeProduction = true;
                    obj_SaveMDCS.p_TestData = objSaveData;

                    bool bRes = false;
                    for (int i = 0; i < 5; i++)
                    {
                        bRes = obj_SaveMDCS.SendMDCSData(ref str_ErrorMessage);
                        if (bRes == false)
                        {
                            DeleteMDCSSqueueXmlFile();
                            Dly(1);
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
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool DeleteMDCSSqueueXmlFile()
        {
            try
            {
                string strXMLPath = System.IO.Path.GetTempPath() + "\\" + "mdcsqueue.xml";

                if (File.Exists(strXMLPath) == true)
                {
                    File.Delete(strXMLPath);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckSinglePreStationResult(string strPanel, string SN, string deviceName, ref string strErrorMessage)
        {
            clsMDCS obj_SaveMDCS = new clsMDCS();
            obj_SaveMDCS.ServerName = m_dic_ModelOption[strPanel].MDCSURL;
            obj_SaveMDCS.DeviceName = deviceName;
            obj_SaveMDCS.UseModeProduction = true;


            bool bRes = false;
            string strValue = "";
            for (int i = 0; i < 1; i++)
            {
                bRes = obj_SaveMDCS.GetMDCSVariable(deviceName, m_dic_ModelOption[strPanel].MDCSPreStationVarName, SN, ref strValue, ref strErrorMessage);
                if (bRes == false)
                {
                    bRes = false;
                    strErrorMessage = "GetMDCSVariable fail.";
                    //Dly(1);
                    continue;
                }
                else
                {
                    if (strValue != m_dic_ModelOption[strPanel].MDCSPreStationVarValue)
                    {
                        bRes = false;
                        strErrorMessage = "Pre station:" + deviceName + " Compare value fail. " + strValue;
                        //Dly(1);
                        continue;
                    }
                    else
                    {
                        bRes = true;
                        strErrorMessage = "";
                        break;
                    }
                }
            }

            return bRes;
        }

        #endregion

        #region MES

        public static bool MESCheckData(string strEID, string strStation, string strWorkOrder, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region Check MES Data

                if (strEID == "")
                {
                    strErrorMessage = "Invalid EID.";
                    return false;
                }
                if (strStation == "")
                {
                    strErrorMessage = "Invalid StationName.";
                    return false;
                }
                if (strWorkOrder == "")
                {
                    strErrorMessage = "Invalid WorkOrder.";
                    return false;
                }

                #endregion

                UploadData data = new UploadData()
                {
                    EID = strEID,
                    StationName = strStation,
                    WorkOrder = strWorkOrder
                };

                Result result = LineDashboard.CheckTestValid(data);
                if (result.code == 0)
                {
                    return true;
                }
                else
                {
                    strErrorMessage = "FailCode: " + result.code.ToString() + ",  Message: " + result.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "MESCheckData Exception:" + ex.Message;
                return false;
            }
        }

        public static bool MESUploadData(string strEID, string strStation, string strWorkOrder, string strSN, bool bPassFailFlag, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strResult = "";

            try
            {
                #region Check MES Data

                if (strEID == "")
                {
                    strErrorMessage = "Invalid EID.";
                    return false;
                }
                if (strStation == "")
                {
                    strErrorMessage = "Invalid StationName.";
                    return false;
                }
                if (strWorkOrder == "")
                {
                    strErrorMessage = "Invalid WorkOrder.";
                    return false;
                }
                if (strSN == "")
                {
                    strErrorMessage = "Invalid SN.";
                    return false;
                }

                #endregion

                if (bPassFailFlag == true)
                {
                    strResult = "PASS";
                }
                else
                {
                    strResult = "Failure";
                }

                UploadData data = new UploadData()
                {
                    EID = strEID,
                    StationName = strStation,
                    WorkOrder = strWorkOrder,
                    SN = strSN,
                    TestResult = strResult
                };

                Result result = LineDashboard.UploadTestValue(data);
                if (result.code == 0)
                {
                    return true;
                }
                else
                {
                    strErrorMessage = "Fail: code=" + result.code.ToString() + ", Message: " + result.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "MESUploadData Exception:" + ex.Message;
                return false;
            }
        }

        #endregion

        #region Reg

        private bool DeleteCOMNameArbiterReg()
        {
            try
            {
                string strReqName = Application.StartupPath + "\\" + "DeleteCOMNameArbiter.reg";
                if (File.Exists(strReqName) == false)
                {
                    return false;
                }
                System.Diagnostics.Process.Start("regedit.exe", "/s " + strReqName);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool HWSerNumEmulationReg()
        {
            try
            {
                string strReqName = Application.StartupPath + "\\" + "HWSerNumEmulation.reg";
                if (File.Exists(strReqName) == false)
                {
                    return false;
                }
                System.Diagnostics.Process.Start("regedit.exe", "/s " + strReqName);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        #endregion

        #region AutoTest

        private void AutoTest(string strPanel)
        {
            string strErrorMessage = "";

            try
            {
                if (m_dic_TestStatus[strPanel] == false)
                {
                    m_dic_TestStatus[strPanel] = true;

                    #region Check Pannel Exist Product or not (Obsolete)

                    //DisplayMessage("Panel:" + strPanel + " Check Exist Product:");
                    //if (CheckProductExist(strPanel, ref strErrorMessage) == true)
                    //{
                    //    DisplayMessage("Panel:" + strPanel + " Already Exist Product:" + strErrorMessage);
                    //    SavePLCLogFile("Panel:" + strPanel + " Already Exist Product:" + strErrorMessage);
                    //    m_dic_TestStatus[strPanel] = false;
                    //    return;
                    //}
                    //DisplayMessage("Panel:" + strPanel + " Not Exist Product.");

                    #endregion

                    #region Give Robot Ready Signal (Unnecessary)

                    DisplayMessage("Panel:" + strPanel + " Give Robot Ready Signal");
                    if (FeedbackStatus(strPanel, clsCPLCDave.FeedbackStatus.READY, ref strErrorMessage) == false)
                    {
                        DisplayMessage("Panel:" + strPanel + " FeedbackStatus READY Fail:" + strErrorMessage);
                        SavePLCLogFile("Panel:" + strPanel + " FeedbackStatus READY Fail:" + strErrorMessage);
                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }

                    #endregion

                    #region Clear Last Run Test Result (Obsolete)

                    //DisplayMessage("Panel:" + strPanel + " Clear Last Test Result");
                    //if (ClearLastRunResult(strPanel, ref strErrorMessage) == false)
                    //{
                    //    DisplayMessage("Panel:" + strPanel + " Clear Test Result Fail:" + strErrorMessage);
                    //    SavePLCLogFile("Panel:" + strPanel + " Clear Test Result Fail:" + strErrorMessage);
                    //}

                    #endregion

                    #region WaitForTestSingal

                    DisplayMessage("Panel:" + strPanel + " WaitForTestSingal");
                    if (WaitForTestSingal(strPanel, ref strErrorMessage) == false)
                    {
                        DisplayMessage("Panel:" + strPanel + " WaitForTestSingal Fail:" + strErrorMessage);
                        SavePLCLogFile("Panel:" + strPanel + " WaitForTestSingal Fail:" + strErrorMessage);
                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }
                    DisplayMessage("Panel:" + strPanel + " WaitForTestSingal Success.");

                    #endregion

                    #region Feedback BUSY

                    DisplayMessage("Panel:" + strPanel + " FeedbackStatus BUSY");
                    if (FeedbackStatus(strPanel, clsCPLCDave.FeedbackStatus.BUSY, ref strErrorMessage) == false)
                    {
                        DisplayMessage("Panel:" + strPanel + " FeedbackStatus BUSY Fail:" + strErrorMessage);
                        SavePLCLogFile("Panel:" + strPanel + " FeedbackStatus BUSY Fail:" + strErrorMessage);
                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }
                    DisplayMessage("Panel:" + strPanel + " FeedbackStatus BUSY Success.");

                    #endregion

                    #region Run Test

                    bool bRunRes = true;
                    InitMDCSData(strPanel);

                    #region Monitor Device

                    if (bRunRes == true)
                    {
                        // Monitor Device
                        bRunRes = MonitorDeviceByPhysicalAddress_AutoTest(strPanel, ref strErrorMessage);
                        if (bRunRes == false)
                        {
                            DisplayMessage("Panel:" + strPanel + " MonitorDevice Fail." + strErrorMessage);
                            this.Invoke((MethodInvoker)delegate { ClearUnitLog(strPanel); });
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Monitor Device fail, Can't Connect Device."); });
                            m_dic_TestStatus[strPanel] = false;
                            bRunRes = false;
                            //return;
                        }
                    }

                    #endregion

                    #region Update Status (Failed to detect device)

                    if (bRunRes == false)
                    {
                        #region STATUS_FAILED

                        this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_FAILED, Color.Red); });
                        UnitDeviceInfo stUnit2 = m_dic_UnitDevice[strPanel];
                        stUnit2.Status = "F";
                        m_dic_UnitDevice[strPanel] = stUnit2;

                        #endregion

                        #region MDCS Data

                        TestSaveData objSaveData = m_dic_TestSaveData[strPanel];
                        objSaveData.TestResult.TestPassed = false;
                        objSaveData.TestResult.TestFailCode = 2050;
                        objSaveData.TestResult.TestFailMessage = strErrorMessage;
                        objSaveData.TestResult.TestStatus = "";
                        m_dic_TestSaveData[strPanel] = objSaveData;

                        #endregion

                        // Feedback Test Result
                        if (FeedbackResult(strPanel, ref strErrorMessage) == false)
                        {
                            DisplayMessage("Panel:" + strPanel + " FeedbackResult Fail:" + strErrorMessage);
                            SavePLCLogFile("Panel:" + strPanel + " FeedbackResult Fail:" + strErrorMessage);
                        }

                        m_dic_COMPort[strPanel] = "";   //Clear ComPort Record When Disconnect.

                        #region Feedback Have Product (Obsolete)

                        //if (FeedbackStatus(strPanel, clsCPLCDave.FeedbackStatus.HAVEPRODUCT, ref strErrorMessage) == false)
                        //{
                        //    DisplayMessage("Panel:" + strPanel + " FeedbackStatus HAVEPRODUCT Fail:" + strErrorMessage);
                        //    SavePLCLogFile("Panel:" + strPanel + " FeedbackStatus HAVEPRODUCT Fail:" + strErrorMessage);
                        //}

                        #endregion

                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }

                    #endregion

                    #endregion
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                DisplayMessage("AutoTest Exception:" + ex.Message);
                return;
            }

            return;
        }

        #endregion

        #region Private

        private bool InitRun()
        {
            //string strOptionFileName = "";
            string strErrorMessage = "";

            rtbTestLog.Clear();
            InitBackgroundworker();

            #region Clear COM Port in use

            //DisplayMessage("Clear COM port inuse status.");
            //if (DeleteCOMNameArbiterReg() == false)
            //{
            //    DisplayMessage("Clear COM port inuse status fail.");
            //    return false;
            //}

            #endregion

            #region HWSerNumEmulationReg

            //DisplayMessage("HWSerNumEmulationReg.");
            //if (HWSerNumEmulationReg() == false)
            //{
            //    DisplayMessage("HWSerNumEmulationReg fail.");
            //    return false;
            //}
            //DisplayMessage("HWSerNumEmulationReg successfully.");

            #endregion

            #region Start adb


            #endregion
  
            #region Option.ini

            DisplayMessage("Read Option.ini file.");
            if (ReadOptionFile(ref strErrorMessage) == false)
            {
                DisplayMessage("Failed to read Option.ini file." + strErrorMessage);
                return false;
            }

            #endregion

            #region Model_Option.ini (Obsolete)

            //if (m_str_Model == "UL")
            //{
            //    m_str_Model = "EDA56";
            //    strOptionFileName = Application.StartupPath + "\\" + m_str_Model + "\\" + "UL_Option.ini";
            //}
            //else
            //{
            //    strOptionFileName = Application.StartupPath + "\\" + m_str_Model + "\\" + m_str_Model + "_Option.ini";
            //}
            //DisplayMessage("Model option ini:" + strOptionFileName);
            //strErrorMessage = "";
            //if (ReadModelOptionFile(strOptionFileName, ref strErrorMessage) == false)
            //{
            //    DisplayMessage("Failed to read model_option.ini file." + strErrorMessage);
            //    return false;
            //}

            #endregion

            #region Setup.ini

            DisplayMessage("Read Setup.ini file.");
            //DisplayMessage("Setup ini:" + Application.StartupPath + "\\" + "Setup.ini");
            if (ReadSetupFile(ref strErrorMessage) == false)
            {
                DisplayMessage("Failed to read setup.ini file." + strErrorMessage);
                return false;
            }

            #endregion

            #region Init data

            if (InitData() == false)
            {
                DisplayMessage("Failed to init data.");
                return false;
            }

            #endregion

            #region InitHW

            if (m_st_OptionData.TestMode == "1")
            {
                if (PLCConnect() == false)
                {
                    DisplayMessage("Failed to connect PLC..");
                    return false;
                }
                m_b_PLCRuning = true;
            }

            #endregion

            #region Timer

            if (m_st_OptionData.TestMode == "1")    // Auto Test
            {
                m_timer_WatchDog = new System.Threading.Timer(Thread_Timer_WatchDog, null, 1000, 3000);

                timerAutoTest.Interval = 5000;
                timerAutoTest.Enabled = true;
                timerAutoTest.Tick += new EventHandler(timerAutoTest_Tick);
            }
            else                                    // Manual Test
            {
                timerMonitor.Interval = 5000;
                timerMonitor.Enabled = true;
                timerMonitor.Tick += new EventHandler(timerMonitorRun_Tick);

                timerDeviceConnect.Interval = 30000;
                timerDeviceConnect.Enabled = true;
                timerDeviceConnect.Tick += new EventHandler(timerMonitorDeviceConnect_Tick);

                //timerKillProcess.Interval = 40000;
                //timerKillProcess.Enabled = true;
                //timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

                //timerClearReg.Interval = 600000;
                //timerClearReg.Enabled = true;
                //timerClearReg.Tick += new EventHandler(timerClearReg_Tick);
            }

            #endregion

            DisplayMessage("InitRun successfully.");

            return true;
        }

        #region Obsolete

        //private bool ScanMCF()
        //{
        //    frmMCF frmMCF = new frmMCF();
        //    DialogResult dlgResult = DialogResult.None;
        //    dlgResult = frmMCF.ShowDialog();
        //    if (dlgResult == DialogResult.No)
        //    {
        //        DisplayMessage("Scan sheet cancel.");
        //        return false;
        //    }
        //    if (dlgResult == DialogResult.OK)
        //    {
        //        #region Setup

        //        frmSetupUSB frm = new frmSetupUSB();
        //        frm.ShowDialog();

        //        #endregion

        //        return false;
        //    }

        //    m_st_MCFData.SKU = frmMCF.SKU;
        //    DisplayMessage("SKU:" + m_st_MCFData.SKU);

        //    string strModel = "";
        //    if (GetModelBySKU(ref strModel) == false)
        //    {
        //        return false;
        //    }
        //    m_str_Model = strModel;
        //    DisplayMessage("Model:" + m_str_Model);

        //    return true;
        //}

        //private bool ScanMES()
        //{
        //    frmMES frmMES = new frmMES();
        //    if (frmMES.ShowDialog() != DialogResult.Yes)
        //    {
        //        return false;
        //    }
        //    m_st_MESData.EID = frmMES.EID;
        //    m_st_MESData.WorkOrder = frmMES.WorkOrder;

        //    return true;
        //}

        #endregion

        private bool GetModelBySKU(string strPanel, ref string strModel)
        {
            try
            {
                strModel = "";
                string strSKU = m_dic_UnitDevice[strPanel].SKU;
                int iIndex = strSKU.IndexOf("-");
                strModel = strSKU.Substring(0, iIndex);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool IsWWAN(string strPanel)
        {
            try
            {
                string strWWAN = "";
                string strSKU = m_dic_UnitDevice[strPanel].SKU;
                int MatrixWWANPos = m_dic_ModelOption[strPanel].MatrixWWANPos;
                strWWAN = strSKU.Substring(MatrixWWANPos - 1, 1);
                if (strWWAN != "1")
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

        private bool ReadOptionFile(ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Option.ini";
                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                #region DAQ

                m_st_OptionData.DAQDevice = "";

                #endregion

                #region TestMode

                m_st_OptionData.TestMode = objIniFile.ReadString("TestMode", "Mode");
                if ((m_st_OptionData.TestMode != "0") && (m_st_OptionData.TestMode != "1"))
                {
                    strErrorMessage = "Invalid TestMode Mode:" + m_st_OptionData.TestMode;
                    return false;
                }

                #endregion

                #region Area

                m_st_OptionData.Area_Location = objIniFile.ReadString("Area", "Location");
                if (m_st_OptionData.Area_Location == "")
                {
                    strErrorMessage = "Invalid Area_Location Value:" + m_st_OptionData.Area_Location;
                    return false;
                }

                #endregion

                #region MES

                m_st_OptionData.MES_Enable = objIniFile.ReadString("MES", "Enable");
                if ((m_st_OptionData.MES_Enable != "0") && (m_st_OptionData.MES_Enable != "1"))
                {
                    strErrorMessage = "Invalid MES Enable:" + m_st_OptionData.MES_Enable;
                    return false;
                }

                m_st_OptionData.MES_Station = objIniFile.ReadString("MES", "Station");
                if (m_st_OptionData.MES_Station == "")
                {
                    strErrorMessage = "Invalid MES Station:" + m_st_OptionData.MES_Station;
                    return false;
                }

                #endregion

                #region PLC

                m_st_OptionData.PLCIP = objIniFile.ReadString("PLC", "PLCIP");
                m_st_OptionData.PLCPort = objIniFile.ReadString("PLC", "PLCPort");

                #endregion

                #region DB Slot

                m_st_OptionData.DB_Slot1_ReadDB = objIniFile.ReadInt("DB_Slot1", "ReadDB");
                m_st_OptionData.DB_Slot1_WriteDB = objIniFile.ReadInt("DB_Slot1", "WriteDB");
                m_st_OptionData.DB_Slot2_ReadDB = objIniFile.ReadInt("DB_Slot2", "ReadDB");
                m_st_OptionData.DB_Slot2_WriteDB = objIniFile.ReadInt("DB_Slot2", "WriteDB");
                m_st_OptionData.DB_Slot3_ReadDB = objIniFile.ReadInt("DB_Slot3", "ReadDB");
                m_st_OptionData.DB_Slot3_WriteDB = objIniFile.ReadInt("DB_Slot3", "WriteDB");
                m_st_OptionData.DB_Slot4_ReadDB = objIniFile.ReadInt("DB_Slot4", "ReadDB");
                m_st_OptionData.DB_Slot4_WriteDB = objIniFile.ReadInt("DB_Slot4", "WriteDB");

                #endregion
       
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool ReadModelOptionFile(string strPanel, string strOptionFileName, ref string strErrorMessage)
        {
            try
            {
                string str_FilePath = "";
                str_FilePath = strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                ModelOption objModelOption = m_dic_ModelOption[strPanel];

                #region MDCS

                objModelOption.MDCSEnable = objIniFile.ReadString("MDCS", "Enable");
                if ((objModelOption.MDCSEnable != "0") && (objModelOption.MDCSEnable != "1"))
                {
                    strErrorMessage = "Invalid MDCS Enable:" + objModelOption.MDCSEnable;
                    return false;
                }

                objModelOption.MDCSURL = objIniFile.ReadString("MDCS", "URL");
                if (objModelOption.MDCSURL == "")
                {
                    strErrorMessage = "Invalid MDCS URL:" + objModelOption.MDCSURL;
                    return false;
                }

                objModelOption.MDCSDeviceName = objIniFile.ReadString("MDCS", "DeviceName");
                if (objModelOption.MDCSDeviceName == "")
                {
                    strErrorMessage = "Invalid MDCS DeviceName:" + objModelOption.MDCSDeviceName;
                    return false;
                }

                objModelOption.MDCSPreStationResultCheck = objIniFile.ReadString("MDCS", "PreStationResultCheck");
                if ((objModelOption.MDCSPreStationResultCheck != "0") && (objModelOption.MDCSPreStationResultCheck != "1"))
                {
                    strErrorMessage = "Invalid MDCS PreStationResultCheck:" + objModelOption.MDCSPreStationResultCheck;
                    return false;
                }

                objModelOption.MDCSPreStationDeviceName = objIniFile.ReadString("MDCS", "PreStationDeviceName");
                if (objModelOption.MDCSPreStationDeviceName == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationDeviceName:" + objModelOption.MDCSPreStationDeviceName;
                    return false;
                }
                objModelOption.MDCSPreStationVarName = objIniFile.ReadString("MDCS", "PreStationVarName");
                if (objModelOption.MDCSPreStationVarName == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationVarName:" + objModelOption.MDCSPreStationVarName;
                    return false;
                }

                objModelOption.MDCSPreStationVarValue = objIniFile.ReadString("MDCS", "PreStationVarValue");
                if (objModelOption.MDCSPreStationVarValue == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationVarValue:" + objModelOption.MDCSPreStationVarValue;
                    return false;
                }

                #endregion

                #region QCN

                objModelOption.QCNFilePath = objIniFile.ReadString("QCN", "FilePath");
                if (Directory.Exists(objModelOption.QCNFilePath) == false)
                {
                    strErrorMessage = "Invalid QCN FilePath:" + objModelOption.QCNFilePath;
                    return false;
                }
                objModelOption.QCNFileSize = objIniFile.ReadString("QCN", "FileSize");
                if (int.Parse(objModelOption.QCNFileSize) < 1)
                {
                    strErrorMessage = "Invalid QCN FileSize:" + objModelOption.QCNFileSize;
                    return false;
                }

                #endregion

                #region Matrix

                objModelOption.MatrixWWANPos = objIniFile.ReadInt("Matrix", "WWANPos");
                if (objModelOption.MatrixWWANPos < 1)
                {
                    strErrorMessage = "Invalid Matrix WWANPos:" + objModelOption.MatrixWWANPos.ToString();
                    return false;
                }

                #endregion

                m_dic_ModelOption[strPanel] = objModelOption;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool ReadSetupFile(ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Setup.ini";

                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                // PortMapping
                m_st_OptionData.DeviceAddress_Panel1 = objIniFile.ReadString("PortMapping", "Panel_1");
                m_st_OptionData.DeviceAddress_Panel2 = objIniFile.ReadString("PortMapping", "Panel_2");
                m_st_OptionData.DeviceAddress_Panel3 = objIniFile.ReadString("PortMapping", "Panel_3");
                m_st_OptionData.DeviceAddress_Panel4 = objIniFile.ReadString("PortMapping", "Panel_4");
                if (m_st_OptionData.DeviceAddress_Panel1 == "" || m_st_OptionData.DeviceAddress_Panel2 == "" || m_st_OptionData.DeviceAddress_Panel3 == "" || m_st_OptionData.DeviceAddress_Panel4 == "")
                {
                    strErrorMessage = "Port Mapping not config." + strOptionFileName;
                    return false;
                }

                #region Check The Same Port

                if (m_st_OptionData.DeviceAddress_Panel1 == m_st_OptionData.DeviceAddress_Panel2)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel1 == m_st_OptionData.DeviceAddress_Panel3)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel1 == m_st_OptionData.DeviceAddress_Panel4)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel2 == m_st_OptionData.DeviceAddress_Panel3)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel2 == m_st_OptionData.DeviceAddress_Panel4)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel3 == m_st_OptionData.DeviceAddress_Panel4)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }

                #endregion

                m_st_OptionData.QDLoaderPortName = objIniFile.ReadString("QDLoader", "PortName");
                if (m_st_OptionData.QDLoaderPortName == "")
                {
                    strErrorMessage = "QDLoader PortName not config." + strOptionFileName;
                    return false;
                }

                m_st_OptionData.ADBDeviceName = objIniFile.ReadString("PortDevice", "DeviceName");
                if (m_st_OptionData.ADBDeviceName == "")
                {
                    strErrorMessage = "PortDevice PortName not config." + strOptionFileName;
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool InitData()
        {
            try
            {
                #region m_dic_UnitDevice

                m_dic_UnitDevice.Clear();

                // Unit1
                UnitDeviceInfo stUnit1 = new UnitDeviceInfo();
                stUnit1.Panel = PANEL_1;
                stUnit1.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel1;
                stUnit1.SN = "";
                stUnit1.SKU = "";
                stUnit1.Model = "";
                stUnit1.EID = "";
                stUnit1.WorkOrder = "";
                stUnit1.Status = "0";
                m_dic_UnitDevice.Add(PANEL_1, stUnit1);

                // Unit2
                UnitDeviceInfo stUnit2 = new UnitDeviceInfo();
                stUnit2.Panel = PANEL_2;
                stUnit2.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel2;
                stUnit2.SN = "";
                stUnit2.SKU = "";
                stUnit2.Model = "";
                stUnit2.EID = "";
                stUnit2.WorkOrder = "";
                stUnit2.Status = "0";
                m_dic_UnitDevice.Add(PANEL_2, stUnit2);


                // Unit3
                UnitDeviceInfo stUnit3 = new UnitDeviceInfo();
                stUnit3.Panel = PANEL_3;
                stUnit3.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel3;
                stUnit3.SN = "";
                stUnit3.SKU = "";
                stUnit3.Model = "";
                stUnit3.EID = "";
                stUnit3.WorkOrder = "";
                stUnit3.Status = "0";
                m_dic_UnitDevice.Add(PANEL_3, stUnit3);

                // Unit4
                UnitDeviceInfo stUnit4 = new UnitDeviceInfo();
                stUnit4.Panel = PANEL_4;
                stUnit4.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel4;
                stUnit4.SN = "";
                stUnit4.SKU = "";
                stUnit4.Model = "";
                stUnit4.EID = "";
                stUnit4.WorkOrder = "";
                stUnit4.Status = "0";
                m_dic_UnitDevice.Add(PANEL_4, stUnit4);

                #endregion

                #region m_dic_ModelOption

                m_dic_ModelOption.Clear();

                // Unit1
                ModelOption objModelOption1 = new ModelOption();
                objModelOption1.QCNFilePath = "";
                objModelOption1.QCNFileSize = "";
                objModelOption1.MatrixWWANPos = 0;
                objModelOption1.MDCSEnable = "";
                objModelOption1.MDCSURL = "";
                objModelOption1.MDCSDeviceName = "";
                objModelOption1.MDCSPreStationResultCheck = "";
                objModelOption1.MDCSPreStationDeviceName = "";
                objModelOption1.MDCSPreStationVarName = "";
                objModelOption1.MDCSPreStationVarValue = "";
                m_dic_ModelOption.Add(PANEL_1, objModelOption1);

                // Unit2
                ModelOption objModelOption2 = new ModelOption();
                objModelOption2.QCNFilePath = "";
                objModelOption2.QCNFileSize = "";
                objModelOption2.MatrixWWANPos = 0;
                objModelOption2.MDCSEnable = "";
                objModelOption2.MDCSURL = "";
                objModelOption2.MDCSDeviceName = "";
                objModelOption2.MDCSPreStationResultCheck = "";
                objModelOption2.MDCSPreStationDeviceName = "";
                objModelOption2.MDCSPreStationVarName = "";
                objModelOption2.MDCSPreStationVarValue = "";
                m_dic_ModelOption.Add(PANEL_2, objModelOption2);

                // Unit3
                ModelOption objModelOption3 = new ModelOption();
                objModelOption3.QCNFilePath = "";
                objModelOption3.QCNFileSize = "";
                objModelOption3.MatrixWWANPos = 0;
                objModelOption3.MDCSEnable = "";
                objModelOption3.MDCSURL = "";
                objModelOption3.MDCSDeviceName = "";
                objModelOption3.MDCSPreStationResultCheck = "";
                objModelOption3.MDCSPreStationDeviceName = "";
                objModelOption3.MDCSPreStationVarName = "";
                objModelOption3.MDCSPreStationVarValue = "";
                m_dic_ModelOption.Add(PANEL_3, objModelOption3);

                // Unit4
                ModelOption objModelOption4 = new ModelOption();
                objModelOption4.QCNFilePath = "";
                objModelOption4.QCNFileSize = "";
                objModelOption4.MatrixWWANPos = 0;
                objModelOption4.MDCSEnable = "";
                objModelOption4.MDCSURL = "";
                objModelOption4.MDCSDeviceName = "";
                objModelOption4.MDCSPreStationResultCheck = "";
                objModelOption4.MDCSPreStationDeviceName = "";
                objModelOption4.MDCSPreStationVarName = "";
                objModelOption4.MDCSPreStationVarValue = "";
                m_dic_ModelOption.Add(PANEL_4, objModelOption4);

                #endregion

                #region m_dic_TestSaveData

                m_dic_TestSaveData.Clear();

                // Unit1
                TestSaveData objSaveData1 = new TestSaveData();
                objSaveData1.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData1.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData1.TestRecord.SN = "";
                objSaveData1.TestRecord.Model = "";
                objSaveData1.TestRecord.SKU = "";
                objSaveData1.TestRecord.IMEI = "";
                objSaveData1.TestRecord.TestTotalTime = 0;
                objSaveData1.TestResult.TestPassed = false;
                objSaveData1.TestResult.TestFailCode = 0;
                objSaveData1.TestResult.TestFailMessage = "";
                objSaveData1.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_1, objSaveData1);

                // Unit2
                TestSaveData objSaveData2 = new TestSaveData();
                objSaveData2.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData2.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData2.TestRecord.SN = "";
                objSaveData2.TestRecord.Model = "";
                objSaveData2.TestRecord.SKU = "";
                objSaveData2.TestRecord.IMEI = "";
                objSaveData2.TestRecord.TestTotalTime = 0;
                objSaveData2.TestResult.TestPassed = false;
                objSaveData2.TestResult.TestFailCode = 0;
                objSaveData2.TestResult.TestFailMessage = "";
                objSaveData2.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_2, objSaveData2);

                // Unit3
                TestSaveData objSaveData3 = new TestSaveData();
                objSaveData3.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData3.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData3.TestRecord.SN = "";
                objSaveData3.TestRecord.Model = "";
                objSaveData3.TestRecord.SKU = "";
                objSaveData3.TestRecord.IMEI = "";
                objSaveData3.TestRecord.TestTotalTime = 0;
                objSaveData3.TestResult.TestPassed = false;
                objSaveData3.TestResult.TestFailCode = 0;
                objSaveData3.TestResult.TestFailMessage = "";
                objSaveData3.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_3, objSaveData3);

                // Unit4
                TestSaveData objSaveData4 = new TestSaveData();
                objSaveData4.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData4.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData4.TestRecord.SN = "";
                objSaveData4.TestRecord.Model = "";
                objSaveData4.TestRecord.SKU = "";
                objSaveData4.TestRecord.IMEI = "";
                objSaveData4.TestRecord.TestTotalTime = 0;
                objSaveData4.TestResult.TestPassed = false;
                objSaveData4.TestResult.TestFailCode = 0;
                objSaveData4.TestResult.TestFailMessage = "";
                objSaveData4.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_4, objSaveData4);

                #endregion

                #region m_dic_TestStatus

                m_dic_TestStatus.Clear();

                // Unit1
                m_dic_TestStatus.Add(PANEL_1, false);

                // Unit2
                m_dic_TestStatus.Add(PANEL_2, false);

                // Unit3
                m_dic_TestStatus.Add(PANEL_3, false);

                // Unit4
                m_dic_TestStatus.Add(PANEL_4, false);

                #endregion

                #region m_dic_TestHandle

                m_dic_TestHandle.Clear();

                // Unit1
                m_dic_TestHandle.Add(PANEL_1, 0);

                // Unit2
                m_dic_TestHandle.Add(PANEL_2, 0);

                // Unit3
                m_dic_TestHandle.Add(PANEL_3, 0);

                // Unit4
                m_dic_TestHandle.Add(PANEL_4, 0);

                #endregion

                #region m_dic_COMPort

                m_dic_COMPort.Clear();

                // Unit1
                m_dic_COMPort.Add(PANEL_1, "");

                // Unit2
                m_dic_COMPort.Add(PANEL_2, "");

                // Unit3
                m_dic_COMPort.Add(PANEL_3, "");

                // Unit4
                m_dic_COMPort.Add(PANEL_4, "");

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private void Dly(double d_WaitTimeSecond)
        {
            try
            {
                long lWaitTime = 0;
                long lStartTime = 0;

                if (d_WaitTimeSecond <= 0)
                {
                    return;
                }

                lWaitTime = Convert.ToInt64(d_WaitTimeSecond * TimeSpan.TicksPerSecond);
                lStartTime = System.DateTime.Now.Ticks;
                while ((System.DateTime.Now.Ticks - lStartTime) < lWaitTime)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private bool ExcuteBat(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, ref string strErrorMessage)
        {
            strErrorMessage = "";

            Process process = null;
            ProcessStartInfo startInfo = null;

            if (strBatParameter != "")
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile, strBatParameter);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.Arguments = strBatParameter;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }
            else
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }

            try
            {
                if (process.Start())
                {
                    if (iTimeOut == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(iTimeOut);
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Bat Exception:" + strr;
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return true;
        }

        private bool ExcuteBat(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, string strSearchResult, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strOutput = "";

            Process process = null;
            ProcessStartInfo startInfo = null;

            if (strBatParameter != "")
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile, strBatParameter);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.Arguments = strBatParameter;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }
            else
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }

            try
            {
                if (process.Start())
                {
                    #region StandardOutput OutputDataReceived

                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        string strData = e.Data;
                        if (!String.IsNullOrEmpty(strData))
                        {
                            strOutput += strData;
                            this.Invoke((MethodInvoker)delegate
                            {
                                DisplayUnitLog(strPanel, strData);
                            });
                        }
                    });

                    process.BeginOutputReadLine();

                    if (iTimeOut == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(iTimeOut);
                    }

                    #endregion
                }

                #region Check Result

                // 输出有时会延迟
                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = strOutput.Contains(strSearchResult);
                    if (bRes == true)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(500);
                        bRes = false;
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "Check bat result:" + strSearchResult;
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Bat Exception:" + strr;
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return true;
        }

        private bool ExcuteBat(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, string strSearchResult, ref string strResult, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strResult = "";
            string strOutput = "";

            Process process = null;
            ProcessStartInfo startInfo = null;

            if (strBatParameter != "")
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile, strBatParameter);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.Arguments = strBatParameter;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }
            else
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }

            try
            {
                if (process.Start())
                {
                    #region StandardOutput OutputDataReceived

                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        string strData = e.Data;
                        if (!String.IsNullOrEmpty(strData))
                        {
                            strOutput += strData;
                            this.Invoke((MethodInvoker)delegate
                            {
                                DisplayUnitLog(strPanel, strData);
                            });
                        }
                    });

                    process.BeginOutputReadLine();

                    if (iTimeOut == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(iTimeOut);
                    }

                    #endregion
                }

                #region Check Result

                // 输出有时会延迟
                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = strOutput.Contains(strSearchResult);
                    if (bRes == true)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(500);
                        bRes = false;
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "Check bat result:" + strSearchResult;
                    return false;
                }

                strResult = strOutput;

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Bat Exception:" + strr;
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return true;
        }

        private void ConfirmDlg(string str_Content)
        {
            frmConfirmOK frm = new frmConfirmOK();
            frm.Content = str_Content;
            frm.ShowDialog();
        }

        private bool ConfirmYesNoDlg(string str_Content)
        {
            frmConfirmYESNO frm = new frmConfirmYESNO();
            frm.Content = str_Content;
            if (frm.ShowDialog() != DialogResult.Yes)
            {
                return false;
            }

            return true;
        }

        private void DisplayMessage(string str_Message)
        {
            try
            {
                if (rtbTestLog.Text.Length > 1000000)
                {
                    rtbTestLog.Clear();
                }

                str_Message = "[" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]:" + str_Message;

                rtbTestLog.AppendText(str_Message + Convert.ToChar(13) + Convert.ToChar(10));
                rtbTestLog.ScrollToCaret();
                rtbTestLog.Refresh();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void DisplayUnit(string str_Index, string str_Text, Color c_BackColor)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        lblUnit1.Text = str_Text;
                        lblUnit1.BackColor = c_BackColor;
                        break;
                    case 2:
                        lblUnit2.Text = str_Text;
                        lblUnit2.BackColor = c_BackColor;
                        break;
                    case 3:
                        lblUnit3.Text = str_Text;
                        lblUnit3.BackColor = c_BackColor;
                        break;
                    case 4:
                        lblUnit4.Text = str_Text;
                        lblUnit4.BackColor = c_BackColor;
                        break;
                    default:
                        break;
                }
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void DisplayUnitStatus(string str_Index, string str_Text, Color c_BackColor)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        lblUnit1Status.Text = str_Text;
                        lblUnit1Status.BackColor = c_BackColor;
                        break;
                    case 2:
                        lblUnit2Status.Text = str_Text;
                        lblUnit2Status.BackColor = c_BackColor;
                        break;
                    case 3:
                        lblUnit3Status.Text = str_Text;
                        lblUnit3Status.BackColor = c_BackColor;
                        break;
                    case 4:
                        lblUnit4Status.Text = str_Text;
                        lblUnit4Status.BackColor = c_BackColor;
                        break;
                    default:
                        break;
                }
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void DisplayUnitLog(string str_Index, string str_Text)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        rtbUnit1Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit1Log.ScrollToCaret();
                        rtbUnit1Log.Refresh();
                        break;
                    case 2:
                        rtbUnit2Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit2Log.ScrollToCaret();
                        rtbUnit2Log.Refresh();
                        break;
                    case 3:
                        rtbUnit3Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit3Log.ScrollToCaret();
                        rtbUnit3Log.Refresh();
                        break;
                    case 4:
                        rtbUnit4Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit4Log.ScrollToCaret();
                        rtbUnit4Log.Refresh();
                        break;
                    default:
                        break;
                }
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void ClearUnitLog(string str_Index)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        rtbUnit1Log.Clear();
                        break;
                    case 2:
                        rtbUnit2Log.Clear();
                        break;
                    case 3:
                        rtbUnit3Log.Clear();
                        break;
                    case 4:
                        rtbUnit4Log.Clear();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private bool SaveUnitTestReport(string str_Index)
        {
            string str_Path = "";
            string str_FileName = "";
            string str_SN = "";

            str_Path = Application.StartupPath + "\\Data";
            str_FileName = "TestReport.txt";
            str_SN = m_dic_UnitDevice[str_Index].SN;

            if (str_SN == "")
            {
                str_FileName = "";
                str_FileName = "TestReport.txt";
            }
            else
            {
                str_FileName = "";
                string str_DateTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                //str_FileName = str_SN + "_TestReport.txt";
                str_FileName = str_SN + "_" + str_DateTime + "_TestReport.txt";
            }

            if (System.IO.Directory.Exists(str_Path) == false)
            {
                System.IO.Directory.CreateDirectory(str_Path);
            }
            if (System.IO.File.Exists(str_Path + "\\" + str_FileName))
            {
                try
                {
                    System.IO.File.Delete(str_Path + "\\" + str_FileName);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                    return false;
                }
            }

            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        rtbUnit1Log.Refresh();
                        rtbUnit1Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    case 2:
                        rtbUnit2Log.Refresh();
                        rtbUnit2Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    case 3:
                        rtbUnit3Log.Refresh();
                        rtbUnit3Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    case 4:
                        rtbUnit4Log.Refresh();
                        rtbUnit4Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                return false;
            }

            return true;
        }

        private bool SaveUnitTestFile(string str_Index, string str_Content)
        {
            string str_Path = "";
            string str_FileName = "";
            string str_SN = "";

            str_Path = Application.StartupPath + "\\Data";
            str_FileName = "TestReport.txt";
            str_SN = m_dic_UnitDevice[str_Index].SN;

            if (str_SN == "")
            {
                str_FileName = "";
                str_FileName = "TestReport.txt";
            }
            else
            {
                str_FileName = "";
                str_FileName = str_SN + "_TestReport.txt";
            }

            if (System.IO.Directory.Exists(str_Path) == false)
            {
                System.IO.Directory.CreateDirectory(str_Path);
            }
            if (System.IO.File.Exists(str_Path + "\\" + str_FileName))
            {
                try
                {
                    System.IO.File.Delete(str_Path + "\\" + str_FileName);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                    return false;
                }
            }

            FileStream fs = null;
            try
            {
                fs = new FileStream(str_Path + "\\" + str_FileName, FileMode.Create);

                //获得字节数组
                byte[] data = System.Text.Encoding.Default.GetBytes(str_Content);

                //开始写入
                fs.Write(data, 0, data.Length);

                //清空缓冲区、关闭流
                fs.Flush();

                fs.Close();
                fs = null;
            }
            catch (Exception ex)
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }

                string str = ex.Message;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }

            return true;
        }

        private bool SavePLCLogFile(string str_Content)
        {
            lock (m_obj_SaveLogLocker)
            {
                bool b_Res = false;
                for (int i = 0; i < 2; i++)
                {
                    b_Res = WriteLogFile(str_Content);
                    if (b_Res == true)
                    {
                        break;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(100);
                        continue;
                    }
                }

                if (b_Res == false)
                {
                    return false;
                }

                return true;
            }
        }

        private bool WriteLogFile(string str_Content)
        {
            string str_Path = "";
            string str_FileName = "";
            string str_PathFileName = "";
            str_Path = Application.StartupPath + "\\LOG\\PLCLOG";
            str_FileName = "Test.log";
            str_PathFileName = str_Path + "\\" + str_FileName;

            try
            {
                if (System.IO.Directory.Exists(str_Path) == false)
                {
                    System.IO.Directory.CreateDirectory(str_Path);
                    System.Threading.Thread.Sleep(500);
                    if (System.IO.Directory.Exists(str_Path) == false)
                    {
                        return false;
                    }
                }

                if (File.Exists(str_PathFileName) == false)
                {
                    StreamWriter sr = File.CreateText(str_PathFileName);
                    sr.Close();
                    System.Threading.Thread.Sleep(500);
                    if (File.Exists(str_PathFileName) == false)
                    {
                        return false;
                    }
                }

                // 大于2M
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(str_PathFileName);
                if (fileInfo.Length / (1024 * 1024) > 2)
                {
                    string str_NewFileName = str_Path + "\\" + "Test_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";
                    fileInfo.MoveTo(str_NewFileName);

                    StreamWriter sr = File.CreateText(str_PathFileName);
                    sr.WriteLine("[" + DateTime.Now.ToString() + "] " + str_Content);
                    sr.Close();
                }
                else
                {
                    StreamWriter sr = File.AppendText(str_PathFileName);
                    sr.WriteLine("[" + DateTime.Now.ToString() + "] " + str_Content);
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private void CollapseMenu(bool enable)
        {
            if (enable == true) //Collapse Menu
            {
                panelMenu.Width = 80;
                picBoxLogo.Visible = false;
                panelInfo.Visible = false;
                btnHome.Visible = true;
                btnHome.Dock = DockStyle.Top;

                //foreach (Button menuButton in panelMenu.Controls.OfType<Button>())
                //{
                //    menuButton.Text = "";
                //    menuButton.ImageAlign = ContentAlignment.MiddleCenter;
                //    menuButton.Padding = new Padding(0);
                //}
            }
            else  //Expand Menu
            {
                panelMenu.Width = 200;
                btnHome.Visible = false;
                //btnHome.Dock = DockStyle.None;
                picBoxLogo.Visible = true;
                picBoxLogo.Image = Resources.HoneywellLog_150;
                picBoxLogo.Dock = DockStyle.Fill;
                panelInfo.Visible = true;

                //foreach (Button menuButton in panelMenu.Controls.OfType<Button>())
                //{
                //    menuButton.Text = "       " + menuButton.Tag.ToString();    // Tag
                //    menuButton.ImageAlign = ContentAlignment.MiddleLeft;
                //    menuButton.Padding = new Padding(10, 0, 10, 0);
                //}
            }
        }

        #endregion       

        //#endregion

    }
}
