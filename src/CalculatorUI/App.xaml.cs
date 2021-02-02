// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// App.xaml.h
// Declaration of the App class.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using CalculatorApp;
//using CalculatorApp.Common;
//using CalculatorApp.Common.Automation;
using TraceLogging;

//using Microsoft.WRL;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Interop;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using System.Threading;

namespace CalculatorApp
{
    namespace ApplicationResourceKeys
    {
        static public partial class Globals
        {
            public static readonly string AppMinWindowHeight = "AppMinWindowHeight";
            public static readonly string AppMinWindowWidth = "AppMinWindowWidth";
        }
    }

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            m_preLaunched = false;

            RegisterDependencyProperties();

            // TODO: MSFT 14645325: Set this directly from XAML.
            // Currently this is bugged so the property is only respected from code-behind.
            this.HighContrastAdjustment = ApplicationHighContrastAdjustment.None;

            this.Suspending += OnSuspending;

            // CSHARP_MIGRATION: TODO:
#if DEBUG
            this.DebugSettings.IsBindingTracingEnabled = true;
            this.DebugSettings.BindingFailed += (sender, args) => {
                if (Debugger.IsAttached)
                {
                    string errorMessage = args.Message;
                    Debugger.Break();
                }
            };
#endif
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user. Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (args.PrelaunchActivated)
            {
                // If the app got pre-launch activated, then save that state in a flag
                m_preLaunched = true;
            }
            OnAppLaunch(args, args.Arguments);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                // We currently don't pass the uri as an argument,
                // and handle any protocol launch as a normal app launch.
                OnAppLaunch(args, null);
            }
        }

        internal void RemoveWindow(WindowFrameService frameService)
        {
            // Shell does not allow killing the main window.
            if (m_mainViewId != frameService.GetViewId())
            {
                _ = HandleViewReleaseAndRemoveWindowFromMap(frameService);
            }
        }

        internal void RemoveSecondaryWindow(WindowFrameService frameService)
        {
            // Shell does not allow killing the main window.
            if (m_mainViewId != frameService.GetViewId())
            {
                RemoveWindowFromMap(frameService.GetViewId());
            }
        }

        private static Frame CreateFrame()
        {
            var frame = new Frame();

            // CSHARP_MIGRATION: TODO:
            //frame.FlowDirection = LocalizationService.GetInstance().GetFlowDirection();

            return frame;
        }

        private static void SetMinWindowSizeAndActivate(Frame rootFrame, Size minWindowSize)
        {
            // SetPreferredMinSize should always be called before Window.Activate
            ApplicationView appView = ApplicationView.GetForCurrentView();
            appView.SetPreferredMinSize(minWindowSize);

            // Place the frame in the current Window
            Window.Current.Content = rootFrame;
            Window.Current.Activate();
        }

        private void OnAppLaunch(IActivatedEventArgs args, String argument)
        {
            // Uncomment the following lines to display frame-rate and per-frame CPU usage info.
            //#if _DEBUG
            //    if (IsDebuggerPresent())
            //    {
            //        DebugSettings->EnableFrameRateCounter = true;
            //    }
            //#endif

            args.SplashScreen.Dismissed += DismissedEventHandler;

            var rootFrame = (Window.Current.Content as Frame);
            WeakReference weak = new WeakReference(this);

            float minWindowWidth = (float)((double)Resources[ApplicationResourceKeys.Globals.AppMinWindowWidth]);
            float minWindowHeight = (float)((double)Resources[ApplicationResourceKeys.Globals.AppMinWindowHeight]);
            Size minWindowSize = SizeHelper.FromDimensions(minWindowWidth, minWindowHeight);

            ApplicationView appView = ApplicationView.GetForCurrentView();
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            // For very first launch, set the size of the calc as size of the default standard mode
            if (!localSettings.Values.ContainsKey("VeryFirstLaunch"))
            {
                localSettings.Values.Add("VeryFirstLaunch", false);
                appView.SetPreferredMinSize(minWindowSize);
                appView.TryResizeView(minWindowSize);
            }
            else
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            }

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")) // PC Family
                {
                    // Disable the system view activation policy during the first launch of the app
                    // only for PC family devices and not for phone family devices
                    try
                    {
                        ApplicationViewSwitcher.DisableSystemViewActivationPolicy();
                    }
                    catch (Exception)
                    {
                        // Log that DisableSystemViewActionPolicy didn't work
                    }
                }

                // Create a Frame to act as the navigation context
                rootFrame = App.CreateFrame();

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), argument))
                {
                    // We couldn't navigate to the main page, kill the app so we have a good
                    // stack to debug
                    throw new SystemException();
                }

                SetMinWindowSizeAndActivate(rootFrame, minWindowSize);
                m_mainViewId = ApplicationView.GetForCurrentView().Id;
                AddWindowToMap(WindowFrameService.CreateNewWindowFrameService(rootFrame, false, weak));
            }
            else
            {
                // For first launch, LaunchStart is logged in constructor, this is for subsequent launches.

                // !Phone check is required because even in continuum mode user interaction mode is Mouse not Touch
                if ((UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Mouse)
                    && (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")))
                {
                    // If the pre-launch hasn't happened then allow for the new window/view creation
                    if (!m_preLaunched)
                    {
                        var newCoreAppView = CoreApplication.CreateNewView();
                        _ = newCoreAppView.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal, async () =>
                            {
                                var that = weak.Target as App;
                                if (that != null)
                                {
                                    var newRootFrame = App.CreateFrame();

                                    SetMinWindowSizeAndActivate(newRootFrame, minWindowSize);

                                    if (!newRootFrame.Navigate(typeof(MainPage), argument))
                                    {
                                        // We couldn't navigate to the main page, kill the app so we have a good
                                        // stack to debug
                                        throw new SystemException();
                                    }

                                    var frameService = WindowFrameService.CreateNewWindowFrameService(newRootFrame, true, weak);
                                    that.AddWindowToMap(frameService);

                                    var dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

                                    // CSHARP_MIGRATION_ANNOTATION:
                                    // class SafeFrameWindowCreation is being interpreted into a IDisposable class
                                    // in order to enhance its RAII capability that was written in C++/CX
                                    using (var safeFrameServiceCreation = new SafeFrameWindowCreation(frameService, that))
                                    {
                                        int newWindowId = ApplicationView.GetApplicationViewIdForWindow(CoreWindow.GetForCurrentThread());

                                        ActivationViewSwitcher activationViewSwitcher = null;
                                        var activateEventArgs = (args as IViewSwitcherProvider);
                                        if (activateEventArgs != null)
                                        {
                                            activationViewSwitcher = activateEventArgs.ViewSwitcher;
                                        }

                                        if (activationViewSwitcher != null)
                                        {
                                            _ = activationViewSwitcher.ShowAsStandaloneAsync(newWindowId, ViewSizePreference.Default);
                                            safeFrameServiceCreation.SetOperationSuccess(true);
                                        }
                                        else
                                        {
                                            var activatedEventArgs = (args as IApplicationViewActivatedEventArgs);
                                            if ((activatedEventArgs != null) && (activatedEventArgs.CurrentlyShownApplicationViewId != 0))
                                            {
                                                // CSHARP_MIGRATION_ANNOTATION:
                                                // here we don't use ContinueWith() to interpret origin code because we would like to 
                                                // pursue the design of class SafeFrameWindowCreate whichi was using RAII to ensure
                                                // some states get handled properly when its instance is being destructed.
                                                //
                                                // To achieve that, SafeFrameWindowCreate has been reinterpreted using IDisposable
                                                // pattern, which forces we use below way to keep async works being controlled within
                                                // a same code block.
                                                var viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                                                                frameService.GetViewId(),
                                                                ViewSizePreference.Default,
                                                                activatedEventArgs.CurrentlyShownApplicationViewId,
                                                                ViewSizePreference.Default);
                                                // SafeFrameServiceCreation is used to automatically remove the frame
                                                // from the list of frames if something goes bad.
                                                safeFrameServiceCreation.SetOperationSuccess(viewShown);
                                            }
                                        }
                                    }
                                }
                            });
                    }
                    else
                    {
                        ActivationViewSwitcher activationViewSwitcher = null;
                        var activateEventArgs = (args as IViewSwitcherProvider);
                        if (activateEventArgs != null)
                        {
                            activationViewSwitcher = activateEventArgs.ViewSwitcher;
                        }

                        if (activationViewSwitcher != null)
                        {
                            _ = activationViewSwitcher.ShowAsStandaloneAsync(
                                ApplicationView.GetApplicationViewIdForWindow(CoreWindow.GetForCurrentThread()), ViewSizePreference.Default);
                        }
                        else
                        {
                            // CSHARP_MIGRATION: TODO:
                            //TraceLogger.GetInstance().LogError(ViewMode.None, "App.OnAppLaunch", "Null_ActivationViewSwitcher");
                        }
                    }
                    // Set the preLaunched flag to false
                    m_preLaunched = false;
                }
                else // for touch devices
                {
                    if (rootFrame.Content == null)
                    {
                        // When the navigation stack isn't restored navigate to the first page,
                        // configuring the new page by passing required information as a navigation
                        // parameter
                        if (!rootFrame.Navigate(typeof(MainPage), argument))
                        {
                            // We couldn't navigate to the main page,
                            // kill the app so we have a good stack to debug
                            throw new SystemException();
                        }
                    }
                    if (ApplicationView.GetForCurrentView().ViewMode != ApplicationViewMode.CompactOverlay)
                    {
                        if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
                        {
                            // for tablet mode: since system view activation policy is disabled so do ShowAsStandaloneAsync if activationViewSwitcher exists in
                            // activationArgs
                            ActivationViewSwitcher activationViewSwitcher = null;
                            var activateEventArgs = (args as IViewSwitcherProvider);
                            if (activateEventArgs != null)
                            {
                                activationViewSwitcher = activateEventArgs.ViewSwitcher;
                            }
                            if (activationViewSwitcher != null)
                            {
                                var viewId = (args as IApplicationViewActivatedEventArgs).CurrentlyShownApplicationViewId;
                                if (viewId != 0)
                                {
                                    _ = activationViewSwitcher.ShowAsStandaloneAsync(viewId);
                                }
                            }
                        }
                        // Ensure the current window is active
                        Window.Current.Activate();
                    }
                }
            }
        }

        private void DismissedEventHandler(SplashScreen sender, Object e)
        {
            _ = SetupJumpList();
        }

        private void RegisterDependencyProperties()
        {
            // CSHARP_MIGRATION: TODO:
            //NarratorNotifier.RegisterDependencyProperties();
        }

        private void OnSuspending(Object sender, SuspendingEventArgs args)
        {
            // CSHARP_MIGRATION: TODO:
            //TraceLogger.GetInstance().LogButtonUsage();
        }

        sealed class SafeFrameWindowCreation : IDisposable
        {
            public SafeFrameWindowCreation(WindowFrameService frameService, App parent)
            {
                m_frameService = frameService;
                m_frameOpenedInWindow = false;
                m_parent = parent;
            }

            public void SetOperationSuccess(bool success)
            {
                m_frameOpenedInWindow = success;
            }

            public void Dispose()
            {
                if (!m_frameOpenedInWindow)
                {
                    // Close the window as the navigation to the window didn't succeed
                    // and this is not visible to the user.
                    m_parent.RemoveWindowFromMap(m_frameService.GetViewId());
                }

                GC.SuppressFinalize(this);
            }

            ~SafeFrameWindowCreation()
            {
                Dispose();
            }

            private WindowFrameService m_frameService;
            private bool m_frameOpenedInWindow;
            private App m_parent;
        };



        // CSHARP_MIGRATION: TODO: check what is the pragma used for???
        //#pragma optimize("", off) // Turn off optimizations to work around coroutine optimization bug
        private async Task SetupJumpList()
        {
            try
            {
                // CSHARP_MIGRATION: TODO:
                //var calculatorOptions = NavCategoryGroup.CreateCalculatorCategory();

                var jumpList = await JumpList.LoadCurrentAsync();
                jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
                jumpList.Items.Clear();

                // CSHARP_MIGRATION: TODO:
                //foreach (NavCategory option in calculatorOptions.Categories)
                //{
                //    if (!option.IsEnabled)
                //    {
                //        continue;
                //    }
                //    ViewMode mode = option.Mode;
                //    var item = JumpListItem.CreateWithArguments(((int)mode).ToString(), "ms-resource:///Resources/" + NavCategory.GetNameResourceKey(mode));
                //    item.Description = "ms-resource:///Resources/" + NavCategory.GetNameResourceKey(mode);
                //    item.Logo = new Uri("ms-appx:///Assets/" + mode.ToString() + ".png");

                //    jumpList.Items.Append(item);
                //}

                await jumpList.SaveAsync();
            }
            catch
            {
            }
        }

        private async Task HandleViewReleaseAndRemoveWindowFromMap(WindowFrameService frameService)
        {
            WeakReference weak = new WeakReference(this);

            // Unregister the event handler of the Main Page
            var frame = (Window.Current.Content as Frame);
            var mainPage = (frame.Content as MainPage);

            // CSHARP_MIGRATION: TODO:
            //mainPage.UnregisterEventHandlers();

            await frameService.HandleViewRelease();
            await Task.Run(() =>
            {
                var that = weak.Target as App;
                that.RemoveWindowFromMap(frameService.GetViewId());
            }).ConfigureAwait(false /* task_continuation_context::use_arbitrary() */);
        }


        private void AddWindowToMap(WindowFrameService frameService)
        {
            try
            {
                m_windowsMapLock.EnterWriteLock();

                m_secondaryWindows[frameService.GetViewId()] = frameService;
                // CSHARP_MIGRATION: TODO:
                //TraceLogger.GetInstance().UpdateWindowCount(m_secondaryWindows.size());
            }
            finally
            {
                m_windowsMapLock.ExitWriteLock();
            }
        }

        private WindowFrameService GetWindowFromMap(int viewId)
        {
            try
            {
                m_windowsMapLock.EnterReadLock();

                if (m_secondaryWindows.TryGetValue(viewId, out var windowMapEntry))
                {
                    return windowMapEntry;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                m_windowsMapLock.ExitReadLock();
            }
        }

        private void RemoveWindowFromMap(int viewId)
        {
            try
            {
                m_windowsMapLock.EnterWriteLock();

                Debug.Assert(m_secondaryWindows.ContainsKey(viewId), "Window does not exist in the list");
                m_secondaryWindows.Remove(viewId);
            }
            finally
            {
                m_windowsMapLock.ExitWriteLock();
            }
        }

        private readonly ReaderWriterLockSlim m_windowsMapLock = new ReaderWriterLockSlim();
        private Dictionary<int, WindowFrameService> m_secondaryWindows = new Dictionary<int, WindowFrameService>();
        private int m_mainViewId;
        private bool m_preLaunched;

        // CSHARP_MIGRATION: TODO: check whether or not this field is in use.
        private Windows.UI.Xaml.Controls.Primitives.Popup m_aboutPopup;
    }

}

