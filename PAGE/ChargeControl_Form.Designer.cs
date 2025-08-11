namespace TestApp.PAGE
{
    partial class ChargeControl_Form
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChargeControl_Form));
            this.manual_button = new System.Windows.Forms.Button();
            this.stop_output_button = new System.Windows.Forms.Button();
            this.start_output_button = new System.Windows.Forms.Button();
            this.ch_comboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.current_textBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.voltage_textBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
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
            // manual_button
            // 
            this.manual_button.BackColor = System.Drawing.Color.White;
            this.manual_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.manual_button.Location = new System.Drawing.Point(281, 177);
            this.manual_button.Name = "manual_button";
            this.manual_button.Size = new System.Drawing.Size(81, 39);
            this.manual_button.TabIndex = 23;
            this.manual_button.Text = "执行修改";
            this.manual_button.UseVisualStyleBackColor = false;
            this.manual_button.Click += new System.EventHandler(this.manual_button_Click);
            // 
            // stop_output_button
            // 
            this.stop_output_button.BackColor = System.Drawing.Color.White;
            this.stop_output_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stop_output_button.Location = new System.Drawing.Point(281, 130);
            this.stop_output_button.Name = "stop_output_button";
            this.stop_output_button.Size = new System.Drawing.Size(81, 39);
            this.stop_output_button.TabIndex = 20;
            this.stop_output_button.Text = "停止输出";
            this.stop_output_button.UseVisualStyleBackColor = false;
            this.stop_output_button.Click += new System.EventHandler(this.stop_output_button_Click);
            // 
            // start_output_button
            // 
            this.start_output_button.BackColor = System.Drawing.Color.White;
            this.start_output_button.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.start_output_button.Location = new System.Drawing.Point(281, 85);
            this.start_output_button.Name = "start_output_button";
            this.start_output_button.Size = new System.Drawing.Size(81, 39);
            this.start_output_button.TabIndex = 19;
            this.start_output_button.Text = "启动输出";
            this.start_output_button.UseVisualStyleBackColor = false;
            this.start_output_button.Click += new System.EventHandler(this.start_output_button_Click);
            // 
            // ch_comboBox
            // 
            this.ch_comboBox.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ch_comboBox.FormattingEnabled = true;
            this.ch_comboBox.Items.AddRange(new object[] {
            "CH1",
            "CH2",
            "CH3"});
            this.ch_comboBox.Location = new System.Drawing.Point(134, 177);
            this.ch_comboBox.Name = "ch_comboBox";
            this.ch_comboBox.Size = new System.Drawing.Size(100, 28);
            this.ch_comboBox.TabIndex = 18;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(35, 176);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 25);
            this.label3.TabIndex = 17;
            this.label3.Text = "通道：";
            // 
            // current_textBox
            // 
            this.current_textBox.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.current_textBox.Location = new System.Drawing.Point(134, 134);
            this.current_textBox.Name = "current_textBox";
            this.current_textBox.Size = new System.Drawing.Size(100, 26);
            this.current_textBox.TabIndex = 16;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(35, 134);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 25);
            this.label2.TabIndex = 15;
            this.label2.Text = "输出电流：";
            // 
            // voltage_textBox
            // 
            this.voltage_textBox.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.voltage_textBox.Location = new System.Drawing.Point(134, 94);
            this.voltage_textBox.Name = "voltage_textBox";
            this.voltage_textBox.Size = new System.Drawing.Size(100, 26);
            this.voltage_textBox.TabIndex = 14;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(35, 95);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 25);
            this.label1.TabIndex = 13;
            this.label1.Text = "输出电压：";
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.pictureBox1);
            this.flowLayoutPanel1.Controls.Add(this.pictureBox2);
            this.flowLayoutPanel1.Controls.Add(this.pictureBox3);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(262, 4);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(128, 32);
            this.flowLayoutPanel1.TabIndex = 24;
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
            // ChargeControl_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(390, 243);
            this.ControlBox = false;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.manual_button);
            this.Controls.Add(this.stop_output_button);
            this.Controls.Add(this.start_output_button);
            this.Controls.Add(this.ch_comboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.current_textBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.voltage_textBox);
            this.Controls.Add(this.label1);
            this.Name = "ChargeControl_Form";
            this.Text = "电源控制";
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button manual_button;
        private System.Windows.Forms.Button stop_output_button;
        private System.Windows.Forms.Button start_output_button;
        private System.Windows.Forms.ComboBox ch_comboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox current_textBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox voltage_textBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
    }
}