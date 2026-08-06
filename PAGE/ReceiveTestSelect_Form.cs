using System;
using System.Drawing;
using System.Windows.Forms;

namespace DbfTest.PAGE
{
    /// <summary>
    /// 接收综合测试：勾选需要执行的指标项。
    /// </summary>
    public class ReceiveTestSelect_Form : Form
    {
        private readonly CheckBox _chkGain;
        private readonly CheckBox _chkImage;
        private readonly CheckBox _chkOutOfBand;
        private readonly CheckBox _chkBandwidth;
        private readonly CheckBox _chkFlatness;
        private readonly CheckBox _chkVswr;

        public bool TestGain => _chkGain.Checked;
        public bool TestImage => _chkImage.Checked;
        public bool TestOutOfBand => _chkOutOfBand.Checked;
        public bool TestBandwidth => _chkBandwidth.Checked;
        public bool TestFlatness => _chkFlatness.Checked;
        public bool TestVswr => _chkVswr.Checked;

        public ReceiveTestSelect_Form()
        {
            Text = "选择接收测试项目";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(320, 302);
            Font = new Font("Segoe UI", 10F);

            Controls.Add(new Label
            {
                Text = "请勾选本次需要测试的项目：",
                AutoSize = true,
                Location = new Point(20, 16)
            });

            _chkGain = CreateCheckBox("增益", 48);
            _chkImage = CreateCheckBox("镜频抑制", 80);
            _chkOutOfBand = CreateCheckBox("带外抑制", 112);
            _chkBandwidth = CreateCheckBox("-1dB带宽", 144);
            _chkFlatness = CreateCheckBox("平坦度", 176);
            _chkVswr = CreateCheckBox("驻波", 208);

            var btnSelectAll = new Button
            {
                Text = "全选",
                Size = new Size(60, 28),
                Location = new Point(20, 252)
            };
            btnSelectAll.Click += (s, e) => SetAll(true);

            var btnClear = new Button
            {
                Text = "全不选",
                Size = new Size(64, 28),
                Location = new Point(86, 252)
            };
            btnClear.Click += (s, e) => SetAll(false);

            var btnOk = new Button
            {
                Text = "开始测试",
                Size = new Size(80, 28),
                Location = new Point(160, 252)
            };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(60, 28),
                Location = new Point(246, 252),
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(_chkGain);
            Controls.Add(_chkImage);
            Controls.Add(_chkOutOfBand);
            Controls.Add(_chkBandwidth);
            Controls.Add(_chkFlatness);
            Controls.Add(_chkVswr);
            Controls.Add(btnSelectAll);
            Controls.Add(btnClear);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private static CheckBox CreateCheckBox(string text, int y)
        {
            return new CheckBox
            {
                Text = text,
                Checked = true,
                AutoSize = true,
                Location = new Point(36, y)
            };
        }

        private void SetAll(bool checkedState)
        {
            _chkGain.Checked = checkedState;
            _chkImage.Checked = checkedState;
            _chkOutOfBand.Checked = checkedState;
            _chkBandwidth.Checked = checkedState;
            _chkFlatness.Checked = checkedState;
            _chkVswr.Checked = checkedState;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!TestGain && !TestImage && !TestOutOfBand && !TestBandwidth && !TestFlatness && !TestVswr)
            {
                MessageBox.Show(this, "请至少勾选一项测试项目。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
