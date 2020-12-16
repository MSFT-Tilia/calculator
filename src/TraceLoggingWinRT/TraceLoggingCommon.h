// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once

namespace winrt::TraceLogging::implementation
{
    struct TraceLoggingCommon : TraceLoggingCommonT<TraceLoggingCommon>
    {
    public:
        TraceLoggingCommon();
        static TraceLogging::TraceLoggingCommon GetInstance();

        // As mentioned in Microsoft's Privacy Statement(https://privacy.microsoft.com/en-US/privacystatement#maindiagnosticsmodule),
        // sampling is involved in Microsoft's diagnostic data collection process.
        // These keywords provide additional input into how frequently an event might be sampled.
        // The lower the level of the keyword, the higher the possibility that the corresponding event may be sampled.
        void LogLevel1Event(winrt::hstring const& eventName, winrt::Windows::Foundation::Diagnostics::LoggingFields const& fields);
        void LogLevel2Event(winrt::hstring const& eventName, winrt::Windows::Foundation::Diagnostics::LoggingFields const& fields);
        void LogLevel3Event(winrt::hstring const& eventName, winrt::Windows::Foundation::Diagnostics::LoggingFields const& fields);

        bool GetTraceLoggingProviderEnabled();

    private:
        winrt::Windows::Foundation::Diagnostics::LoggingChannel  g_calculatorProvider;
        winrt::Windows::Foundation::Diagnostics::LoggingActivity  m_appLaunchActivity;
        GUID sessionGuid;
    };
}

namespace winrt::TraceLogging::factory_implementation
{
    struct TraceLoggingCommon : TraceLoggingCommonT<TraceLoggingCommon, implementation::TraceLoggingCommon, winrt::static_lifetime>
    {
        TraceLoggingCommon()
            : m_instance({nullptr})
        {}

        TraceLogging::TraceLoggingCommon GetInstance()
        {
            if (!m_instance)
            {
                slim_lock_guard lock{m_lock};
                if (!m_instance)
                {
                    m_instance = winrt::make<implementation::TraceLoggingCommon>();
                }
            }

            return m_instance;
        }

    private:
        winrt::slim_mutex m_lock;
        TraceLogging::TraceLoggingCommon m_instance;
    };
}
