using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestApp.PAGE
{
    public partial class RecieveTestWait_Form : Form
    {
        public RecieveTestWait_Form()
        {
            InitializeComponent();
        }
        public void ChangeLabelText(string theLabel, string newText)
        {
            // 在当前窗体中查找匹配名称的 Label 控件
            Control[] controls = this.Controls.Find(theLabel, true); // true 表示递归查找所有子容器

            if (controls.Length > 0 && controls[0] is Label label)
            {
                label.Text = newText;
            }
            else
            {
                MessageBox.Show($"未找到名为 \"{theLabel}\" 的 Label 控件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

    }
}
