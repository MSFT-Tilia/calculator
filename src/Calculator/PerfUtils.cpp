#include <chrono>

#include "pch.h"
#include "PerfUtils.h"

#include "CalcViewModel/Common/Utils.h"

using namespace Concurrency;
using namespace Windows::Storage;
using namespace Windows::Foundation;

namespace CalculatorApp
{
    PerfUtils PerfUtils::_default;

    PerfUtils& PerfUtils::Default()
    {
        return _default;
    }

    PerfUtils::PerfUtils()
        : _workthread(nullptr)
    {
        std::time_t now = std::chrono::system_clock::to_time_t(std::chrono::system_clock::now());
        tm utc_now;
        gmtime_s(&utc_now, &now);

        std::wstringstream oss;
        oss << L"cxx.calc.perflog-"
            << std::setw(2) << std::setfill(L'0') << utc_now.tm_mday << L"-"
            << std::setw(2) << std::setfill(L'0') << utc_now.tm_mon + 1 << L"-"
            << std::setw(2) << std::setfill(L'0') << utc_now.tm_year + 1900 << L"-"
            << L"_"
            << std::setw(2) << std::setfill(L'0') << utc_now.tm_hour << L"-"
            << std::setw(2) << std::setfill(L'0') << utc_now.tm_min << L"-"
            << std::setw(2) << std::setfill(L'0') << utc_now.tm_sec
            << L".log";

        auto filename = oss.str();
        auto folder = Windows::Storage::ApplicationData::Current->LocalFolder;
        create_task(
            folder->CreateFileAsync(ref new Platform::String(filename.c_str()), CreationCollisionOption::ReplaceExisting),
            task_continuation_context::use_arbitrary())
            .then([this](StorageFile ^ file) { _file = file; }, task_continuation_context::use_arbitrary())
            .wait();

        _workthread = new std::thread([this]() { _WorkThread(); });
    }

    void PerfUtils::WriteLine(const std::wstring& content)
    {
        WriteRequest req = { content };
        _SubmitReq(std::move(req));
        _cv.notify_all();
    }

    PerfUtils::ScopedLog PerfUtils::CreateScopedLog(const std::wstring& content)
    {
        return ScopedLog(*this, content);
    }

    void PerfUtils::_WorkThread()
    {
        _quit_flag.test_and_set(std::memory_order::memory_order_release);
        while (_quit_flag.test_and_set(std::memory_order::memory_order_acquire))
        {
            for (;;)
            {
                auto req = _FetchReq();
                if (!req.has_value())
                    break;

                create_task(
                    FileIO::AppendTextAsync(
                        _file,
                        ref new Platform::String(req.value().content.c_str())),
                    task_continuation_context::use_arbitrary())
                    .wait();
            }
            std::unique_lock<std::mutex> lk(_cv_mtx);
            _cv.wait(lk);
        }
    }

    void PerfUtils::_SubmitReq(WriteRequest&& req)
    {
        std::scoped_lock<std::mutex> lock(_wreqs_mtx);
        _wreqs.emplace_back(std::move(req));
    }

    std::optional<PerfUtils::WriteRequest> PerfUtils::_FetchReq()
    {
        std::scoped_lock<std::mutex> lock(_wreqs_mtx);
        if (_wreqs.size() > 0)
        {
            auto retval = std::make_optional<WriteRequest>(std::move(_wreqs.front()));
            _wreqs.pop_front();
            return retval;
        }
        return std::optional<WriteRequest>();
    }

    std::wstring PerfUtils::_TimeStamp()
    {
    }

    PerfUtils::ScopedLog::ScopedLog(PerfUtils& utils, const std::wstring& content)
        : _utils(&utils)
        , _message(content)
    {
        _utils->WriteLine(L"ScopedLog | " + _message + L" | begins");
    }

    PerfUtils::ScopedLog::~ScopedLog()
    {
        _utils->WriteLine(L"ScopedLog | " + _message + L" | ends");
    }

    PerfUtils::ScopedLog::ScopedLog(ScopedLog&& rhs)
        : _utils(std::move(rhs._utils))
        , _message(std::move(rhs._message))
    {}

    PerfUtils::ScopedLog& PerfUtils::ScopedLog::operator=(ScopedLog&& rhs)
    {
        _utils = std::move(rhs._utils);
        _message = std::move(rhs._message);
        return *this;
    }
}

