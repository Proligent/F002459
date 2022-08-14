using System;
using System.Windows.Forms;

namespace F002459.Forms
{
    public partial class frmConfirmOK : Form
    {
        #region Variable

        private string m_str_Content;

        #endregion

        #region Propery

        public string Content
        {
            get
            {
                return m_str_Content;
            }
            set
            {
                m_str_Content = value;
            }
        }

        #endregion

        public frmConfirmOK()
        {
            InitializeComponent();
        }

        private void frmConfirmOK_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;

            this.textBoxContent.Text = m_str_Content;
        }

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        #endregion


    }
}
