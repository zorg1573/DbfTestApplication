using System.Runtime.InteropServices;

namespace TestApp.ViewControl
{
    partial class SpectrumControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.scanOnce_button = new System.Windows.Forms.Button();
            this.save_button = new System.Windows.Forms.Button();
            this.trigger_comboBox = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.scanTime_textBox = new System.Windows.Forms.TextBox();
            this.disconnect_button = new System.Windows.Forms.Button();
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
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Xuhao = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Timestamp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CenterFrequencyGHz = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StartFrequencyMHz = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StopFrequencyGHz = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SpanGHz = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RBW = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.VBW = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SweepTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PeakFrequencyHz = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PeakPowerdBm = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewPNResult = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPNResult)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.scanOnce_button);
            this.splitContainer1.Panel1.Controls.Add(this.save_button);
            this.splitContainer1.Panel1.Controls.Add(this.trigger_comboBox);
            this.splitContainer1.Panel1.Controls.Add(this.label15);
            this.splitContainer1.Panel1.Controls.Add(this.scanTime_textBox);
            this.splitContainer1.Panel1.Controls.Add(this.disconnect_button);
            this.splitContainer1.Panel1.Controls.Add(this.setMark_button);
            this.splitContainer1.Panel1.Controls.Add(this.getData_button);
            this.splitContainer1.Panel1.Controls.Add(this.scanStart_button);
            this.splitContainer1.Panel1.Controls.Add(this.set_button);
            this.splitContainer1.Panel1.Controls.Add(this.label13);
            this.splitContainer1.Panel1.Controls.Add(this.vbw_textBox);
            this.splitContainer1.Panel1.Controls.Add(this.label12);
            this.splitContainer1.Panel1.Controls.Add(this.rbw_textBox);
            this.splitContainer1.Panel1.Controls.Add(this.label11);
            this.splitContainer1.Panel1.Controls.Add(this.label10);
            this.splitContainer1.Panel1.Controls.Add(this.label9);
            this.splitContainer1.Panel1.Controls.Add(this.stopFreq_textBox);
            this.splitContainer1.Panel1.Controls.Add(this.label8);
            this.splitContainer1.Panel1.Controls.Add(this.startFreq_textBox);
            this.splitContainer1.Panel1.Controls.Add(this.label7);
            this.splitContainer1.Panel1.Controls.Add(this.label6);
            this.splitContainer1.Panel1.Controls.Add(this.span_textBox);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.centerFreq_textBox);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.label5);
            this.splitContainer1.Panel1.Controls.Add(this.label4);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dataGridView1);
            this.splitContainer1.Panel2.Controls.Add(this.dataGridViewPNResult);
            this.splitContainer1.Size = new System.Drawing.Size(896, 516);
            this.splitContainer1.SplitterDistance = 255;
            this.splitContainer1.TabIndex = 37;
            // 
            // scanOnce_button
            // 
            this.scanOnce_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanOnce_button.Location = new System.Drawing.Point(774, 101);
            this.scanOnce_button.Name = "scanOnce_button";
            this.scanOnce_button.Size = new System.Drawing.Size(56, 46);
            this.scanOnce_button.TabIndex = 71;
            this.scanOnce_button.Text = "单次扫描";
            this.scanOnce_button.UseVisualStyleBackColor = true;
            this.scanOnce_button.Click += new System.EventHandler(this.scanOnce_button_Click);
            // 
            // save_button
            // 
            this.save_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.save_button.Location = new System.Drawing.Point(701, 49);
            this.save_button.Name = "save_button";
            this.save_button.Size = new System.Drawing.Size(129, 46);
            this.save_button.TabIndex = 70;
            this.save_button.Text = "保存参数";
            this.save_button.UseVisualStyleBackColor = true;
            this.save_button.Click += new System.EventHandler(this.save_button_Click);
            // 
            // trigger_comboBox
            // 
            this.trigger_comboBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
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
            this.trigger_comboBox.Location = new System.Drawing.Point(359, 173);
            this.trigger_comboBox.Name = "trigger_comboBox";
            this.trigger_comboBox.Size = new System.Drawing.Size(100, 24);
            this.trigger_comboBox.TabIndex = 69;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label15.Location = new System.Drawing.Point(266, 176);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(87, 16);
            this.label15.TabIndex = 68;
            this.label15.Text = "触发模式：";
            // 
            // scanTime_textBox
            // 
            this.scanTime_textBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanTime_textBox.Location = new System.Drawing.Point(359, 131);
            this.scanTime_textBox.Name = "scanTime_textBox";
            this.scanTime_textBox.Size = new System.Drawing.Size(100, 26);
            this.scanTime_textBox.TabIndex = 66;
            // 
            // disconnect_button
            // 
            this.disconnect_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.disconnect_button.Location = new System.Drawing.Point(701, 153);
            this.disconnect_button.Name = "disconnect_button";
            this.disconnect_button.Size = new System.Drawing.Size(129, 46);
            this.disconnect_button.TabIndex = 65;
            this.disconnect_button.Text = "关闭连接";
            this.disconnect_button.UseVisualStyleBackColor = true;
            this.disconnect_button.Click += new System.EventHandler(this.disconnect_button_Click);
            // 
            // setMark_button
            // 
            this.setMark_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.setMark_button.Location = new System.Drawing.Point(518, 101);
            this.setMark_button.Name = "setMark_button";
            this.setMark_button.Size = new System.Drawing.Size(129, 46);
            this.setMark_button.TabIndex = 63;
            this.setMark_button.Text = "设置标记";
            this.setMark_button.UseVisualStyleBackColor = true;
            this.setMark_button.Click += new System.EventHandler(this.setMark_button_Click);
            // 
            // getData_button
            // 
            this.getData_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.getData_button.Location = new System.Drawing.Point(518, 153);
            this.getData_button.Name = "getData_button";
            this.getData_button.Size = new System.Drawing.Size(129, 46);
            this.getData_button.TabIndex = 62;
            this.getData_button.Text = "读取标记";
            this.getData_button.UseVisualStyleBackColor = true;
            this.getData_button.Click += new System.EventHandler(this.getData_button_Click);
            // 
            // scanStart_button
            // 
            this.scanStart_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scanStart_button.Location = new System.Drawing.Point(701, 101);
            this.scanStart_button.Name = "scanStart_button";
            this.scanStart_button.Size = new System.Drawing.Size(56, 46);
            this.scanStart_button.TabIndex = 61;
            this.scanStart_button.Text = "连续扫描";
            this.scanStart_button.UseVisualStyleBackColor = true;
            this.scanStart_button.Click += new System.EventHandler(this.scanStart_button_Click);
            // 
            // set_button
            // 
            this.set_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.set_button.Location = new System.Drawing.Point(518, 49);
            this.set_button.Name = "set_button";
            this.set_button.Size = new System.Drawing.Size(129, 46);
            this.set_button.TabIndex = 60;
            this.set_button.Text = "应用参数";
            this.set_button.UseVisualStyleBackColor = true;
            this.set_button.Click += new System.EventHandler(this.set_button_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label13.Location = new System.Drawing.Point(267, 134);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(87, 16);
            this.label13.TabIndex = 55;
            this.label13.Text = "扫描时间：";
            // 
            // vbw_textBox
            // 
            this.vbw_textBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.vbw_textBox.Location = new System.Drawing.Point(359, 90);
            this.vbw_textBox.Name = "vbw_textBox";
            this.vbw_textBox.Size = new System.Drawing.Size(100, 26);
            this.vbw_textBox.TabIndex = 54;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label12.Location = new System.Drawing.Point(266, 93);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(87, 16);
            this.label12.TabIndex = 53;
            this.label12.Text = "视频带宽：";
            // 
            // rbw_textBox
            // 
            this.rbw_textBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.rbw_textBox.Location = new System.Drawing.Point(359, 49);
            this.rbw_textBox.Name = "rbw_textBox";
            this.rbw_textBox.Size = new System.Drawing.Size(100, 26);
            this.rbw_textBox.TabIndex = 52;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label11.Location = new System.Drawing.Point(266, 52);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(103, 16);
            this.label11.TabIndex = 51;
            this.label11.Text = "分辨率带宽：";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label10.Location = new System.Drawing.Point(218, 176);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(31, 16);
            this.label10.TabIndex = 50;
            this.label10.Text = "GHz";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label9.Location = new System.Drawing.Point(218, 134);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(31, 16);
            this.label9.TabIndex = 49;
            this.label9.Text = "MHz";
            // 
            // stopFreq_textBox
            // 
            this.stopFreq_textBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stopFreq_textBox.Location = new System.Drawing.Point(112, 173);
            this.stopFreq_textBox.Name = "stopFreq_textBox";
            this.stopFreq_textBox.Size = new System.Drawing.Size(100, 26);
            this.stopFreq_textBox.TabIndex = 48;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label8.Location = new System.Drawing.Point(19, 176);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(87, 16);
            this.label8.TabIndex = 47;
            this.label8.Text = "终止频率：";
            // 
            // startFreq_textBox
            // 
            this.startFreq_textBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.startFreq_textBox.Location = new System.Drawing.Point(112, 131);
            this.startFreq_textBox.Name = "startFreq_textBox";
            this.startFreq_textBox.Size = new System.Drawing.Size(100, 26);
            this.startFreq_textBox.TabIndex = 46;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label7.Location = new System.Drawing.Point(19, 134);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(87, 16);
            this.label7.TabIndex = 45;
            this.label7.Text = "起始频率：";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label6.Location = new System.Drawing.Point(218, 93);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(31, 16);
            this.label6.TabIndex = 44;
            this.label6.Text = "GHz";
            // 
            // span_textBox
            // 
            this.span_textBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.span_textBox.Location = new System.Drawing.Point(112, 90);
            this.span_textBox.Name = "span_textBox";
            this.span_textBox.Size = new System.Drawing.Size(100, 26);
            this.span_textBox.TabIndex = 43;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(19, 93);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 16);
            this.label3.TabIndex = 42;
            this.label3.Text = "频宽：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(218, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 16);
            this.label2.TabIndex = 41;
            this.label2.Text = "GHz";
            // 
            // centerFreq_textBox
            // 
            this.centerFreq_textBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.centerFreq_textBox.Location = new System.Drawing.Point(112, 49);
            this.centerFreq_textBox.Name = "centerFreq_textBox";
            this.centerFreq_textBox.Size = new System.Drawing.Size(100, 26);
            this.centerFreq_textBox.TabIndex = 40;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(19, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 16);
            this.label1.TabIndex = 39;
            this.label1.Text = "中心频率：";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(5, 21);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 12);
            this.label5.TabIndex = 38;
            this.label5.Text = "未连接";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(5, 4);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 37;
            this.label4.Text = "设备名";
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Xuhao,
            this.Timestamp,
            this.CenterFrequencyGHz,
            this.StartFrequencyMHz,
            this.StopFrequencyGHz,
            this.SpanGHz,
            this.RBW,
            this.VBW,
            this.SweepTime,
            this.PeakFrequencyHz,
            this.PeakPowerdBm});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(896, 257);
            this.dataGridView1.TabIndex = 1;
            // 
            // Xuhao
            // 
            this.Xuhao.HeaderText = "序号";
            this.Xuhao.Name = "Xuhao";
            // 
            // Timestamp
            // 
            this.Timestamp.HeaderText = "时间";
            this.Timestamp.Name = "Timestamp";
            // 
            // CenterFrequencyGHz
            // 
            this.CenterFrequencyGHz.HeaderText = "中心频率(GHz)";
            this.CenterFrequencyGHz.Name = "CenterFrequencyGHz";
            // 
            // StartFrequencyMHz
            // 
            this.StartFrequencyMHz.HeaderText = "起始频率(MHz)";
            this.StartFrequencyMHz.Name = "StartFrequencyMHz";
            // 
            // StopFrequencyGHz
            // 
            this.StopFrequencyGHz.HeaderText = "终止频率(GHz)";
            this.StopFrequencyGHz.Name = "StopFrequencyGHz";
            // 
            // SpanGHz
            // 
            this.SpanGHz.HeaderText = "频宽(GHz)";
            this.SpanGHz.Name = "SpanGHz";
            // 
            // RBW
            // 
            this.RBW.HeaderText = "RBW";
            this.RBW.Name = "RBW";
            // 
            // VBW
            // 
            this.VBW.HeaderText = "VBW";
            this.VBW.Name = "VBW";
            // 
            // SweepTime
            // 
            this.SweepTime.HeaderText = "扫描时间(s)";
            this.SweepTime.Name = "SweepTime";
            // 
            // PeakFrequencyHz
            // 
            this.PeakFrequencyHz.HeaderText = "峰值频率(Hz)";
            this.PeakFrequencyHz.Name = "PeakFrequencyHz";
            // 
            // PeakPowerdBm
            // 
            this.PeakPowerdBm.HeaderText = "峰值功率(dBm)";
            this.PeakPowerdBm.Name = "PeakPowerdBm";
            // 
            // dataGridViewPNResult
            // 
            this.dataGridViewPNResult.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewPNResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewPNResult.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewPNResult.Name = "dataGridViewPNResult";
            this.dataGridViewPNResult.RowTemplate.Height = 23;
            this.dataGridViewPNResult.Size = new System.Drawing.Size(896, 257);
            this.dataGridViewPNResult.TabIndex = 0;
            // 
            // SpectrumControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "SpectrumControl";
            this.Size = new System.Drawing.Size(896, 516);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewPNResult)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
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
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DataGridView dataGridViewPNResult;
        private System.Windows.Forms.Button disconnect_button;
        private System.Windows.Forms.TextBox scanTime_textBox;
        private System.Windows.Forms.ComboBox trigger_comboBox;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Xuhao;
        private System.Windows.Forms.DataGridViewTextBoxColumn Timestamp;
        private System.Windows.Forms.DataGridViewTextBoxColumn CenterFrequencyGHz;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartFrequencyMHz;
        private System.Windows.Forms.DataGridViewTextBoxColumn StopFrequencyGHz;
        private System.Windows.Forms.DataGridViewTextBoxColumn SpanGHz;
        private System.Windows.Forms.DataGridViewTextBoxColumn RBW;
        private System.Windows.Forms.DataGridViewTextBoxColumn VBW;
        private System.Windows.Forms.DataGridViewTextBoxColumn SweepTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn PeakFrequencyHz;
        private System.Windows.Forms.DataGridViewTextBoxColumn PeakPowerdBm;
        private System.Windows.Forms.Button save_button;
        private System.Windows.Forms.Button scanOnce_button;
    }
}
