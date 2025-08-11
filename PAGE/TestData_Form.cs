using MetroFramework.Forms;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestApp.DAL;
using TestApp.MODEL;

namespace TestApp.PAGE
{
    public partial class TestData_Form : MetroForm
    {
        private Main_DAL main_DAL = new Main_DAL();
        private DataTable testData_DT = new DataTable();
        private List<MeasurementResult> testData_List = new List<MeasurementResult>();
        public TestData_Form()
        {
            InitializeComponent();
            this.Load += TestData_Form_Load;
        }

        private void TestData_Form_Load(object sender, EventArgs e)
        {
            testData_List = main_DAL.GetTestData_List();
            testData_DT = main_DAL.GetTestData_DT();
            dataGridView1.DataSource = testData_DT;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string ch = comboBox1.Text;
                string testType = comboBox2.Text;
                DateTime datePick = dateTimePicker1.Value.Date;
                bool enableDateFilter = checkBox1.Checked; // 是否启用时间筛选

                var filteredRows = testData_DT.AsEnumerable()
                    .Where(row =>
                    {
                        string updateTimeStr = row.Field<string>("UpdateTime");
                        DateTime updateTime;

                        bool parsed = DateTime.TryParseExact(
                            updateTimeStr,
                            "yyyy-MM-dd HH:mm:ss",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out updateTime);

                        bool matchDate = !enableDateFilter || (parsed && updateTime.Date == datePick);

                        return
                            (string.IsNullOrEmpty(ch) || row.Field<string>("TestType").Contains(ch)) &&
                            (string.IsNullOrEmpty(testType) || row.Field<string>("TestType").Contains(testType)) &&
                            (string.IsNullOrEmpty(textBox2.Text) || row.Field<string>("ComponentName").Contains(textBox2.Text)) &&
                            matchDate;
                    });

                DataTable filteredDataTable = filteredRows.Any() ? filteredRows.CopyToDataTable() : testData_DT.Clone();
                dataGridView1.DataSource = filteredDataTable;
            }
            catch
            {
                MessageBox.Show("请先查询所有数据，再筛选");
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xls",
                Title = "Save an Excel File"
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;
                ExportToExcel(filePath);
                MessageBox.Show("Excel 导出成功");
            }
        }

        /// <summary>
        /// 自定义表格内容
        /// </summary>
        /// <param name="filePath"></param>
        public void ExportToExcel(string filePath)
        {
            HSSFWorkbook workbook = new HSSFWorkbook(); // 创建 .xls 工作簿
            ISheet sheet = workbook.CreateSheet("测试数据");

            // 创建表头
            IRow headerRow = sheet.CreateRow(0);
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                headerRow.CreateCell(i).SetCellValue(dataGridView1.Columns[i].HeaderText);
            }

            // 写入数据
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                DataGridViewRow dgvRow = dataGridView1.Rows[i];

                // 排除新建行
                if (dgvRow.IsNewRow) continue;

                IRow row = sheet.CreateRow(i + 1);

                for (int j = 0; j < dataGridView1.Columns.Count; j++)
                {
                    object value = dgvRow.Cells[j].Value;
                    row.CreateCell(j).SetCellValue(value?.ToString() ?? "");
                }
            }

            // 设置列宽（按列名长度设置）
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                sheet.SetColumnWidth(i, 20 * 256); // 20 个字符宽
            }

            // 保存到文件
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            string[] decimalColumns = { "Gain", "InitialPhase", "InputSWR", "OutputSWR" };

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            if (decimalColumns.Contains(colName))
            {
                if (e.Value != null && double.TryParse(e.Value.ToString(), out double result))
                {
                    e.Value = result.ToString("F3"); // 显示三位小数
                    e.FormattingApplied = true;
                }
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
