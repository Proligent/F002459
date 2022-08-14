using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

/*******************************************************************************
 *
 * History Log:
 * REV  AUTHOR       DATE        COMMENTS
 *
 * A    CalvinXie    2022/08/14  First version. 
 * 
 *******************************************************************************/

namespace F002459
{
    static class Program
    {
        public static string g_str_ToolNumber = "";
        public static string g_str_ToolRev = "";
        private static System.Threading.Mutex mutex;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            g_str_ToolNumber = "F002459";
            g_str_ToolRev = "A";

            mutex = new System.Threading.Mutex(false, "F002459 QCNBackup Fixture");

            if (!mutex.WaitOne(0, false))
            {
                mutex.Close();
                mutex = null;
            }
            if (mutex != null)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());

                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("F002459 Already Running !!!");
            }


        }
    }
}
