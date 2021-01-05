//*********************************************************  
//  
// Copyright (c) Microsoft. All rights reserved.  
// This code is licensed under the MIT License (MIT).  
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY  
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR  
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.  
//  
//********************************************************* 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Utf8Json;
using System.Drawing;

namespace Taskman
{
    class SystrayApplicationContext : ApplicationContext
    {

        private AppServiceConnection connection = null;
        private NotifyIcon notifyIcon = null;
        private Form1 configWindow = new Form1();
        public Process[] processes;

        public string LocalFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\";

        public SystrayApplicationContext()
        {
            MenuItem openMenuItem = new MenuItem("Open UWP", new EventHandler(OpenApp));
            MenuItem sendMenuItem = new MenuItem("Send message to UWP", new EventHandler(SendMessageToUWP));
            MenuItem legacyMenuItem = new MenuItem("Open legacy companion", new EventHandler(OpenLegacy));
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));
            openMenuItem.DefaultItem = true;

            notifyIcon = new NotifyIcon();
            notifyIcon.Click += new EventHandler(OpenApp);
            notifyIcon.Icon = Taskman.Properties.Resources.Icon1;
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[]{ openMenuItem, sendMenuItem, legacyMenuItem, exitMenuItem });
            notifyIcon.Visible = true;

            /*ValueSet request = new ValueSet();
            request.Add("KEY", "key");
            SendToUWP(request);*/

            SendProcessList();


        }


        [DllImport("Kernel32.dll")]
        static extern uint QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder text, out uint size);

        //Get the path to a process
        //proc = the process desired
        private string GetPathToApp(Process proc)
        {
            
            string pathToExe = string.Empty;
            try
            {
                if (null != proc)
                {
                    uint nChars = 256;
                    StringBuilder Buff = new StringBuilder((int)nChars);

                    uint success = QueryFullProcessImageName(proc.Handle, 0, Buff, out nChars);

                    if (0 != success)
                    {
                        pathToExe = Buff.ToString();
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        pathToExe = ("Error = " + error + " when calling GetProcessImageFileName");
                    }
                }
            }
            catch { }

            return pathToExe;
        }

        private async void OpenApp(object sender, EventArgs e)
        {
            
            /*string Filename = $"{LocalFolderPath}firstboot.txt";
            File.WriteAllText(Filename, "1");*/

            IEnumerable<AppListEntry> appListEntries = await Package.Current.GetAppListEntriesAsync();
            await appListEntries.First().LaunchAsync();
            await SendProcessList();
        }

        private async Task SendProcessList()
		{
            processes = Process.GetProcesses();
            string process_list = "";

            ValueSet message = new ValueSet();
            message.Add("clear", "clear");
            await SendToUWP(message);

            List<string> Process_Filename_List = new List<string>();

            foreach (Process process in processes)
            {

                string path = GetPathToApp(process);
                
                if (path.Length > 0)
                {
                    //MessageBox.Show(path);

                    Icon icon = Taskman.Properties.Resources.Icon1;
                    try
                    {
                        icon = Icon.ExtractAssociatedIcon(path);
                        int startPos = path.LastIndexOf('\\') + 1;
                        string iconFile = path.Substring(startPos, path.Length - startPos);
                        icon.ToBitmap().Save($"{LocalFolderPath}{iconFile}.png");
                    }
                    catch { }


                    //MessageBox.Show(icon.Handle.ToString());
                    message = new ValueSet();
                    message.Add("content", path);
                    await SendToUWP(message);
                }
            }

        }

        private async void SendMessageToUWP(object sender, EventArgs e)
        {
            await SendProcessList();
        }

        private void OpenLegacy(object sender, EventArgs e)
        {
            Form1 form = new Form1();
            form.Show();
        }

        private async void Exit(object sender, EventArgs e)
        {
            ValueSet message = new ValueSet();
            message.Add("exit", "");
            await SendToUWP(message);
            Application.Exit();
        }

        private async Task SendToUWP(ValueSet message)
        { 
            if (connection == null)
            {
                connection = new AppServiceConnection();
                connection.PackageFamilyName = Package.Current.Id.FamilyName;
                connection.AppServiceName = "TaskmanWindowService";
                connection.RequestReceived += Connection_RequestReceived;
                connection.ServiceClosed += Connection_ServiceClosed;
                AppServiceConnectionStatus connectionStatus = await connection.OpenAsync();
                if (connectionStatus != AppServiceConnectionStatus.Success)
                {
                    MessageBox.Show("Status: " + connectionStatus.ToString());
                    return;
                }
            }
            //Debug.WriteLine("SEND UWP REQUEST");
            await connection.SendMessageAsync(message);
        }

        private void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //MessageBox.Show("UWP RECEIVED REQUEST");
            string path = "";
            foreach(var v in args.Request.Message.Values)
            {
                //MessageBox.Show($"{(string)v}");
                path = (string)v;
            }
            if (path.Length > 0)
            {
                int startPos = path.LastIndexOf('\\') + 1;
                string file = path.Substring(startPos, path.Length - startPos);
                //MessageBox.Show(file);
                //Process process = processes.ToList().Find(x => x.ProcessName.Contains(filepath));
                //MessageBox.Show(process.ProcessName);
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/C taskkill /F /IM \"{file}\" /T";
                process.StartInfo = startInfo;
                process.Start();
            }


            //process.Kill();
            /*object message;
            args.Request.Message.TryGetValue("item", out message);
            MessageBox.Show($"{(string)message}");*/

        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            connection.ServiceClosed -= Connection_ServiceClosed;
            connection = null;
        }
    }
}
