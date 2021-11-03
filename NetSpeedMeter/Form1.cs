using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace NetSpeedMeter
{
    public partial class Form1 : Form
    {
        // Selected network interface index
        int selIndex = 0;
        // Update interval
        int interval = 1000;

        long BytesSent = 0;
        long BytesReceived = 0;

        NetworkInterface[] nicArr;
        NetworkInterface nic;
        List<int> goodAdapters = new List<int>();

        // Session usage
        double dataSent = 0;
        double dataReceived = 0;
        // Current speed
        double upSpeed = 0;
        double downSpeed = 0;

        // Current speed units
        string upUnit;
        string downUnit;
        // Session usage unit
        string sentUnit;
        string receiveUnit;

        // Current window state
        string windowState;

        public Form1()
        {
            InitializeComponent();
            NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == SingleInstance.WM_SHOWFIRSTINSTANCE)
            {
                ShowWindow();
            }
            base.WndProc(ref message);
        }

        public void ShowWindow()
        {
            winAPI.ShowToFront(this.Handle);
        }

        private void InitNetworkInterface()
        {
            goodAdapters.Clear();
            nicArr = NetworkInterface.GetAllNetworkInterfaces();

            for (int i = 0; i < nicArr.Length; i++)
            {
                if (nicArr[i].SupportsMulticast && nicArr[i].GetIPv4Statistics().UnicastPacketsReceived >= 1 && nicArr[i].OperationalStatus.ToString() == "Up")
                {
                    goodAdapters.Add(i);
                }
            }
        }

        private void InitNetworkCombo()
        {
            if (goodAdapters.Count != comboBox1.Items.Count && goodAdapters.Count != 0)
            {
                comboBox1.Items.Clear();
                foreach (int gadpt in goodAdapters)
                {
                    comboBox1.Items.Add(nicArr[gadpt].Name);
                }
                selIndex = goodAdapters[0];
                comboBox1.SelectedIndex = 0;
            }
            if (goodAdapters.Count == 0)
            {
                comboBox1.Items.Clear();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            bool err = false;
            while (!backgroundWorker1.CancellationPending)
            {
                try
                {
                    IPv4InterfaceStatistics interfaceStats = nic.GetIPv4Statistics();
                    err = false;
                    long uBytesSent = interfaceStats.BytesSent;
                    long uBytesReceived = interfaceStats.BytesReceived;
                    long sentSpeed = uBytesSent - BytesSent;
                    long receiveSpeed = uBytesReceived - BytesReceived;

                    BytesSent = uBytesSent;
                    BytesReceived = uBytesReceived;

                    // Upload Speed
                    if (sentSpeed > 1024 && sentSpeed < 1048576)
                    {
                        upSpeed = sentSpeed / 1024.0;
                        upUnit = " KB/s";
                    }
                    else if (sentSpeed > 1048576)
                    {
                        upSpeed = sentSpeed / 1048576.0;
                        upUnit = " MB/s";
                    }
                    else
                    {
                        upSpeed = sentSpeed;
                        upUnit = " B/s";
                    }

                    // Download Speed
                    if (receiveSpeed > 1024 && receiveSpeed < 1048576)
                    {
                        downSpeed = receiveSpeed / 1024.0;
                        downUnit = " KB/s";
                    }
                    else if (receiveSpeed > 1048576)
                    {
                        downSpeed = receiveSpeed / 1048576.0;
                        downUnit = " MB/s";
                    }
                    else
                    {
                        downSpeed = receiveSpeed;
                        downUnit = " B/s";
                    }

                    // Session Upload
                    if (uBytesSent > 1024 && uBytesSent < 1048576)
                    {
                        dataSent = uBytesSent / 1024.0;
                        sentUnit = " KB";
                    }
                    else if (uBytesSent > 1048576 && uBytesSent < 1073741824)
                    {
                        dataSent = uBytesSent / 1048576.0;
                        sentUnit = " MB";
                    }
                    else if (uBytesSent > 1073741824)
                    {
                        dataSent = uBytesSent / 1073741824.0;
                        sentUnit = " GB";
                    }
                    else
                    {
                        dataSent = uBytesSent;
                        sentUnit = " B";
                    }

                    // Session Download
                    if (uBytesReceived > 1024 && uBytesReceived < 1048576)
                    {
                        dataReceived = uBytesReceived / 1024.0;
                        receiveUnit = " KB";
                    }
                    else if (uBytesReceived > 1048576 && uBytesReceived < 1073741824)
                    {
                        dataReceived = uBytesReceived / 1048576.0;
                        receiveUnit = " MB";
                    }
                    else if (uBytesReceived > 1073741824)
                    {
                        dataReceived = uBytesReceived / 1073741824.0;
                        receiveUnit = " GB";
                    }
                    else
                    {
                        dataReceived = uBytesReceived;
                        receiveUnit = " B";
                    }

                    new ToastContentBuilder()
                        .AddText("Up: " + upSpeed.ToString("0.00") + upUnit + " Dl: " + downSpeed.ToString("0.00") + downUnit)
                        .AddText("Up: " + dataSent.ToString("0.00") + sentUnit + " Dl: " + dataReceived.ToString("0.00") + receiveUnit)
                        .Show(toast =>
                        {
                            toast.Tag = "1252";
                            toast.Group = "Main";
                            toast.SuppressPopup = true;
                        });

                    if (windowState == "Normal")
                    {
                        backgroundWorker1.ReportProgress(1);
                    }
                }
                catch (Exception ex)
                {
                    if (!err)
                    {
                        new ToastContentBuilder()
                        .AddText("Network adapter is not operational")
                        .AddText(ex.Message)
                        .SetToastDuration(ToastDuration.Short)
                        .Show(toast =>
                        {
                            toast.Tag = "1253";
                            toast.Group = "Other";
                            toast.SuppressPopup = false;
                        });
                        err = true;
                    }
                }

                System.Threading.Thread.Sleep(interval);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            upSpeedLabel.Text = upSpeed.ToString("0.00") + " " + upUnit;
            downSpeedLabel.Text = downSpeed.ToString("0.00") + " " + downUnit;
            upDataLabel.Text = dataSent.ToString("0.00") + " " + sentUnit;
            downDataLabel.Text = dataReceived.ToString("0.00") + " " + receiveUnit;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            windowState = "Normal";
            InitNetworkInterface();
            InitNetworkCombo();
            if (goodAdapters.Count > 0)
            {
                nic = nicArr[selIndex];
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void AvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                InitNetworkInterface();
                if (windowState == "Normal")
                {
                    InitNetworkCombo();
                }
                if (goodAdapters.Count > 0)
                {
                    nic = nicArr[selIndex];
                    backgroundWorker1.RunWorkerAsync();
                    new ToastContentBuilder()
                        .AddText("Network connection available")
                        .SetToastDuration(ToastDuration.Short)
                        .Show(toast =>
                        {
                            toast.Tag = "1254";
                            toast.Group = "Other";
                            toast.SuppressPopup = false;
                        });
                }
            }
            else
            {
                backgroundWorker1.CancelAsync();
                new ToastContentBuilder()
                        .AddText("Network connection lost")
                        .SetToastDuration(ToastDuration.Short)
                        .Show(toast =>
                        {
                            toast.Tag = "1255";
                            toast.Group = "Other";
                            toast.SuppressPopup = false;
                        });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundWorker1.CancelAsync();
            ToastNotificationManagerCompat.History.Clear();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                windowState = "Minimized";
                Hide();
            }
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            refreshButton.Enabled = false;
            comboBox1.Enabled = false;
            InitNetworkInterface();
            InitNetworkCombo();
            comboBox1.Enabled = true;
            refreshButton.Enabled = true;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Show();
                windowState = "Normal";
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selIndex = goodAdapters[comboBox1.SelectedIndex];
            nic = nicArr[selIndex];
        }
    }
}
