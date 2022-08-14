using System;
using System.Windows.Forms;

namespace F002459.Forms
{
    public partial class frmMCF : Form
    {
        #region Variable

        private string m_str_SKU = "";

        #endregion

        #region Propery

        public string SKU
        {
            get
            {
                return m_str_SKU;
            }
        }

        #endregion

        public frmMCF()
        {
            InitializeComponent();
        }
 
        private void frmMCF_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;
            textBoxSKU.Focus();
        }

        private void frmMCF_FormClosing(object sender, FormClosingEventArgs e)
        {
            //this.DialogResult = DialogResult.Cancel;
        }

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;

            // SKU
            m_str_SKU = this.textBoxSKU.Text.Trim();
            if (m_str_SKU.Length <= 0)
            {
                return;
            }

            this.DialogResult = DialogResult.Yes;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
        }

        private void btnSetup_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        #endregion
     
        #region KeyDown

        private void textBoxSKU_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBoxSKU.Text = "EDA51-1-B633SOGO";
                btnOK.Focus();
            }
            if (e.KeyValue == 13)
            {
                btnOK.Focus();
            }
        }

        #endregion

    }
}
