/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) and the SumatraPDF project authors (because part of the code is copied from them) License: GPLv3 */

#include "pch.h"
#include "Chm.h"
#include "ChmDoc.h"

using namespace ChmCore;
using namespace Platform;

class EbookTocExtractor : public EbookTocVisitor{
private:
    Windows::Foundation::Collections::IVector<ChmTopic^>^ contents_;
public:
    EbookTocExtractor()
    {
        contents_ = ref new Platform::Collections::Vector<ChmTopic^>();
    }
    Windows::Foundation::Collections::IVector<ChmTopic^>^ GetContents()
    {
        return contents_;
    }
public:
    virtual void Visit(const WCHAR *name, const WCHAR *url, int level) override
    {
        if (url == nullptr)
        {
            return;
        }
        ChmTopic^ newTopic = ref new ChmTopic(
            ref new Platform::String(name), 
            ref new Platform::String(url), 
            level - 1); //start from 0 (instead of 1)
        contents_->Append(newTopic);
    }
};
bool Chm::IsValidChmFile(Platform::String^ file)
{
    std::unique_ptr<ChmDoc> doc(ChmDoc::CreateFromFile(file->Data()));
    return doc != nullptr;
}
Chm::Chm(Platform::String^ file, bool loadOutline)
{
    file_ = file;
    doc_.reset(ChmDoc::CreateFromFile(file->Data()));
    if (doc_ == nullptr)
    {
        throw ref new Platform::FailureException();
    }
    if (loadOutline)
    {
        try
        {
            EbookTocExtractor holder;
            doc_->ParseToc(&holder);
            Contents = holder.GetContents();
        }
        catch (...)
        {
            Contents = nullptr;
        }
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
    ScopedMem<unsigned char> data(doc_->GetData(urlUtf8, &length));
    if (data != nullptr)
    {
        return ref new Platform::Array<byte>(data, length);
    }
    return nullptr;
}
bool Chm::HasData(Platform::String^ path)
{
    ScopedMem<WCHAR> plainUrl(url::GetFullPath(path->Data()));
    ScopedMem<char> urlUtf8(str::conv::ToUtf8(plainUrl));
    return doc_->HasData(urlUtf8);
}