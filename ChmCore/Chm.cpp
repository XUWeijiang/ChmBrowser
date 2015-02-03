/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) and the SumatraPDF project authors (because part of the code is copied from them) License: GPLv3 */

#include "pch.h"
#include "Chm.h"
#include "ChmDoc.h"

using namespace ChmCore;
using namespace Platform;

class EbookTocExtractor : public EbookTocVisitor{
private:
    std::stack<ChmOutline^> topics_stack_;
    std::stack<int> topics_level_stack_;
    ChmOutline^ root_;
public:
    EbookTocExtractor()
    {
        root_ = ref new ChmOutline("", "", nullptr);
        topics_stack_.push(root_);
        topics_level_stack_.push(0);
    }
    ChmOutline^ GetOutline()
    {
        return root_;
    }
public:
    virtual void Visit(const WCHAR *name, const WCHAR *url, int level) override
    {
        if (url == nullptr)
        {
            return;
        }
        while (!topics_stack_.empty() && topics_level_stack_.top() >= level)
        {
            topics_stack_.pop();
            topics_level_stack_.pop();
        }
        if (topics_stack_.empty())
        {
            topics_stack_.push(root_);
            topics_level_stack_.push(0);
        }
        while (*url == '/') // trim backslash.
        {
            url++;
        }
        auto x = topics_stack_.top();
        ChmOutline^ newOutline = ref new ChmOutline(
            ref new Platform::String(name), 
            ref new Platform::String(url), 
            x);
        x->SubSections->Append(newOutline);
        topics_stack_.push(newOutline);
        topics_level_stack_.push(level);
    }
};

Chm::Chm(Platform::String^ file)
{
    file_ = file;
    doc_.reset(ChmDoc::CreateFromFile(file->Data()));
    if (doc_ == nullptr)
    {
        throw ref new Platform::FailureException();
    }
    try
    {
        EbookTocExtractor holder;
        doc_->ParseToc(&holder);
        Outline = holder.GetOutline();
    }
    catch (...)
    {
        Outline = nullptr;
    }
    WCHAR* title = doc_->GetProperty(DocumentProperty::Prop_Title);
    if (title != nullptr)
    {
        title_ = ref new Platform::String(title);
        free(title);
    }
    else
    {
        title_ = nullptr;
    }
    WCHAR* home = doc_->GetHomePath();
    WCHAR* url = home;
    while (*url == '/') // trim backslash.
    {
        url++;
    }
    home_ = ref new Platform::String(url);
    free(home);
}

Platform::Array<byte>^ Chm::GetData(Platform::String^ path)
{
    ScopedMem<WCHAR> plainUrl(url::GetFullPath(path->Data()));
    size_t length;
    ScopedMem<char> urlUtf8(str::conv::ToUtf8(plainUrl));
    unsigned char* data = doc_->GetData(urlUtf8, &length);
    if (data == nullptr)
    {
        return ref new Platform::Array<byte>(0);
    }
    else
    {
        return ref new Platform::Array<byte>(data, length);
    }
}
bool Chm::HasData(Platform::String^ path)
{
    ScopedMem<WCHAR> plainUrl(url::GetFullPath(path->Data()));
    ScopedMem<char> urlUtf8(str::conv::ToUtf8(plainUrl));
    return doc_->HasData(urlUtf8);
}