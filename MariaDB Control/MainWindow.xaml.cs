using System;
using System.Collections.Generic;
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
using System.ServiceProcess;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MariaDBApp
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        private ManagementObject manObj;
        private ManagementObjectSearcher manObjSearch;
        private ManagementPath path;
        private ManagementBaseObject manBaseObj;
        private Thread statusUpdateThread, externalProg;
        private string MariaDBPath;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key,
            string defaultValue, StringBuilder value, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string section, string key, string defaultValue,
            [In, Out] char[] value, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileSection(string section, IntPtr keyValue,
            int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string section, string key,
            string value, string filePath);
        public static int capacity = 512;

        public MainWindow()
        {

            InitializeComponent();
            if(MariaDBControl.Properties.Settings.Default.MariaDBPath.Equals("") ||
                MariaDBControl.Properties.Settings.Default.MariaDBPath == null)
            {
                SetMariaDBPath();
                MariaDBControl.Properties.Settings.Default.MariaDBPath = txtPath.Text;
                MariaDBControl.Properties.Settings.Default.Save();
                txtPath.Focus();
            }
            MariaDBPath = MariaDBControl.Properties.Settings.Default.MariaDBPath;
            txtPath.Text = MariaDBPath;
            SetPortLabel();
            RetrieveServiceStatus();
            externalProg = new Thread(CheckDBVersion);
            externalProg.IsBackground = true;
            externalProg.Start();
            statusUpdateThread = new Thread(UpdateLabelStatus);
            statusUpdateThread.IsBackground = true;
            statusUpdateThread.Start();
        }

        #region INI

        public static string ReadValue(string section, string key, string filePath, string defaultValue = "")
        {
            var value = new StringBuilder(capacity);
            GetPrivateProfileString(section, key, defaultValue, value, value.Capacity, filePath);
            return value.ToString();
        }

        public static string[] ReadSections(string filePath)
        {
            // first line will not recognize if ini file is saved in UTF-8 with BOM 
            while (true)
            {
                char[] chars = new char[capacity];
                int size = GetPrivateProfileString(null, null, "", chars, capacity, filePath);

                if (size == 0)
                {
                    return null;
                }

                if (size < capacity - 2)
                {
                    string result = new String(chars, 0, size);
                    string[] sections = result.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    return sections;
                }

                capacity = capacity * 2;
            }
        }

        public static string[] ReadKeys(string section, string filePath)
        {
            // first line will not recognize if ini file is saved in UTF-8 with BOM 
            while (true)
            {
                char[] chars = new char[capacity];
                int size = GetPrivateProfileString(section, null, "", chars, capacity, filePath);

                if (size == 0)
                {
                    return null;
                }

                if (size < capacity - 2)
                {
                    string result = new String(chars, 0, size);
                    string[] keys = result.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    return keys;
                }

                capacity = capacity * 2;
            }
        }

        public static string[] ReadKeyValuePairs(string section, string filePath)
        {
            while (true)
            {
                IntPtr returnedString = Marshal.AllocCoTaskMem(capacity * sizeof(char));
                int size = GetPrivateProfileSection(section, returnedString, capacity, filePath);

                if (size == 0)
                {
                    Marshal.FreeCoTaskMem(returnedString);
                    return null;
                }

                if (size < capacity - 2)
                {
                    string result = Marshal.PtrToStringAuto(returnedString, size - 1);
                    Marshal.FreeCoTaskMem(returnedString);
                    string[] keyValuePairs = result.Split('\0');
                    return keyValuePairs;
                }

                Marshal.FreeCoTaskMem(returnedString);
                capacity = capacity * 2;
            }
        }
        #endregion


        #region Logic

        private void CheckDBVersion()
        {
            try
            {
                Process p = new Process();
                
                p.StartInfo.FileName = MariaDBPath + "\\bin\\mysqld.exe";
                p.StartInfo.Arguments = "-V";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                Regex regex = new Regex(@"[0-9]+\.[0-9]+\.[0-9]+");
                Match match = regex.Match(output);
                if (match.Success)
                {
                    lbl_versao_show.Dispatcher.BeginInvoke(new Action(() => lbl_versao_show.Content = match.Value));
                }
                else
                {
                    lbl_versao_show.Dispatcher.BeginInvoke(new Action(() => lbl_versao_show.Content = "Error checking version"));
                }
            } catch (Exception e)
            {
                MessageBox.Show("Couldn't find \\bin\\mysqld.exe");
                return;
            }


            
        }

        private void UpdateLabelStatus()
        {
            
            while (true)
            {
                manObj = new ManagementObject(path);
                switch (manObj.GetPropertyValue("State").ToString())
                {
                    case "Running":
                        lbl_status_show.Dispatcher.BeginInvoke(new Action(() => lbl_status_show.Foreground = Brushes.Green));
                        break;
                    case "Stopped":
                        lbl_status_show.Dispatcher.BeginInvoke(new Action(() => lbl_status_show.Foreground = Brushes.Red));
                        break;
                    default:
                        lbl_status_show.Dispatcher.BeginInvoke(new Action(() => lbl_status_show.Foreground = Brushes.White));
                        break;
                }
                lbl_status_show.Dispatcher.BeginInvoke(new Action(() => lbl_status_show.Content = manObj.GetPropertyValue("State").ToString()));
                Thread.Sleep(1000);
            }

        }

        private void RetrieveServiceStatus()
        {
            try
            {
                //manObjSearch = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE NAME = 'MySQL'");
                //if (manObjSearch.Get() != null)
                //{
                //    var queryCollection = from ManagementObject x in manObjSearch.Get()
                //                          select x;
                //    manObj = queryCollection.FirstOrDefault();
                //}

                path = new ManagementPath
                {
                    Server = System.Environment.MachineName,
                    NamespacePath = @"root\CIMV2",
                    RelativePath = "Win32_service.Name='MySQL'"
                };

                manObj = new ManagementObject(path);

                
            }
            catch (Exception e)
            {
                return;
            }
        }

        private void SetPortLabel()
        {
            try
            {
                lbl_porta_show.Content = ReadValue("mysqld", "port", txtPath.Text + "\\data\\my.ini", "");
            }
            catch (Exception e)
            {
                MessageBox.Show("Couldn't find \\data\\my.ini");
                return;
            }
        }

        private void SetMariaDBPath()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Path to MariaDB Installation";
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    MariaDBPath = dialog.SelectedPath;
                    txtPath.Text = MariaDBPath;
                }
            }
        }

        #endregion

        private void Btn_iniciar_Click(object sender, RoutedEventArgs e)
        {
            manBaseObj = manObj.InvokeMethod("StartService", null, null);       
        }

        private void Btn_parar_Click(object sender, RoutedEventArgs e)
        {
            manBaseObj = manObj.InvokeMethod("StopService", null, null);
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            MariaDBControl.Properties.Settings.Default.MariaDBPath = txtPath.Text;
            MariaDBControl.Properties.Settings.Default.Save();
        }

        private void btn_open_Click(object sender, RoutedEventArgs e)
        {
            SetMariaDBPath();
            lbl_versao_show.Content = "";
            SetPortLabel();
            CheckDBVersion();
        }

        private void btn_reload_Click(object sender, RoutedEventArgs e)
        {
            lbl_versao_show.Content = "";
            SetPortLabel();
            CheckDBVersion();
        }

        private void Btn_reiniciar_Click(object sender, RoutedEventArgs e)
        {
            manBaseObj = manObj.InvokeMethod("StopService", null, null);
            manBaseObj = manObj.InvokeMethod("StartService", null, null);

        }

    }
}
