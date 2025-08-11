namespace TestApp.PAGE
{
    partial class PinpuControl_Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PinpuControl_Form));
            this.scanOnce_button = new System.Windows.Forms.Button();
            this.save_button = new System.Windows.Forms.Button();
            this.trigger_comboBox = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.scanTime_textBox = new System.Windows.Forms.TextBox();
            this.setMark_button = new System.Windows.Forms.Button();
            this.getData_button = new System.Windows.Forms.Button();
            this.scanStart_button = new System.Windows.Forms.Button();
            this.set_button = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.vbw_textBox = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.rbw_textBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.stopFreq_textBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.startFreq_textBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.span_textBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.centerFreq_textBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.mark_textBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.SuspendLayout();
            // 
            // scanOnce_button
            // 
            this.scanOnce_button.BackColor = System.Drawing.Color.White;
            this.scanOnce_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanOnce_button.Location = new System.Drawing.Point(705, 170);
            this.scanOnce_button.Name = "scanOnce_button";
            this.scanOnce_button.Size = new System.Drawing.Size(129, 46);
            this.scanOnce_button.TabIndex = 98;
            this.scanOnce_button.Text = "单次扫描";
            this.scanOnce_button.UseVisualStyleBackColor = false;
            this.scanOnce_button.Click += new System.EventHandler(this.scanOnce_button_Click);
            // 
            // save_button
            // 
            this.save_button.BackColor = System.Drawing.Color.White;
            this.save_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.save_button.Location = new System.Drawing.Point(705, 66);
            this.save_button.Name = "save_button";
            this.save_button.Size = new System.Drawing.Size(129, 46);
            this.save_button.TabIndex = 97;
            this.save_button.Text = "保存参数";
            this.save_button.UseVisualStyleBackColor = false;
            this.save_button.Click += new System.EventHandler(this.save_button_Click);
            // 
            // trigger_comboBox
            // 
            this.trigger_comboBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.trigger_comboBox.FormattingEnabled = true;
            this.trigger_comboBox.Items.AddRange(new object[] {
            "IMM",
            "LINE",
            "EXT1",
            "EXT2",
            "EXT3",
            "VID",
            "TIME",
            "RFP"});
            this.trigger_comboBox.Location = new System.Drawing.Point(363, 190);
            this.trigger_comboBox.Name = "trigger_comboBox";
            this.trigger_comboBox.Size = new System.Drawing.Size(100, 29);
            this.trigger_comboBox.TabIndex = 96;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label15.Location = new System.Drawing.Point(270, 193);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(90, 21);
            this.label15.TabIndex = 95;
            this.label15.Text = "触发模式：";
            // 
            // scanTime_textBox
            // 
            this.scanTime_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanTime_textBox.Location = new System.Drawing.Point(363, 148);
            this.scanTime_textBox.Name = "scanTime_textBox";
            this.scanTime_textBox.Size = new System.Drawing.Size(100, 29);
            this.scanTime_textBox.TabIndex = 94;
            // 
            // setMark_button
            // 
            this.setMark_button.BackColor = System.Drawing.Color.White;
            this.setMark_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.setMark_button.Location = new System.Drawing.Point(522, 118);
            this.setMark_button.Name = "setMark_button";
            this.setMark_button.Size = new System.Drawing.Size(129, 46);
            this.setMark_button.TabIndex = 92;
            this.setMark_button.Text = "设置标记";
            this.setMark_button.UseVisualStyleBackColor = false;
            this.setMark_button.Click += new System.EventHandler(this.setMark_button_Click);
            // 
            // getData_button
            // 
            this.getData_button.BackColor = System.Drawing.Color.White;
            this.getData_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.getData_button.Location = new System.Drawing.Point(522, 170);
            this.getData_button.Name = "getData_button";
            this.getData_button.Size = new System.Drawing.Size(129, 46);
            this.getData_button.TabIndex = 91;
            this.getData_button.Text = "读取标记";
            this.getData_button.UseVisualStyleBackColor = false;
            this.getData_button.Click += new System.EventHandler(this.getData_button_Click);
            // 
            // scanStart_button
            // 
            this.scanStart_button.BackColor = System.Drawing.Color.White;
            this.scanStart_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanStart_button.Location = new System.Drawing.Point(705, 118);
            this.scanStart_button.Name = "scanStart_button";
            this.scanStart_button.Size = new System.Drawing.Size(129, 46);
            this.scanStart_button.TabIndex = 90;
            this.scanStart_button.Text = "连续扫描";
            this.scanStart_button.UseVisualStyleBackColor = false;
            this.scanStart_button.Click += new System.EventHandler(this.scanStart_button_Click);
            // 
            // set_button
            // 
            this.set_button.BackColor = System.Drawing.Color.White;
            this.set_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.set_button.Location = new System.Drawing.Point(522, 66);
            this.set_button.Name = "set_button";
            this.set_button.Size = new System.Drawing.Size(129, 46);
            this.set_button.TabIndex = 89;
            this.set_button.Text = "应用参数";
            this.set_button.UseVisualStyleBackColor = false;
            this.set_button.Click += new System.EventHandler(this.set_button_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label13.Location = new System.Drawing.Point(271, 151);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(90, 21);
            this.label13.TabIndex = 88;
            this.label13.Text = "扫描时间：";
            // 
            // vbw_textBox
            // 
            this.vbw_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.vbw_textBox.Location = new System.Drawing.Point(363, 107);
            this.vbw_textBox.Name = "vbw_textBox";
            this.vbw_textBox.Size = new System.Drawing.Size(100, 29);
            this.vbw_textBox.TabIndex = 87;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label12.Location = new System.Drawing.Point(270, 110);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(90, 21);
            this.label12.TabIndex = 86;
            this.label12.Text = "视频带宽：";
            // 
            // rbw_textBox
            // 
            this.rbw_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.rbw_textBox.Location = new System.Drawing.Point(363, 66);
            this.rbw_textBox.Name = "rbw_textBox";
            this.rbw_textBox.Size = new System.Drawing.Size(100, 29);
            this.rbw_textBox.TabIndex = 85;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label11.Location = new System.Drawing.Point(270, 69);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(106, 21);
            this.label11.TabIndex = 84;
            this.label11.Text = "分辨率带宽：";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label10.Location = new System.Drawing.Point(222, 193);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(37, 20);
            this.label10.TabIndex = 83;
            this.label10.Text = "GHz";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label9.Location = new System.Drawing.Point(222, 151);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(37, 20);
            this.label9.TabIndex = 82;
            this.label9.Text = "GHz";
            // 
            // stopFreq_textBox
            // 
            this.stopFreq_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stopFreq_textBox.Location = new System.Drawing.Point(116, 190);
            this.stopFreq_textBox.Name = "stopFreq_textBox";
            this.stopFreq_textBox.Size = new System.Drawing.Size(100, 29);
            this.stopFreq_textBox.TabIndex = 81;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label8.Location = new System.Drawing.Point(23, 193);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(90, 21);
            this.label8.TabIndex = 80;
            this.label8.Text = "终止频率：";
            // 
            // startFreq_textBox
            // 
            this.startFreq_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.startFreq_textBox.Location = new System.Drawing.Point(116, 148);
            this.startFreq_textBox.Name = "startFreq_textBox";
            this.startFreq_textBox.Size = new System.Drawing.Size(100, 29);
            this.startFreq_textBox.TabIndex = 79;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label7.Location = new System.Drawing.Point(23, 151);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(90, 21);
            this.label7.TabIndex = 78;
            this.label7.Text = "起始频率：";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label6.Location = new System.Drawing.Point(222, 110);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(37, 20);
            this.label6.TabIndex = 77;
            this.label6.Text = "GHz";
            // 
            // span_textBox
            // 
            this.span_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.span_textBox.Location = new System.Drawing.Point(116, 107);
            this.span_textBox.Name = "span_textBox";
            this.span_textBox.Size = new System.Drawing.Size(100, 29);
            this.span_textBox.TabIndex = 76;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(23, 110);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 21);
            this.label3.TabIndex = 75;
            this.label3.Text = "频宽：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(222, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 20);
            this.label2.TabIndex = 74;
            this.label2.Text = "GHz";
            // 
            // centerFreq_textBox
            // 
            this.centerFreq_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.centerFreq_textBox.Location = new System.Drawing.Point(116, 66);
            this.centerFreq_textBox.Name = "centerFreq_textBox";
            this.centerFreq_textBox.Size = new System.Drawing.Size(100, 29);
            this.centerFreq_textBox.TabIndex = 73;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(23, 69);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 21);
            this.label1.TabIndex = 72;
            this.label1.Text = "中心频率：";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(222, 233);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 20);
            this.label4.TabIndex = 101;
            this.label4.Text = "GHz";
            // 
            // mark_textBox
            // 
            this.mark_textBox.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.mark_textBox.Location = new System.Drawing.Point(116, 230);
            this.mark_textBox.Name = "mark_textBox";
            this.mark_textBox.Size = new System.Drawing.Size(100, 29);
            this.mark_textBox.TabIndex = 100;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(23, 233);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(74, 21);
            this.label5.TabIndex = 99;
            this.label5.Text = "标记点：";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.pictureBox1);
            this.flowLayoutPanel1.Controls.Add(this.pictureBox2);
            this.flowLayoutPanel1.Controls.Add(this.pictureBox3);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(751, 5);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(128, 32);
            this.flowLayoutPanel1.TabIndex = 102;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(107, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(18, 18);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(83, 3);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(18, 18);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 1;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.pictureBox2_Click);
            // 
            // pictureBox3
            // 
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.Location = new System.Drawing.Point(59, 3);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(18, 18);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 2;
            this.pictureBox3.TabStop = false;
            this.pictureBox3.Click += new System.EventHandler(this.pictureBox3_Click);
            // 
            // PinpuControl_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(878, 275);
            this.ControlBox = false;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.mark_textBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.scanOnce_button);
            this.Controls.Add(this.save_button);
            this.Controls.Add(this.trigger_comboBox);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.scanTime_textBox);
            this.Controls.Add(this.setMark_button);
            this.Controls.Add(this.getData_button);
            this.Controls.Add(this.scanStart_button);
            this.Controls.Add(this.set_button);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.vbw_textBox);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.rbw_textBox);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.stopFreq_textBox);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.startFreq_textBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.span_textBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.centerFreq_textBox);
            this.Controls.Add(this.label1);
            this.Name = "PinpuControl_Form";
            this.Text = "频谱分析仪控制";
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button scanOnce_button;
        private System.Windows.Forms.Button save_button;
        private System.Windows.Forms.ComboBox trigger_comboBox;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox scanTime_textBox;
        private System.Windows.Forms.Button setMark_button;
        private System.Windows.Forms.Button getData_button;
        private System.Windows.Forms.Button scanStart_button;
        private System.Windows.Forms.Button set_button;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox vbw_textBox;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox rbw_textBox;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox stopFreq_textBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox startFreq_textBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox span_textBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox centerFreq_textBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox mark_textBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
    }
}