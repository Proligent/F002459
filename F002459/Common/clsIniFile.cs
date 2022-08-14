using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace F002459
{
    public class clsIniFile
    {
        #region Variable

        public string _FileName;

        #endregion

        #region ImportDLL

        [DllImport("kernel32.dll")]
        private extern static int GetPrivateProfileStringA(string segName, string keyName, string sDefault, StringBuilder buffer, int nSize, string fileName);
        [DllImport("kernel32.dll")]
        private extern static int GetPrivateProfileSectionA(string segName, StringBuilder buffer, int nSize, string fileName);
        [DllImport("kernel32.dll")]
        private extern static int WritePrivateProfileSectionA(string segName, string sValue, string fileName);
        [DllImport("kernel32.dll")]
        private extern static int WritePrivateProfileStringA(string segName, string keyName, string sValue, string fileName);
        [DllImport("kernel32.dll")]
        private extern static int GetPrivateProfileSectionNamesA(byte[] buffer, int iLen, string fileName);

        #endregion

        #region Construct

        public clsIniFile()
        {

        }

        public clsIniFile(string strFileName)
        {
            _FileName = strFileName;
        }

        #endregion

        #region Read

        #region ReadValue
        /// <summary>
        /// 读取字段值
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public string ReadValue(string Section, string Key, string FileName)
        {
            StringBuilder buffer = new StringBuilder(65535);
            GetPrivateProfileStringA(Section, Key, "", buffer, buffer.Capacity, FileName);
            return buffer.ToString();
        }
        #endregion

        #region ReadString
        /// <summary>
        /// 返回字符串
        /// </summary>
        public string ReadString(string Section, string Key)
        {
            StringBuilder buffer = new StringBuilder(65535);
            GetPrivateProfileStringA(Section, Key, "", buffer, buffer.Capacity, _FileName);
            return buffer.ToString();
        }
        #endregion

        #region ReadInt
        /// <summary>
        /// 返回int型的数
        /// </summary>
        public virtual int ReadInt(string Section, string Key)
        {
            int result = -1;
            try
            {
                result = int.Parse(this.ReadString(Section, Key));
            }
            catch
            {
                result = -1;
            }
            return result;
        }
        #endregion

        #region ReadLong
        /// <summary>
        /// 返回long型的数
        /// </summary>
        public virtual long ReadLong(string Section, string Key)
        {
            long result = -1;
            try
            {
                result = long.Parse(this.ReadString(Section, Key));
            }
            catch
            {
                result = -1;
            }
            return result;
        }
        #endregion

        #region ReadByte
        /// <summary>
        /// 返回byte型的数
        /// </summary>
        public virtual byte ReadByte(string Section, string Key)
        {
            byte result = 0;
            try
            {
                result = byte.Parse(this.ReadString(Section, Key));
            }
            catch
            {
                result = 0;
            }
            return result;
        }
        #endregion

        #region ReadFload
        /// <summary>
        /// 返回float型的数
        /// </summary>
        public virtual float ReadFloat(string Section, string Key)
        {
            float result = -1;
            try
            {
                result = float.Parse(this.ReadString(Section, Key));
            }
            catch
            {
                result = -1;
            }
            return result;
        }
        #endregion

        #region ReadDouble
        /// <summary>
        /// 返回double型的数
        /// </summary>
        public virtual double ReadDouble(string Section, string Key)
        {
            double result = -1;
            try
            {
                result = double.Parse(this.ReadString(Section, Key));
            }
            catch
            {
                result = -1;
            }
            return result;
        }
        #endregion

        #region ReadDateTime
        /// <summary>
        /// 返回日期型的数
        /// </summary>
        public virtual DateTime ReadDateTime(string Section, string Key)
        {
            DateTime result;
            try
            {
                result = DateTime.Parse(this.ReadString(Section, Key));
            }
            catch
            {
                result = DateTime.Parse("0-0-0"); ;
            }
            return result;
        }
        #endregion

        #region ReadBool
        /// <summary>
        /// Get Bool
        /// </summary>
        public virtual bool ReadBool(string Section, string Key)
        {
            bool result = false;
            try
            {
                result = bool.Parse(this.ReadString(Section, Key));
            }
            catch
            {
                result = bool.Parse("0-0-0");
            }
            return result;
        }
        #endregion

        #endregion

        #region Write

        #region WriteValue
        /// <summary>
        /// 保存Key的值
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="FileName"></param>
        public void WriteValue(string Section, string Key, object Value, string FileName)
        {
            if (Value != null)
            {
                WritePrivateProfileStringA(Section, Key, Value.ToString(), FileName);
            }
            else
            {
                WritePrivateProfileStringA(Section, Key, null, FileName);
            }
        }

        #endregion

        #region Write
        /// <summary>
        /// 用于写任何类型的键值到ini文件中
        /// </summary>
        /// <param name="Section">该键所在的节名称</param>
        /// <param name="Key">该键的名称</param>
        /// <param name="Value">该键的值</param>
        public void Write(string Section, string Key, object Value)
        {
            if (Value != null)
            {
                WritePrivateProfileStringA(Section, Key, Value.ToString(), _FileName);
            }
            else
            {
                WritePrivateProfileStringA(Section, Key, null, _FileName);
            }
        }

        #endregion

        #region WriteIniFile
        /// <summary>
        /// 写段，键，值到文件
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public void WriteIniFile(string Section, string Key, string Value)
        {
            if (FileExists() == false)
            {
                CreateFile();
            }

            if (SectionExists(Section) == false)
            {
                AddSection(Section);
                AddKey(Section, Key);
                Write(Section, Key, Value);
            }
            else
            {
                if (ValueExits(Section, Key) == false)
                {
                    AddKey(Section, Key);
                    Write(Section, Key, Value);
                }
                else
                {
                    Write(Section, Key, Value);
                }
            }
        }
        #endregion

        #endregion

        #region Section

        /// <summary>
        /// Read Sections
        /// </summary>
        public ArrayList ReadSections()
        {
            byte[] buffer = new byte[65535];
            int rel = GetPrivateProfileSectionNamesA(buffer, buffer.GetUpperBound(0), _FileName);
            int iCnt, iPos;
            ArrayList arrayList = new ArrayList();
            string tmp;
            if (rel > 0)
            {
                iCnt = 0; iPos = 0;
                for (iCnt = 0; iCnt < rel; iCnt++)
                {
                    if (buffer[iCnt] == 0x00)
                    {
                        tmp = System.Text.ASCIIEncoding.Default.GetString(buffer, iPos, iCnt).Trim();
                        iPos = iCnt + 1;
                        if (tmp != "")
                            arrayList.Add(tmp);
                    }
                }
            }
            return arrayList;
        }

        /// <summary>
        /// Check Section Whether Exists
        /// </summary>
        public bool SectionExists(string Section)
        {
            //done SectionExists
            StringBuilder buffer = new StringBuilder(65535);
            GetPrivateProfileSectionA(Section, buffer, buffer.Capacity, _FileName);
            if (buffer.ToString().Trim() == "")
                return false;
            else
                return true;
        }

        /// <summary>
        /// Check Value
        /// </summary>
        public bool ValueExits(string Section, string Key)
        {
            if (ReadString(Section, Key).Trim() == "")
                return false;
            else
                return true;
        }

        /// <summary>
        /// Delete Key
        /// </summary>
        /// <param name="Section">该键所在的节的名称</param>
        /// <param name="Key">该键的名称</param>
        public void DeleteKey(string Section, string Key)
        {
            Write(Section, Key, null);
        }

        /// <summary>
        /// Add Key
        /// </summary>
        /// <param name="Section">该键所在的节的名称</param>
        /// <param name="Key">该键的名称</param>
        public void AddKey(string Section, string Key)
        {
            Write(Section, Key, "");
        }

        /// <summary>
        /// 删除指定的节的所有内容
        /// </summary>
        /// <param name="Section">要删除的节的名字</param>
        public void DeleteSection(string Section)
        {
            WritePrivateProfileSectionA(Section, null, _FileName);
        }

        /// <summary>
        /// 添加一个节
        /// </summary>
        /// <param name="Section">要添加的节名称</param>
        public void AddSection(string Section)
        {
            WritePrivateProfileSectionA(Section, "", _FileName);
        }

        #endregion

        #region IniFile

        /// <summary>
        /// 删除ini文件
        /// </summary>
        private void DeleteFile()
        {
            if (FileExists())
            {
                try
                {
                    File.Delete(_FileName);
                }
                catch (Exception e)
                {
                    string strException = e.Message;
                    return;
                }
            }
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        private void CreateFile()
        {
            try
            {
                File.Create(_FileName).Close();
            }
            catch (Exception e)
            {
                string strException = e.Message;
                return;
            }

            return;
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <returns></returns>
        private bool FileExists()
        {
            return File.Exists(_FileName);
        }

        #endregion
    }
}
