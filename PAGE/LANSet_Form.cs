using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestApp.FUNCTION;
using TestApp.MODEL;

namespace TestApp
{
    public partial class LANSet_Form : Form
    {
        LanScanFunc lanScanFunc = new LanScanFunc();

        public LANSet_Form()
        {
            InitializeComponent();
            this.Load += LANSet_Form_Load;

            txtStartIP1.TextChanged += TxtIpPart_TextChanged;
            txtStartIP2.TextChanged += TxtIpPart_TextChanged;
            txtStartIP3.TextChanged += TxtIpPart_TextChanged;
            txtStartIP4.TextChanged += TxtIpPart_TextChanged;

            txtEndIP1.TextChanged += TxtIpPart_TextChanged;
            txtEndIP2.TextChanged += TxtIpPart_TextChanged;
            txtEndIP3.TextChanged += TxtIpPart_TextChanged;
            txtEndIP4.TextChanged += TxtIpPart_TextChanged;


        }

        private void TxtIpPart_TextChanged(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null) return;

            if (txt.Text.Length == 3)
            {
                switch (txt.Name)
                {
                    case "txtStartIP1": txtStartIP2.Focus(); break;
                    case "txtStartIP2": txtStartIP3.Focus(); break;
                    case "txtStartIP3": txtStartIP4.Focus(); break;
                    case "txtStartIP4": txtEndIP1.Focus(); break; // 输入完起始IP最后一段跳到终止IP第1段
                    case "txtEndIP1": txtEndIP2.Focus(); break;
                    case "txtEndIP2": txtEndIP3.Focus(); break;
                    case "txtEndIP3": txtEndIP4.Focus(); break;
                    case "txtEndIP4": nudPort.Focus(); break; // 终止IP最后一段跳到端口输入
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!ValidateIpPart(txtStartIP1.Text) || !ValidateIpPart(txtStartIP2.Text) ||
                !ValidateIpPart(txtStartIP3.Text) || !ValidateIpPart(txtStartIP4.Text) ||
                !ValidateIpPart(txtEndIP1.Text) || !ValidateIpPart(txtEndIP2.Text) ||
                !ValidateIpPart(txtEndIP3.Text) || !ValidateIpPart(txtEndIP4.Text))
            {
                MessageBox.Show("请输入有效的IP地址段（0-255）", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LanScanConfig config = new LanScanConfig()
            {
                StartIP = $"{txtStartIP1.Text}.{txtStartIP2.Text}.{txtStartIP3.Text}.{txtStartIP4.Text}",
                EndIP = $"{txtEndIP1.Text}.{txtEndIP2.Text}.{txtEndIP3.Text}.{txtEndIP4.Text}",
                Port = (int)nudPort.Value,
                Timeout = (int)nudTimeout.Value,
                MaxConcurrent = (int)nudMaxConcurrent.Value,
                QueryIdn = chkQueryIdn.Checked,
                AutoSave = chkAutoSave.Checked
            };

            lanScanFunc.SaveConfig(config);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        private bool ValidateIpPart(string s)
        {
            if (int.TryParse(s, out int val))
            {
                return val >= 0 && val <= 255;
            }
            return false;
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void LANSet_Form_Load(object sender, EventArgs e)
        {
            LanScanConfig config = lanScanFunc.LoadConfig();
            if (config != null)
            {
                string[] startIpParts = config.StartIP.Split('.');
                if (startIpParts.Length == 4)
                {
                    txtStartIP1.Text = startIpParts[0];
                    txtStartIP2.Text = startIpParts[1];
                    txtStartIP3.Text = startIpParts[2];
                    txtStartIP4.Text = startIpParts[3];
                }

                string[] endIpParts = config.EndIP.Split('.');
                if (endIpParts.Length == 4)
                {
                    txtEndIP1.Text = endIpParts[0];
                    txtEndIP2.Text = endIpParts[1];
                    txtEndIP3.Text = endIpParts[2];
                    txtEndIP4.Text = endIpParts[3];
                }

                nudPort.Value = config.Port;
                nudTimeout.Value = config.Timeout;
                nudMaxConcurrent.Value = config.MaxConcurrent;
                chkQueryIdn.Checked = config.QueryIdn;
                chkAutoSave.Checked = config.AutoSave;
            }
        }

    }
}
