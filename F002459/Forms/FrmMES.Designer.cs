namespace F002459.Forms
{
    partial class frmMES
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblEID = new System.Windows.Forms.Label();
            this.lblWorkOrder = new System.Windows.Forms.Label();
            this.textBoxEID = new System.Windows.Forms.TextBox();
            this.textBoxWorkOrder = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblEID
            // 
            this.lblEID.AutoSize = true;
            this.lblEID.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.lblEID.Location = new System.Drawing.Point(22, 49);
            this.lblEID.Name = "lblEID";
            this.lblEID.Size = new System.Drawing.Size(45, 24);
            this.lblEID.TabIndex = 0;
            this.lblEID.Text = "EID:";
            this.lblEID.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWorkOrder
            // 
            this.lblWorkOrder.AutoSize = true;
            this.lblWorkOrder.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.lblWorkOrder.Location = new System.Drawing.Point(22, 109);
            this.lblWorkOrder.Name = "lblWorkOrder";
            this.lblWorkOrder.Size = new System.Drawing.Size(108, 24);
            this.lblWorkOrder.TabIndex = 0;
            this.lblWorkOrder.Text = "WorkOrder:";
            this.lblWorkOrder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxEID
            // 
            this.textBoxEID.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.textBoxEID.Location = new System.Drawing.Point(136, 49);
            this.textBoxEID.Name = "textBoxEID";
            this.textBoxEID.Size = new System.Drawing.Size(203, 29);
            this.textBoxEID.TabIndex = 1;
            this.textBoxEID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxEID_KeyDown);
            // 
            // textBoxWorkOrder
            // 
            this.textBoxWorkOrder.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.textBoxWorkOrder.Location = new System.Drawing.Point(136, 106);
            this.textBoxWorkOrder.Name = "textBoxWorkOrder";
            this.textBoxWorkOrder.Size = new System.Drawing.Size(203, 29);
            this.textBoxWorkOrder.TabIndex = 1;
            this.textBoxWorkOrder.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxWorkOrder_KeyDown);
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.btnOK.Location = new System.Drawing.Point(150, 221);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 46);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
            this.btnCancel.Location = new System.Drawing.Point(259, 221);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 46);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // frmMES
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 279);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.textBoxWorkOrder);
            this.Controls.Add(this.textBoxEID);
            this.Controls.Add(this.lblWorkOrder);
            this.Controls.Add(this.lblEID);
            this.MaximizeBox = false;
            this.Name = "frmMES";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MES";
            this.Load += new System.EventHandler(this.frmMES_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblEID;
        private System.Windows.Forms.Label lblWorkOrder;
        private System.Windows.Forms.TextBox textBoxEID;
        private System.Windows.Forms.TextBox textBoxWorkOrder;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}