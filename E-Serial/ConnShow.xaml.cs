﻿using E_Serial.Core;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace E_Serial
{
    /// <summary>
    /// ConnShow.xaml 的交互逻辑
    /// </summary>
    public partial class ConnShow : UserControl
    {
        private bool isRun;
        private IConnCore icc;
        private App app;
        private FileStream rFS;
        private string rFPath;
        private bool isNewLine = true;

        public ConnShow(IConnCore icc)
        {
            InitializeComponent();
            this.icc = icc;
            isRun = false;
            app = (App)Application.Current;
            rFS = null;
            rFPath = string.Empty;
            IsPause = false;
            this.DataContext = icc;
        }

        public IConnCore Icc
        {
            get
            {
                return icc;
            }

            private set
            {
                icc = value;
            }
        }

        public string RFPath
        {
            get { return this.rFPath; }
        }

        public bool IsPause { get; set; }

        public void ClearRFPath()
        {
            try
            {
                File.Delete(this.rFPath);
            }
            catch { }
            this.rFPath = string.Empty;
        }

        public void StartLoad()
        {
            if (!isRun)
            {
                icc.DataReceived += (object sendor, Core.DataReceivedEventArgs ea) =>
                {
                    try
                    {
                        this.txt_Data.Dispatcher.Invoke(() =>
                        {
                            if (app.AutoClear)
                                if (this.txt_Data.LineCount >= app.AutoClearLines)
                                {
                                    string s = this.txt_Data.Text.Substring(this.txt_Data.Text.Length / 2);
                                    txt_Data.Clear();
                                    txt_Data.Text = s;
                                }
                            if (!IsPause)
                            {
                                if (app.Timestamp && isNewLine)
                                {
                                    this.txt_Data.AppendText(string.Format("[{0}] ", DateTime.Now.ToString("MM/dd HH:mm:ss.ffff")));
                                }
                                this.txt_Data.AppendText(ea.Data);
                                isNewLine = ea.isNewLine;
                                if (app.AutoScroll)
                                    this.txt_Data.ScrollToEnd();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                };
                icc.Open();
                isRun = true;
                this.txt_Write.DataContext = this.Icc;
            }
        }

        public bool RStart()
        {
            if (this.rFS == null && this.rFPath == string.Empty && isRun)
            {
                rFPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, app.Tmp, Guid.NewGuid().ToString() + ".tmp");
                rFS = File.Create(rFPath);
                icc.DataReceived += rDataReceived;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RStop()
        {
            if (this.rFS != null && this.rFPath != string.Empty && isRun)
            {
                icc.DataReceived -= rDataReceived;
                rFS.Close();
                rFS = null;
                return true;
            }
            else
                return false;
        }

        private async void rDataReceived(object sendor, Core.DataReceivedEventArgs ea)
        {
            byte[] buf = Encoding.UTF8.GetBytes(ea.Data);
            try { await rFS.WriteAsync(buf, 0, buf.Length); }
            catch { }
        }

        private void txt_Write_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox t = sender as TextBox;
            if (e.Key == Key.Enter)
            {
                icc.Write(t.Text);
                t.Clear();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.StartLoad();
            this.txt_Write.Focus();
        }

        private void txt_Data_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MainWindow mw = (MainWindow)this.app.MainWindow;
            this.IsPause = !this.IsPause;
            if (IsPause)
            {
                mw.FlyoutPauseContent = "PAUSE!";
                mw.FlyoutPauseAutoClose = false;
            }
            else
            {
                mw.FlyoutPauseContent = "Restart!";
                mw.FlyoutPauseAutoClose = true;
            }
            mw.FlyoutPauseShow = true;
        }
    }
}
