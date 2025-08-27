namespace DbfTest
{
    partial class LANSet_Form
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelStartIP;
        private System.Windows.Forms.Label labelEndIP;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.Label labelTimeout;
        private System.Windows.Forms.Label labelMaxConcurrent;
        private System.Windows.Forms.TextBox txtStartIP1;
        private System.Windows.Forms.TextBox txtStartIP2;
        private System.Windows.Forms.TextBox txtStartIP3;
        private System.Windows.Forms.TextBox txtStartIP4;
        private System.Windows.Forms.TextBox txtEndIP1;
        private System.Windows.Forms.TextBox txtEndIP2;
        private System.Windows.Forms.TextBox txtEndIP3;
        private System.Windows.Forms.TextBox txtEndIP4;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.NumericUpDown nudTimeout;
        private System.Windows.Forms.NumericUpDown nudMaxConcurrent;
        private System.Windows.Forms.CheckBox chkQueryIdn;
        private System.Windows.Forms.CheckBox chkAutoSave;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelDot1;
        private System.Windows.Forms.Label labelDot2;
        private System.Windows.Forms.Label labelDot3;
        private System.Windows.Forms.Label labelDot4;
        private System.Windows.Forms.Label labelDot5;
        private System.Windows.Forms.Label labelDot6;
        private System.Windows.Forms.Label labelDot7;
        private System.Windows.Forms.Label labelDot8;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelStartIP = new System.Windows.Forms.Label();
            this.labelEndIP = new System.Windows.Forms.Label();
            this.labelPort = new System.Windows.Forms.Label();
            this.labelTimeout = new System.Windows.Forms.Label();
            this.labelMaxConcurrent = new System.Windows.Forms.Label();
            this.txtStartIP1 = new System.Windows.Forms.TextBox();
            this.txtStartIP2 = new System.Windows.Forms.TextBox();
            this.txtStartIP3 = new System.Windows.Forms.TextBox();
            this.txtStartIP4 = new System.Windows.Forms.TextBox();
            this.txtEndIP1 = new System.Windows.Forms.TextBox();
            this.txtEndIP2 = new System.Windows.Forms.TextBox();
            this.txtEndIP3 = new System.Windows.Forms.TextBox();
            this.txtEndIP4 = new System.Windows.Forms.TextBox();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.nudTimeout = new System.Windows.Forms.NumericUpDown();
            this.nudMaxConcurrent = new System.Windows.Forms.NumericUpDown();
            this.chkQueryIdn = new System.Windows.Forms.CheckBox();
            this.chkAutoSave = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelDot1 = new System.Windows.Forms.Label();
            this.labelDot2 = new System.Windows.Forms.Label();
            this.labelDot3 = new System.Windows.Forms.Label();
            this.labelDot4 = new System.Windows.Forms.Label();
            this.labelDot5 = new System.Windows.Forms.Label();
            this.labelDot6 = new System.Windows.Forms.Label();
            this.labelDot7 = new System.Windows.Forms.Label();
            this.labelDot8 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxConcurrent)).BeginInit();
            this.SuspendLayout();
            // 
            // labelStartIP
            // 
            this.labelStartIP.AutoSize = true;
            this.labelStartIP.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelStartIP.Location = new System.Drawing.Point(20, 20);
            this.labelStartIP.Name = "labelStartIP";
            this.labelStartIP.Size = new System.Drawing.Size(103, 16);
            this.labelStartIP.TabIndex = 0;
            this.labelStartIP.Text = "起始 IP 地址";
            // 
            // labelEndIP
            // 
            this.labelEndIP.AutoSize = true;
            this.labelEndIP.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelEndIP.Location = new System.Drawing.Point(20, 60);
            this.labelEndIP.Name = "labelEndIP";
            this.labelEndIP.Size = new System.Drawing.Size(103, 16);
            this.labelEndIP.TabIndex = 8;
            this.labelEndIP.Text = "终止 IP 地址";
            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelPort.Location = new System.Drawing.Point(20, 100);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(55, 16);
            this.labelPort.TabIndex = 16;
            this.labelPort.Text = "端口号";
            // 
            // labelTimeout
            // 
            this.labelTimeout.AutoSize = true;
            this.labelTimeout.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelTimeout.Location = new System.Drawing.Point(20, 140);
            this.labelTimeout.Name = "labelTimeout";
            this.labelTimeout.Size = new System.Drawing.Size(119, 16);
            this.labelTimeout.TabIndex = 18;
            this.labelTimeout.Text = "超时时间(毫秒)";
            // 
            // labelMaxConcurrent
            // 
            this.labelMaxConcurrent.AutoSize = true;
            this.labelMaxConcurrent.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelMaxConcurrent.Location = new System.Drawing.Point(20, 180);
            this.labelMaxConcurrent.Name = "labelMaxConcurrent";
            this.labelMaxConcurrent.Size = new System.Drawing.Size(119, 16);
            this.labelMaxConcurrent.TabIndex = 20;
            this.labelMaxConcurrent.Text = "最大并发扫描数";
            // 
            // txtStartIP1
            // 
            this.txtStartIP1.Location = new System.Drawing.Point(150, 17);
            this.txtStartIP1.MaxLength = 3;
            this.txtStartIP1.Name = "txtStartIP1";
            this.txtStartIP1.Size = new System.Drawing.Size(40, 26);
            this.txtStartIP1.TabIndex = 1;
            // 
            // txtStartIP2
            // 
            this.txtStartIP2.Location = new System.Drawing.Point(213, 17);
            this.txtStartIP2.MaxLength = 3;
            this.txtStartIP2.Name = "txtStartIP2";
            this.txtStartIP2.Size = new System.Drawing.Size(40, 26);
            this.txtStartIP2.TabIndex = 3;
            // 
            // txtStartIP3
            // 
            this.txtStartIP3.Location = new System.Drawing.Point(273, 17);
            this.txtStartIP3.MaxLength = 3;
            this.txtStartIP3.Name = "txtStartIP3";
            this.txtStartIP3.Size = new System.Drawing.Size(40, 26);
            this.txtStartIP3.TabIndex = 5;
            // 
            // txtStartIP4
            // 
            this.txtStartIP4.Location = new System.Drawing.Point(335, 17);
            this.txtStartIP4.MaxLength = 3;
            this.txtStartIP4.Name = "txtStartIP4";
            this.txtStartIP4.Size = new System.Drawing.Size(40, 26);
            this.txtStartIP4.TabIndex = 7;
            // 
            // txtEndIP1
            // 
            this.txtEndIP1.Location = new System.Drawing.Point(150, 57);
            this.txtEndIP1.MaxLength = 3;
            this.txtEndIP1.Name = "txtEndIP1";
            this.txtEndIP1.Size = new System.Drawing.Size(40, 26);
            this.txtEndIP1.TabIndex = 9;
            // 
            // txtEndIP2
            // 
            this.txtEndIP2.Location = new System.Drawing.Point(213, 57);
            this.txtEndIP2.MaxLength = 3;
            this.txtEndIP2.Name = "txtEndIP2";
            this.txtEndIP2.Size = new System.Drawing.Size(40, 26);
            this.txtEndIP2.TabIndex = 11;
            // 
            // txtEndIP3
            // 
            this.txtEndIP3.Location = new System.Drawing.Point(273, 57);
            this.txtEndIP3.MaxLength = 3;
            this.txtEndIP3.Name = "txtEndIP3";
            this.txtEndIP3.Size = new System.Drawing.Size(40, 26);
            this.txtEndIP3.TabIndex = 13;
            // 
            // txtEndIP4
            // 
            this.txtEndIP4.Location = new System.Drawing.Point(335, 57);
            this.txtEndIP4.MaxLength = 3;
            this.txtEndIP4.Name = "txtEndIP4";
            this.txtEndIP4.Size = new System.Drawing.Size(40, 26);
            this.txtEndIP4.TabIndex = 15;
            // 
            // nudPort
            // 
            this.nudPort.Location = new System.Drawing.Point(150, 98);
            this.nudPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(80, 26);
            this.nudPort.TabIndex = 17;
            this.nudPort.Value = new decimal(new int[] {
            5025,
            0,
            0,
            0});
            // 
            // nudTimeout
            // 
            this.nudTimeout.Location = new System.Drawing.Point(150, 138);
            this.nudTimeout.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudTimeout.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudTimeout.Name = "nudTimeout";
            this.nudTimeout.Size = new System.Drawing.Size(80, 26);
            this.nudTimeout.TabIndex = 19;
            this.nudTimeout.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // nudMaxConcurrent
            // 
            this.nudMaxConcurrent.Location = new System.Drawing.Point(150, 178);
            this.nudMaxConcurrent.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudMaxConcurrent.Name = "nudMaxConcurrent";
            this.nudMaxConcurrent.Size = new System.Drawing.Size(80, 26);
            this.nudMaxConcurrent.TabIndex = 21;
            this.nudMaxConcurrent.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // chkQueryIdn
            // 
            this.chkQueryIdn.AutoSize = true;
            this.chkQueryIdn.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chkQueryIdn.Location = new System.Drawing.Point(23, 220);
            this.chkQueryIdn.Name = "chkQueryIdn";
            this.chkQueryIdn.Size = new System.Drawing.Size(170, 20);
            this.chkQueryIdn.TabIndex = 22;
            this.chkQueryIdn.Text = "扫描时查询设备 IDN";
            // 
            // chkAutoSave
            // 
            this.chkAutoSave.AutoSize = true;
            this.chkAutoSave.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chkAutoSave.Location = new System.Drawing.Point(23, 258);
            this.chkAutoSave.Name = "chkAutoSave";
            this.chkAutoSave.Size = new System.Drawing.Size(154, 20);
            this.chkAutoSave.TabIndex = 23;
            this.chkAutoSave.Text = "自动保存扫描设置";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(77, 312);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(84, 37);
            this.btnOK.TabIndex = 24;
            this.btnOK.Text = "保存";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(260, 312);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(84, 37);
            this.btnCancel.TabIndex = 25;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // labelDot1
            // 
            this.labelDot1.AutoSize = true;
            this.labelDot1.Location = new System.Drawing.Point(196, 27);
            this.labelDot1.Name = "labelDot1";
            this.labelDot1.Size = new System.Drawing.Size(15, 16);
            this.labelDot1.TabIndex = 2;
            this.labelDot1.Text = ".";
            // 
            // labelDot2
            // 
            this.labelDot2.AutoSize = true;
            this.labelDot2.Location = new System.Drawing.Point(257, 27);
            this.labelDot2.Name = "labelDot2";
            this.labelDot2.Size = new System.Drawing.Size(15, 16);
            this.labelDot2.TabIndex = 4;
            this.labelDot2.Text = ".";
            // 
            // labelDot3
            // 
            this.labelDot3.AutoSize = true;
            this.labelDot3.Location = new System.Drawing.Point(319, 27);
            this.labelDot3.Name = "labelDot3";
            this.labelDot3.Size = new System.Drawing.Size(15, 16);
            this.labelDot3.TabIndex = 6;
            this.labelDot3.Text = ".";
            // 
            // labelDot4
            // 
            this.labelDot4.AutoSize = true;
            this.labelDot4.Location = new System.Drawing.Point(196, 67);
            this.labelDot4.Name = "labelDot4";
            this.labelDot4.Size = new System.Drawing.Size(15, 16);
            this.labelDot4.TabIndex = 10;
            this.labelDot4.Text = ".";
            // 
            // labelDot5
            // 
            this.labelDot5.AutoSize = true;
            this.labelDot5.Location = new System.Drawing.Point(257, 67);
            this.labelDot5.Name = "labelDot5";
            this.labelDot5.Size = new System.Drawing.Size(15, 16);
            this.labelDot5.TabIndex = 12;
            this.labelDot5.Text = ".";
            // 
            // labelDot6
            // 
            this.labelDot6.AutoSize = true;
            this.labelDot6.Location = new System.Drawing.Point(319, 67);
            this.labelDot6.Name = "labelDot6";
            this.labelDot6.Size = new System.Drawing.Size(15, 16);
            this.labelDot6.TabIndex = 14;
            this.labelDot6.Text = ".";
            // 
            // labelDot7
            // 
            this.labelDot7.Location = new System.Drawing.Point(0, 0);
            this.labelDot7.Name = "labelDot7";
            this.labelDot7.Size = new System.Drawing.Size(100, 23);
            this.labelDot7.TabIndex = 0;
            // 
            // labelDot8
            // 
            this.labelDot8.Location = new System.Drawing.Point(0, 0);
            this.labelDot8.Name = "labelDot8";
            this.labelDot8.Size = new System.Drawing.Size(100, 23);
            this.labelDot8.TabIndex = 0;
            // 
            // LANSet_Form
            // 
            this.ClientSize = new System.Drawing.Size(410, 392);
            this.Controls.Add(this.labelStartIP);
            this.Controls.Add(this.txtStartIP1);
            this.Controls.Add(this.labelDot1);
            this.Controls.Add(this.txtStartIP2);
            this.Controls.Add(this.labelDot2);
            this.Controls.Add(this.txtStartIP3);
            this.Controls.Add(this.labelDot3);
            this.Controls.Add(this.txtStartIP4);
            this.Controls.Add(this.labelEndIP);
            this.Controls.Add(this.txtEndIP1);
            this.Controls.Add(this.labelDot4);
            this.Controls.Add(this.txtEndIP2);
            this.Controls.Add(this.labelDot5);
            this.Controls.Add(this.txtEndIP3);
            this.Controls.Add(this.labelDot6);
            this.Controls.Add(this.txtEndIP4);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.nudPort);
            this.Controls.Add(this.labelTimeout);
            this.Controls.Add(this.nudTimeout);
            this.Controls.Add(this.labelMaxConcurrent);
            this.Controls.Add(this.nudMaxConcurrent);
            this.Controls.Add(this.chkQueryIdn);
            this.Controls.Add(this.chkAutoSave);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LANSet_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LAN 扫描设置";
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxConcurrent)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}

