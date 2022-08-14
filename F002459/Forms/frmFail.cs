using System;
using System.Windows.Forms;

namespace F002459.Forms
{
    public partial class frmFail : Form
    {
        public frmFail()
        {
            InitializeComponent();
        }

        public string Message
        {
            get
            {
                return lblErrorMessage.Text;
            }
            set
            {
                lblErrorMessage.Text = value;
            }
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
