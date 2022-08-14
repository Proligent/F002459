using System;
using System.Collections.Generic;
using System.Windows.Forms;
using F002459.Common;

namespace F002459.Forms
{
    public partial class frmSetupUSB : Form
    {
        #region Variable

        private string m_str_DeviceName = "";

        #endregion

        #region Load

        public frmSetupUSB()
        {
            InitializeComponent();
        }

        private void frmSetupUSB_Load(object sender, EventArgs e)
        {
            string strNote = "";
            strNote = "Step 1:Do not connect the mobility to the USB port." + "\r\n";
            strNote += "Step 2:Select the panel and connect the USB port." + "\r\n";
            strNote += "Step 3:Restart the test tool after configuration.";
            lblNote.Text = strNote;

            comboBoxPanel.SelectedItem = "1";

            string strErrorMessage = "";
            if (ReadSetupFile(ref strErrorMessage) == false)
            {
                MessageBox.Show("Failed to read setup ini file." + strErrorMessage);
                return;
            }
        }

        #endregion

        private void btnOK_Click(object sender, EventArgs e)
        {
            btnOK.Enabled = false;

            SetupUSB();

            btnOK.Enabled = true;

            return;
        }

        private void SetupUSB()
        {
            bool bRes = false;
            string strPanel = comboBoxPanel.Text.Trim();

            if (strPanel == "")
            {
                DisplayMessage("Please select panel.");
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    bRes = SetupUSBDevicePhysicalAddress();
                    if (bRes == true)
                    {
                        break;
                    }
                    if (bRes == false)
                    {
                        Dly(1);
                        continue;
                    }
                }
                if (bRes == false)
                {
                    DisplayMessage("Setup failed.");
                }
                else
                {
                    DisplayMessage("Setup successfully.");
                }
            }

            return;
        }

        #region Function

        private bool SetupUSBDevicePhysicalAddress()
        {
            string strErrorMessage = "";

            if (m_str_DeviceName == "")
            {
                DisplayMessage("Faild to get device name from ini file.");
                return false;
            }

            USBEnumerator usbEnum = new USBEnumerator();
            string strDeviceName = m_str_DeviceName;
            List<string> m_List_PhysicalAddress = new List<string>();
            if (usbEnum.GetUSBDevicePhysicalAddressByDeviceName(strDeviceName, ref m_List_PhysicalAddress, ref strErrorMessage) == false)
            {
                DisplayMessage(strErrorMessage);
                return false;
            }
            if (m_List_PhysicalAddress.Count != 1)
            {
                strErrorMessage = "Failed to enume USB device." + m_List_PhysicalAddress.Count.ToString();
                DisplayMessage(strErrorMessage);
                return false;
            }

            string strPanel = "Panel_" + comboBoxPanel.Text.Trim();
            if (WriteOptionFile(strPanel, m_List_PhysicalAddress[0].ToString(), ref strErrorMessage) == false)
            {
                DisplayMessage(strErrorMessage);
                return false;
            }

            DisplayMessage("Panel:" + strPanel);
            DisplayMessage("USBDevicePhysicalAddress:" + m_List_PhysicalAddress[0].ToString());

            return true;
        }

        #endregion

        #region Private

        private bool ReadSetupFile(ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Setup.ini";

                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (System.IO.File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                m_str_DeviceName = objIniFile.ReadString("PortDevice", "DeviceName");
                if (m_str_DeviceName == "")
                {
                    strErrorMessage = "Read ini key fail.";
                    return false;
                }

            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool WriteOptionFile(string strKey, string strValue, ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Setup.ini";

                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (System.IO.File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                objIniFile.WriteIniFile("PortMapping", strKey, strValue);

                string strReadValue = objIniFile.ReadString("PortMapping", strKey);
                if (strReadValue != strValue)
                {
                    strErrorMessage = "Write ini key value fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        private void DisplayMessage(string str_Message)
        {
            try
            {
                rtbInfoList.AppendText(str_Message + Convert.ToChar(13) + Convert.ToChar(10));
                rtbInfoList.ScrollToCaret();
                rtbInfoList.Refresh();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                rtbInfoList.Clear();
                string strr = ex.Message;
                return;
            }

            return;
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

        #endregion

    }
}
