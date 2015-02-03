/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

#pragma once
#include <windows.h>
#include <collection.h>
#include "ChmDoc.h"
#include <memory>
#include <map>
namespace ChmCore
{
    public ref class ChmOutline sealed
    {
    public:
        property Platform::String^ Name;
        property Platform::String^ Url;
        property ChmOutline^ Parent;
    public:
        property Windows::Foundation::Collections::IVector<ChmOutline^>^ SubSections;

    public:
        ChmOutline(Platform::String^ name, Platform::String^ url, ChmOutline^ parent)
        {
            Name = name;
            Url = url;
            Parent = parent;
            SubSections = ref new Platform::Collections::Vector<ChmOutline^>();
        }
    public:
        property ChmOutline^ Next
        {
            ChmOutline^ get()
            {
                if (SubSections->Size > 0)
                {
                    return SubSections->GetAt(0);
                }
                ChmOutline^ p = Parent;
                ChmOutline^ c = this;
                while (p != nullptr)
                {
                    unsigned int index = 0;
                    if (p->SubSections->IndexOf(c, &index) && index != p->SubSections->Size - 1)
                    {
                        return p->SubSections->GetAt(index + 1);
                    }
                    else
                    {
                        c = p;
                        p = p->Parent;
                    }
                }
                return nullptr;
            }
        }
        
        property ChmOutline^ Prev
        {
            ChmOutline^ get()
            {
                ChmOutline^ p = Parent;
                ChmOutline^ c = this;
                if (p != nullptr)
                {
                    unsigned int index = 0;
                    p->SubSections->IndexOf(c, &index);
                    if (index == 0)
                    {
                        return p;
                    }
                    else
                    {
                        ChmOutline^ prevNode = p->SubSections->GetAt(index - 1);
                        while (prevNode->SubSections->Size > 0)
                        {
                            prevNode = prevNode->SubSections->GetAt(prevNode->SubSections->Size - 1);
                        }
                        return prevNode;
                    }
                }
                return nullptr;
            }
        }
    };
    public ref class Chm sealed
    {
    private:
        Platform::String^ file_;
        Platform::String^ title_;
        Platform::String^ home_;
    public:
        property ChmOutline^ Outline;
        property Platform::String^ FilePath
        {
            Platform::String^ get() { return file_; }
        }
        property Platform::String^ Title
        {
            Platform::String^ get() { return title_; }
        }
        property Platform::String^ Home
        {
            Platform::String^ get() { return home_; }
        }
    public:
        Chm(Platform::String^ file);
        virtual ~Chm()
        {
            doc_.reset();
        }
    public:
        Platform::Array<byte>^ GetData(Platform::String^ path);
        bool HasData(Platform::String^ path);
    private:
        std::unique_ptr<ChmDoc> doc_;
    };
}