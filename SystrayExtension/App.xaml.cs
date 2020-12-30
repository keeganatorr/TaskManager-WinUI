﻿//*********************************************************  
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
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SystrayExtension
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static BackgroundTaskDeferral AppServiceDeferral = null;
        public static AppServiceConnection Connection = null;
        public static bool IsForeground = false;
        public string LocalFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\";
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.LeavingBackground += App_LeavingBackground;
            this.EnteredBackground += App_EnteredBackground;

            Windows.UI.ViewManagement.ApplicationView.PreferredLaunchViewSize = new Windows.Foundation.Size(1000, 500);
            Windows.UI.ViewManagement.ApplicationView.PreferredLaunchWindowingMode = Windows.UI.ViewManagement.ApplicationViewWindowingMode.PreferredLaunchViewSize;
            Debug.WriteLine(LocalFolderPath);
        }

        private void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            IsForeground = false;
        }

        private void App_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            IsForeground = true;
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            // connection established from the fulltrust process
            base.OnBackgroundActivated(args);
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details)
            {
                AppServiceDeferral = args.TaskInstance.GetDeferral();
                args.TaskInstance.Canceled += OnTaskCanceled;
                Connection = details.AppServiceConnection;
                Connection.RequestReceived += OnRequestReceived;
                Debug.WriteLine("Connection established.");

                /*ValueSet request = new ValueSet();
                request.Add("D1", "d1");
                SendToWinForms(request);*/
            }
        }

        public static async void SendToWinForms(ValueSet valueSet)
        {
            AppServiceResponse response = await Connection.SendMessageAsync(valueSet);

        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //Debug.WriteLine("RECEIVE UWP REQUEST");
            if (args.Request.Message.ContainsKey("clear"))
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
                {
                    Global.ProcessList.Clear();
                }));
            }
            if (args.Request.Message.ContainsKey("content"))
            {
                object message = null;
                args.Request.Message.TryGetValue("content", out message);
                if (App.IsForeground)
                {                    
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async()=>
                    {
                        //Debug.WriteLine(message);

                        string[] process_Files = (message as string).Split('\n');


                        foreach (string s in process_Files)
						{
                            int startPos = s.LastIndexOf('\\') + 1;
                            string iconFile = s.Substring(startPos, s.Length - startPos);
                            Global.ProcessList.Add(new OpenProcess() { FilePath = s, Icon = iconFile });
						}

                        //MessageDialog dialog = new MessageDialog(message.ToString());
                        //await dialog.ShowAsync();
                    }));
                }
                /*else
                {
                    ToastHelper.ShowToast(message.ToString());
                }*/
            }

            if (args.Request.Message.ContainsKey("exit"))
            {
                App.Current.Exit();
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (AppServiceDeferral != null)
            {
                try
                {
                    AppServiceDeferral.Complete();
                }
                catch { }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            if (AppServiceDeferral != null)
            {
                AppServiceDeferral.Complete();
            }
            deferral.Complete();
        }
    }
}