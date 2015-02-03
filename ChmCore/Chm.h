﻿/* Copyright 2015 XU Weijiang (weijiang.xu AT gmail.com) License: GPLv3 */

#pragma once
#include <windows.h>
#include <collection.h>
#include "ChmDoc.h"
#include <memory>
#include <map>

namespace ChmCore
{
    public ref class ChmTopic sealed
    {
    public:
        property Platform::String^ Name;
        property Platform::String^ Url;
        property int Level;
    public:
        ChmTopic(Platform::String^ name, Platform::String^ url, int level)
        {
            Name = name;
            Url = url;
            Level = level;
        }
    };
    public ref class Chm sealed
    {
    private:
        Platform::String^ file_;
        Platform::String^ title_;
        Platform::String^ home_;
    public:
        property Windows::Foundation::Collections::IVector<ChmTopic^>^ Contents;
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
            Contents->Clear();
            Contents = nullptr;
        }
    public:
        Platform::Array<byte>^ GetData(Platform::String^ path);
        bool HasData(Platform::String^ path);
    private:
        std::unique_ptr<ChmDoc> doc_;
    };
}