// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

using CalculatorApp;
//using CalculatorApp.Common;
using TraceLogging;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Interop;
using Windows.UI.Xaml.Navigation;
using System.Data;

namespace CalculatorApp
{
    public sealed class WindowFrameService
    {

        public Page GetCurrentPage()
        {

            return (m_frame.Content as Page);

        }

        public void SetNewFrame(Windows.UI.Xaml.Controls.Frame frame)
        {

            Debug.Assert(frame.BackStackDepth == 0);
            m_frame = frame;

        }

        // createdByUs means any window that we create.
        // !createdByUs means the main window
        internal static WindowFrameService CreateNewWindowFrameService(Frame viewFrame, bool createdByUs, WeakReference parent)
        {

            Debug.Assert(CoreWindow.GetForCurrentThread() != null);
            var frameService = new WindowFrameService(viewFrame, parent);
            frameService.InitializeFrameService(createdByUs);
            return frameService;

        }

        public CoreDispatcher GetCoreDispatcher()
        {

            return m_coreDispatcher;

        }

        public int GetViewId()
        {

            return m_viewId;

        }

        public void RegisterOnWindowClosingHandler(Action onWindowClosingHandler)
        {

            m_onWindowClosingHandlers.Add(onWindowClosingHandler);

        }

        public async Task HandleViewRelease()
        {
            var that = this;
            //task_completion_event<void> closingHandlersCompletedEvent;
            await m_coreDispatcher.RunAsync(CoreDispatcherPriority.Low, new DispatchedHandler(() =>
            {
                //KeyboardShortcutManager.OnWindowClosed(that.m_viewId);
                Window.Current.Content = null;
                that.InvokeWindowClosingHandlers();
                // This is to ensure InvokeWindowClosingHandlers is be done before RemoveWindowFromMap
                // If InvokeWindowClosingHandlers throws any exception we want it to crash the application
                // so we are OK not setting closingHandlersCompletedEvent in that case
                //closingHandlersCompletedEvent.set();
                that.m_coreDispatcher.StopProcessEvents();
                Window.Current.Close();
            }));
            //return create_task(closingHandlersCompletedEvent);
        }

        // Throws InvalidArgumentException if a service is already registered with the specified id
        public void RegisterRuntimeWindowService(Type serviceId, Object service)
        {

            if (TryResolveRuntimeWindowService(serviceId) != null)
            {
                throw new DuplicateNameException(serviceId + " already registered");
            }

            m_runtimeServicesMap[serviceId.Name] = service;

        }

        // Returns false if no service was registered with the specified id
        public bool RemoveRuntimeWindowService(Type serviceId)
        {

            return m_runtimeServicesMap.Remove(serviceId.Name);

        }

        // Throws InvalidArgumentException if no service is registered with the specified id
        public Object ResolveRuntimeWindowService(Type serviceId)
        {
            var service = TryResolveRuntimeWindowService(serviceId);

            if (service != null)
            {
                return service;
            }
            else
            {
                throw new EntryPointNotFoundException(serviceId.Name + " not found");
            }
        }


        public Frame GetFrame()
        {

            return m_frame;

        }

        public void InvokeWindowClosingHandlers()
        {

            // Should be called only once just before we kill the window.
            foreach (var handler in m_onWindowClosingHandlers)
            {
                handler();
            }
            m_onWindowClosingHandlers.Clear();

        }

        private WindowFrameService(Frame frame, WeakReference parent)
        {
            m_currentWindow = CoreWindow.GetForCurrentThread();
            m_coreDispatcher = m_currentWindow.Dispatcher;
            m_frame = frame;
            m_parent = parent;
            m_viewId = ApplicationView.GetApplicationViewIdForWindow(m_currentWindow);

        }

        private void InitializeFrameService(bool createdByUs)
        {

            Debug.Assert(createdByUs == (!CoreApplication.GetCurrentView().IsHosted && !CoreApplication.GetCurrentView().IsMain));
            if (createdByUs)
            {
                ApplicationView.GetForCurrentView().Consolidated += OnConsolidated;
            }
            else
            {
                CoreWindow.GetForCurrentThread().Closed += OnClosed;
            }

        }

        private void OnConsolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs e)
        {
            //TraceLogger.GetInstance().DecreaseWindowCount();
            if (m_parent.IsAlive)
            {
                var parent = m_parent.Target as App;
                parent.RemoveWindow(this);
            }
        }

        private void OnClosed(CoreWindow sender, CoreWindowEventArgs args)
        {

            if (m_parent.IsAlive)
            {
                var parent = m_parent.Target as App;
                parent.RemoveSecondaryWindow(this);
            }
        }

        // Returns nullptr if no service is registered with the specified id
        private object TryResolveRuntimeWindowService(Type serviceId)
        {
            object retval;
            if (m_runtimeServicesMap.TryGetValue(serviceId.Name, out retval))
            {
                return retval;
            }
            else
            {
                return null;
            }
        }

        private Windows.UI.Core.CoreWindow m_currentWindow;
        private Windows.UI.Core.CoreDispatcher m_coreDispatcher;
        private Windows.UI.Xaml.Controls.Frame m_frame;
        private int m_viewId;
        private WeakReference m_parent;

        private Dictionary<string, Object> m_runtimeServicesMap = new Dictionary<string, object>();
        private List<Action> m_onWindowClosingHandlers = new List<Action>();
    }

}


