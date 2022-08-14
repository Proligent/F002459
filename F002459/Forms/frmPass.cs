using System;
using System.Windows.Forms;

namespace F002459.Forms
{
    public partial class frmPass : Form
    {
        public frmPass()
        {
            InitializeComponent();
        }

        public string TestResult
        {
            get
            {
                return lblTestResult.Text;
            }
            set
            {
                lblTestResult.Text = value;
            }
        }

        private void frmPass_Load(object sender, EventArgs e)
        {
            this.btnContinue.Focus();
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
