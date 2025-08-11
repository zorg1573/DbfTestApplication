namespace TestApp.ViewControl
{
    partial class ChargeControl
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.label1 = new System.Windows.Forms.Label();
            this.voltage_textBox = new System.Windows.Forms.TextBox();
            this.current_textBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ch_comboBox = new System.Windows.Forms.ComboBox();
            this.start_output_button = new System.Windows.Forms.Button();
            this.stop_output_button = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.manual_button = new System.Windows.Forms.Button();
            this.disconnect_button = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(36, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "输出电压：";
            // 
            // voltage_textBox
            // 
            this.voltage_textBox.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.voltage_textBox.Location = new System.Drawing.Point(135, 47);
            this.voltage_textBox.Name = "voltage_textBox";
            this.voltage_textBox.Size = new System.Drawing.Size(100, 23);
            this.voltage_textBox.TabIndex = 1;
            // 
            // current_textBox
            // 
            this.current_textBox.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.current_textBox.Location = new System.Drawing.Point(135, 87);
            this.current_textBox.Name = "current_textBox";
            this.current_textBox.Size = new System.Drawing.Size(100, 23);
            this.current_textBox.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(36, 91);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 19);
            this.label2.TabIndex = 2;
            this.label2.Text = "输出电流：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(36, 130);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 19);
            this.label3.TabIndex = 4;
            this.label3.Text = "通道：";
            // 
            // ch_comboBox
            // 
            this.ch_comboBox.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ch_comboBox.FormattingEnabled = true;
            this.ch_comboBox.Items.AddRange(new object[] {
            "CH1",
            "CH2",
            "CH3"});
            this.ch_comboBox.Location = new System.Drawing.Point(135, 130);
            this.ch_comboBox.Name = "ch_comboBox";
            this.ch_comboBox.Size = new System.Drawing.Size(100, 22);
            this.ch_comboBox.TabIndex = 5;
            // 
            // start_output_button
            // 
            this.start_output_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.start_output_button.Location = new System.Drawing.Point(40, 209);
            this.start_output_button.Name = "start_output_button";
            this.start_output_button.Size = new System.Drawing.Size(81, 39);
            this.start_output_button.TabIndex = 6;
            this.start_output_button.Text = "启动输出";
            this.start_output_button.UseVisualStyleBackColor = true;
            this.start_output_button.Click += new System.EventHandler(this.start_output_button_Click);
            // 
            // stop_output_button
            // 
            this.stop_output_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stop_output_button.Location = new System.Drawing.Point(154, 209);
            this.stop_output_button.Name = "stop_output_button";
            this.stop_output_button.Size = new System.Drawing.Size(81, 39);
            this.stop_output_button.TabIndex = 7;
            this.stop_output_button.Text = "停止输出";
            this.stop_output_button.UseVisualStyleBackColor = true;
            this.stop_output_button.Click += new System.EventHandler(this.stop_output_button_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 4);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 8;
            this.label4.Text = "设备名";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(4, 20);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 12);
            this.label5.TabIndex = 9;
            this.label5.Text = "未连接";
            // 
            // manual_button
            // 
            this.manual_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.manual_button.Location = new System.Drawing.Point(40, 307);
            this.manual_button.Name = "manual_button";
            this.manual_button.Size = new System.Drawing.Size(81, 39);
            this.manual_button.TabIndex = 10;
            this.manual_button.Text = "执行修改";
            this.manual_button.UseVisualStyleBackColor = true;
            this.manual_button.Click += new System.EventHandler(this.manual_button_Click);
            // 
            // disconnect_button
            // 
            this.disconnect_button.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.disconnect_button.Location = new System.Drawing.Point(154, 307);
            this.disconnect_button.Name = "disconnect_button";
            this.disconnect_button.Size = new System.Drawing.Size(81, 39);
            this.disconnect_button.TabIndex = 11;
            this.disconnect_button.Text = "断开连接";
            this.disconnect_button.UseVisualStyleBackColor = true;
            this.disconnect_button.Click += new System.EventHandler(this.disconnect_button_Click);
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(293, 15);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(564, 385);
            this.chart1.TabIndex = 12;
            this.chart1.Text = "chart1";
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            // 
            // ChargeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.disconnect_button);
            this.Controls.Add(this.manual_button);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.stop_output_button);
            this.Controls.Add(this.start_output_button);
            this.Controls.Add(this.ch_comboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.current_textBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.voltage_textBox);
            this.Controls.Add(this.label1);
            this.Name = "ChargeControl";
            this.Size = new System.Drawing.Size(886, 444);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox voltage_textBox;
        private System.Windows.Forms.TextBox current_textBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox ch_comboBox;
        private System.Windows.Forms.Button start_output_button;
        private System.Windows.Forms.Button stop_output_button;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button manual_button;
        private System.Windows.Forms.Button disconnect_button;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.Timer timer1;
    }
}
