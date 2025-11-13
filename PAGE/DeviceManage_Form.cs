using Ivi.Visa;
using NationalInstruments.Visa;
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

namespace DbfTest.PAGE
{
    public partial class DeviceManage_Form : Form
    {
        private Main_Form mainForm;
        private string shiwangAddress = "";
        private string gonglvAddress = "";
        private string xinhaoAddress = "";
        private string zaoshengAddress = "";
        private string pinpuAddress = "";
        private string kaiguanAddress = "";
        private string hanshuAddress = "";
        private string v6Address = "";
        private string v65Address = "";
        private string v28Address = "";
        private string maichongAddress = "";
        private string v2Address = "";
        private string v18Address = "";
        private string shiboAddress = "";
        public DeviceManage_Form()
        {
            InitializeComponent();
            this.Load += DeviceManage_Form_Load;
        }
        public DeviceManage_Form(Main_Form mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
            this.Load += DeviceManage_Form_Load;
        }
        private void DeviceManage_Form_Load(object sender, EventArgs e)
        {
            LoadFromJson();
        }
        private void LoadFromJson()
        {
            string filePath = "DeviceAddress.json";
            if (!File.Exists(filePath))
                return;

            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (data == null)
                return;
            shiwangAddress = data.ContainsKey("shiwang_textBox") ? data["shiwang_textBox"].ToString() : "";
            gonglvAddress = data.ContainsKey("gonglv_textBox") ? data["gonglv_textBox"].ToString() : "";
            xinhaoAddress = data.ContainsKey("xinhao_textBox") ? data["xinhao_textBox"].ToString() : "";
            zaoshengAddress = data.ContainsKey("zaosheng_textBox") ? data["zaosheng_textBox"].ToString() : "";
            pinpuAddress = data.ContainsKey("pinpu_textBox") ? data["pinpu_textBox"].ToString() : "";
            kaiguanAddress = data.ContainsKey("kaiguan_textBox") ? data["kaiguan_textBox"].ToString() : "";
            hanshuAddress = data.ContainsKey("hanshu_textBox") ? data["hanshu_textBox"].ToString() : "";
            v6Address = data.ContainsKey("v6_textBox") ? data["v6_textBox"].ToString() : "";
            v65Address = data.ContainsKey("v65_textBox") ? data["v65_textBox"].ToString() : "";
            v28Address = data.ContainsKey("v28_textBox") ? data["v28_textBox"].ToString() : "";
            maichongAddress = data.ContainsKey("maichong_textBox") ? data["maichong_textBox"].ToString() : "";
            v2Address = data.ContainsKey("v2_textBox") ? data["v2_textBox"].ToString() : "";
            v18Address = data.ContainsKey("v18_textBox") ? data["v18_textBox"].ToString() : "";
            shiboAddress = data.ContainsKey("shibo_textBox") ? data["shibo_textBox"].ToString() : "";

        }
        private Dictionary<string, string> GetAllDeviceAddresses()
        {
            return new Dictionary<string, string>
            {
                { "shiwang", shiwangAddress },
                { "gonglv", gonglvAddress },
                { "xinhao", xinhaoAddress },
                { "zaosheng", zaoshengAddress },
                { "pinpu", pinpuAddress },
                { "kaiguan", kaiguanAddress },
                { "hanshu", hanshuAddress },
                { "v6", v6Address },
                { "v65", v65Address },
                { "v28", v28Address },
                { "maichong", maichongAddress },
                { "v2", v2Address },
                { "v18", v18Address },
                { "shibo", shiboAddress }
            };
        }

        private async void connect_button_Click(object sender, EventArgs e)
        {
            var devices = GetAllDeviceAddresses();
            dataGridView1.Rows.Clear();

            var tasks = devices
                .Where(d => !string.IsNullOrWhiteSpace(d.Value))
                .Select(async kvp =>
                {
                    mainForm.LogToConsole($"正在连接设备 {kvp.Key}...");
                    bool connected = await TryConnectToDevice(kvp.Value);
                    mainForm.LogToConsole($"{kvp.Key} {(connected ? "连接成功" : "连接失败")}");

                    dataGridView1.Invoke(new Action(() =>
                    {
                        dataGridView1.Rows.Add(kvp.Key, kvp.Value, connected ? "Connected" : "Failed");
                    }));
                });

            await Task.WhenAll(tasks);
        }

        private Task<bool> TryConnectToDevice(string visaAddress)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var resourceManager = new ResourceManager())
                    using (var session = resourceManager.Open(visaAddress) as IMessageBasedSession)
                    {
                        if (session == null)
                            return false;

                        session.TimeoutMilliseconds = 2000;
                        session.FormattedIO.WriteLine("*IDN?");
                        string response = session.FormattedIO.ReadLine();

                        Console.WriteLine($"{visaAddress} → {response}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to {visaAddress}: {ex.Message}");
                    return false;
                }
            });
        }


    }
}
