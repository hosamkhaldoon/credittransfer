namespace CreditTransferTestClient
{
    partial class frmTest
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
            this.btnProcessTransactions = new System.Windows.Forms.Button();
            this.btnTestHTTPService = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnProcessTransactions
            // 
            this.btnProcessTransactions.Location = new System.Drawing.Point(1, 33);
            this.btnProcessTransactions.Name = "btnProcessTransactions";
            this.btnProcessTransactions.Size = new System.Drawing.Size(282, 36);
            this.btnProcessTransactions.TabIndex = 0;
            this.btnProcessTransactions.Text = "Process Transactions";
            this.btnProcessTransactions.UseVisualStyleBackColor = true;
            this.btnProcessTransactions.Click += new System.EventHandler(this.btnProcessTransactions_Click);
            // 
            // btnTestHTTPService
            // 
            this.btnTestHTTPService.Location = new System.Drawing.Point(1, 85);
            this.btnTestHTTPService.Name = "btnTestHTTPService";
            this.btnTestHTTPService.Size = new System.Drawing.Size(282, 40);
            this.btnTestHTTPService.TabIndex = 1;
            this.btnTestHTTPService.Text = "Test HTTP Service";
            this.btnTestHTTPService.UseVisualStyleBackColor = true;
            this.btnTestHTTPService.Click += new System.EventHandler(this.btnTestHTTPService_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1, 149);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(282, 40);
            this.button1.TabIndex = 2;
            this.button1.Text = "Delete Event Logs";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1, 210);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(282, 40);
            this.button2.TabIndex = 2;
            this.button2.Text = "Test Windows Service";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(1, 270);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(282, 40);
            this.button3.TabIndex = 3;
            this.button3.Text = "Test WCF Service";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // frmTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 322);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnTestHTTPService);
            this.Controls.Add(this.btnProcessTransactions);
            this.Name = "frmTest";
            this.Text = "frmTest";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnProcessTransactions;
        private System.Windows.Forms.Button btnTestHTTPService;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
    }
}