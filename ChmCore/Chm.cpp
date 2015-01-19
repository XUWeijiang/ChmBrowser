// Class1.cpp
#include "pch.h"
#include "Chm.h"
#include "ChmDoc.h"

using namespace ChmCore;
using namespace Platform;

ChmOutline^ CreateOutlineFromTopic(const Topic& t, ChmOutline^ parent)
{
    ChmOutline^ outline = ref new ChmOutline(ref new Platform::String(t.Name.c_str()), ref new Platform::String(t.Url.c_str()), parent);
    for (size_t i = 0; i < t.SubTopics.size(); ++i)
    {
        outline->SubSections->Append(CreateOutlineFromTopic(t.SubTopics[i], outline));
    }
    return outline;
}

Chm::Chm(Platform::String^ file)
{
    file_ = file;
    doc_.reset(ChmDoc::CreateFromFile(file->Data()));
    if (doc_ == nullptr)
    {
        throw ref new Platform::FailureException();
    }
    auto t = doc_->GetTopics();
    Outline = CreateOutlineFromTopic(t, nullptr);
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
    InitializeCriticalSectionEx(&docAccess_, 0, 0);
}
Platform::Array<byte>^ Chm::GetData(Platform::String^ path)
{
    ScopedCritSec scope(&docAccess_);
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
    ScopedCritSec scope(&docAccess_);
    ScopedMem<WCHAR> plainUrl(url::GetFullPath(path->Data()));
    ScopedMem<char> urlUtf8(str::conv::ToUtf8(plainUrl));
    return doc_->HasData(urlUtf8);
}