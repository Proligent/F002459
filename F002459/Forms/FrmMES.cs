using System;
using System.Windows.Forms;

namespace F002459.Forms
{
    public partial class frmMES : Form
    {
        #region Variable

        private string m_str_EID = "";
        private string m_str_WorkOrder = "";

        #endregion

        #region Propery

        public string EID
        {
            get
            {
                return m_str_EID;
            }
        }

        public string WorkOrder
        {
            get
            {
                return m_str_WorkOrder;
            }
        }

        #endregion

        public frmMES()
        {
            InitializeComponent();
        }

        private void frmMES_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;
            textBoxEID.Focus();
        }

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;

            // EID
            m_str_EID = this.textBoxEID.Text.Trim();
            if (m_str_EID.Length != 7)
            {
                return;
            }

            // WorkOrder
            m_str_WorkOrder = this.textBoxWorkOrder.Text.Trim();
            if (m_str_WorkOrder.Length <= 0)
            {
                return;
            }

            this.DialogResult = DialogResult.Yes;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
        }

        #endregion

        #region KeyDown

        private void textBoxEID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBoxEID.Text = "S00001";
                textBoxWorkOrder.Focus();
            }
            if (e.KeyValue == 13)
            {
                textBoxWorkOrder.Focus();
            }
        }

        private void textBoxWorkOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBoxWorkOrder.Text = "WO123456";
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
