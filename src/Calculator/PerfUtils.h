#pragma once

#include <deque>

namespace CalculatorApp
{
    class PerfUtils
    {
    public:
        static PerfUtils& Default();

    public:
        PerfUtils();
        void WriteLine(const std::wstring& content);

    public:
        class ScopedLog
        {
        public:
            ScopedLog(PerfUtils& utils, const std::wstring& content);
            ScopedLog(ScopedLog&& rhs);
            ScopedLog(const ScopedLog&) = delete;
            ~ScopedLog();

            ScopedLog& operator=(ScopedLog&& rhs);
            ScopedLog& operator=(const ScopedLog&) = delete;

        private:
            PerfUtils* _utils;
            std::wstring _message;
        };

    public:
        ScopedLog CreateScopedLog(const std::wstring& content);

    private:
        struct WriteRequest
        {
            std::wstring content;
        };

        void _WorkThread();
        void _SubmitReq(WriteRequest&& req);
        std::optional<WriteRequest> _FetchReq();

    private:
        static std::wstring _TimeStamp();

    private:
        Windows::Storage::StorageFile ^ _file;
        std::thread* _workthread;
        std::atomic_flag _quit_flag;
        std::deque<WriteRequest> _wreqs;
        std::mutex _wreqs_mtx;

        std::mutex _cv_mtx;
        std::condition_variable _cv;

    private:
        static PerfUtils _default;
    };

} // namespace CalculatorApp



