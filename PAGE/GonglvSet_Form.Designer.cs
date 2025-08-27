namespace DbfTest.PAGE
{
    partial class GonglvSet_Form
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
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.stop_freq_textBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.manual_button = new System.Windows.Forms.Button();
            this.start_freq_textBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Items.AddRange(new object[] {
            "GHz",
            "MHz",
            "Hz"});
            this.comboBox2.Location = new System.Drawing.Point(265, 68);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(64, 20);
            this.comboBox2.TabIndex = 48;
            // 
            // stop_freq_textBox
            // 
            this.stop_freq_textBox.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stop_freq_textBox.Location = new System.Drawing.Point(122, 66);
            this.stop_freq_textBox.Name = "stop_freq_textBox";
            this.stop_freq_textBox.Size = new System.Drawing.Size(127, 23);
            this.stop_freq_textBox.TabIndex = 47;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(21, 65);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 19);
            this.label5.TabIndex = 46;
            this.label5.Text = "终止频率：";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "GHz",
            "MHz",
            "Hz"});
            this.comboBox1.Location = new System.Drawing.Point(265, 28);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(64, 20);
            this.comboBox1.TabIndex = 40;
            // 
            // manual_button
            // 
            this.manual_button.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.manual_button.Location = new System.Drawing.Point(378, 41);
            this.manual_button.Name = "manual_button";
            this.manual_button.Size = new System.Drawing.Size(87, 43);
            this.manual_button.TabIndex = 39;
            this.manual_button.Text = "保存设置";
            this.manual_button.UseVisualStyleBackColor = true;
            // 
            // start_freq_textBox
            // 
            this.start_freq_textBox.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.start_freq_textBox.Location = new System.Drawing.Point(122, 26);
            this.start_freq_textBox.Name = "start_freq_textBox";
            this.start_freq_textBox.Size = new System.Drawing.Size(127, 23);
            this.start_freq_textBox.TabIndex = 34;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(21, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 19);
            this.label1.TabIndex = 33;
            this.label1.Text = "起始频率：";
            // 
            // GonglvSet_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 127);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.stop_freq_textBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.manual_button);
            this.Controls.Add(this.start_freq_textBox);
            this.Controls.Add(this.label1);
            this.Name = "GonglvSet_Form";
            this.Text = "功率计控制";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.TextBox stop_freq_textBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button manual_button;
        private System.Windows.Forms.TextBox start_freq_textBox;
        private System.Windows.Forms.Label label1;
    }
}