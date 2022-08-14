using System;
using System.Windows.Forms;

namespace F002459.Forms
{
    public partial class frmConfirmYESNO : Form
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

        public frmConfirmYESNO()
        {
            InitializeComponent();
        }

        private void frmConfirmYESNO_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;

            this.textBoxContent.Text = m_str_Content;
        }

        #region Event

        private void btnYES_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
        }

        private void btnNO_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
        }

        #endregion

    }
}
