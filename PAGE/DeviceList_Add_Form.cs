using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DbfTest.MODEL;

namespace DbfTest
{
    public partial class DeviceList_Add_Form : Form
    {
        public DeviceInfo Device { get; private set; }

        public DeviceList_Add_Form(DeviceInfo existing = null)
        {
            InitializeComponent();
            InitConnectionTypes();
            InitSerialDefaults();

            if (existing != null)
                LoadDeviceInfo(existing);
        }

        private void InitConnectionTypes()
        {
            cmbConnectionType.Items.AddRange(Enum.GetNames(typeof(ConnectionType)));
            cmbConnectionType.SelectedIndexChanged += (s, e) =>
            {
                var selected = (ConnectionType)Enum.Parse(typeof(ConnectionType), cmbConnectionType.Text);
                panelSerialParams.Visible = selected == ConnectionType.Serial || selected == ConnectionType.Bluetooth;
                if(selected == ConnectionType.Serial || selected == ConnectionType.Bluetooth)
                {
                    label3.Text = "串口号：";
                    label4.Visible = true;
                    label5.Visible = true;
                    label6.Visible = true;
                    label7.Visible = true;
                }
                else
                {
                    label3.Text = "设备地址：";
                    label4.Visible = false;
                    label5.Visible = false;
                    label6.Visible = false;
                    label7.Visible = false;
                }
            };
        }

        private void InitSerialDefaults()
        {
            cmbBaudRate.Items.AddRange(new[] { "9600", "19200", "38400", "57600", "115200" });
            cmbDataBits.Items.AddRange(new[] { "5", "6", "7", "8" });
            cmbParity.Items.AddRange(Enum.GetNames(typeof(Parity)));
            cmbStopBits.Items.AddRange(Enum.GetNames(typeof(StopBits)));

            cmbBaudRate.SelectedIndex = 0;
            cmbDataBits.SelectedIndex = 1;
            cmbParity.SelectedIndex = 0;
            cmbStopBits.SelectedIndex = 1;
        }

        private void LoadDeviceInfo(DeviceInfo info)
        {
            txtDeviceName.Text = info.DeviceName;
            cmbConnectionType.SelectedItem = info.Type.ToString();
            txtAddress.Text = info.Address;

            if (info.Parameters.TryGetValue("BaudRate", out var baud)) cmbBaudRate.Text = baud;
            if (info.Parameters.TryGetValue("DataBits", out var dataBits)) cmbDataBits.Text = dataBits;
            if (info.Parameters.TryGetValue("Parity", out var parity)) cmbParity.Text = parity;
            if (info.Parameters.TryGetValue("StopBits", out var stopBits)) cmbStopBits.Text = stopBits;
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            var dev = new DeviceInfo
            {
                DeviceName = txtDeviceName.Text.Trim(),
                Address = txtAddress.Text.Trim(),
                Type = (ConnectionType)Enum.Parse(typeof(ConnectionType), cmbConnectionType.Text),
            };

            if (dev.Type == ConnectionType.Serial || dev.Type == ConnectionType.Bluetooth)
            {
                dev.Parameters["BaudRate"] = cmbBaudRate.Text;
                dev.Parameters["DataBits"] = cmbDataBits.Text;
                dev.Parameters["Parity"] = cmbParity.Text;
                dev.Parameters["StopBits"] = cmbStopBits.Text;
            }

            Device = dev;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

}
