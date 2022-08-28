using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F002459
{
    public class ScanSheetItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Id_Record { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Id_TemplateField { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FieldName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string OldContentStr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ContentStr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ViewContentStr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int CarriageReturnCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Id_FieldType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FieldTypeName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Type { get; set; }
    }

    public class QEsItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// 刘丹
        /// </summary>
        public string ChineseName { get; set; }
        /// <summary>
        /// 刘丹                                              
        /// </summary>
        public string EnglishName { get; set; }
    }

    public class DataItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MaterialNo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string BarCodeValue { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MEComment { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string QEComment { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ApproveEID { get; set; }
        /// <summary>
        /// 刘丹
        /// </summary>
        public string ApproveUsername { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ApproveTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string UpdateTime { get; set; }
        /// <summary>
        /// E837985/郝欣欣
        /// </summary>
        public string UpdateUser { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Valid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Id_Department { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Id_Template { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TemplateName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TestSoftware { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TestSoftwareRev { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Station { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ProductGroup { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<ScanSheetItem> Fields { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<QEsItem> QEs { get; set; }
    }

    public class ScanSheetRes
    {
        /// <summary>
        /// 
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// 操作成功
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<DataItem> Data { get; set; }
    }

}
