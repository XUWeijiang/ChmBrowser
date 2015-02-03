#pragma once

#include <windows.h>
#include <vector>
#include <utility>
#include <string>
#include <stack>
#include <memory>

/* Copyright 2014 the SumatraPDF project authors (see AUTHORS file).
License: Simplified BSD (see COPYING.BSD) */
enum DocumentProperty {
    Prop_Title, Prop_Author, Prop_Copyright, Prop_Subject,
    Prop_CreationDate, Prop_ModificationDate, Prop_CreatorApp,
    Prop_UnsupportedFeatures, Prop_FontList,
    Prop_PdfVersion, Prop_PdfProducer, Prop_PdfFileStructure,
};

class EbookTocVisitor {
public:
    virtual void Visit(const WCHAR *name, const WCHAR *url, int level) = 0;
    virtual ~EbookTocVisitor() { }
};
