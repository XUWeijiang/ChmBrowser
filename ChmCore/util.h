#pragma once

#include <windows.h>
#include <vector>
#include <utility>
#include <string>
#include <stack>
#include <memory>

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

struct Topic{
    std::wstring Name;
    std::wstring Url;
    int Level;
    std::vector<Topic> SubTopics;
};

class EbookTocHolder : public EbookTocVisitor{
private:
    Topic root_;
    std::stack<Topic*> topics_stack_;
public:
    EbookTocHolder()
    {
        root_.Level = 0;
        topics_stack_.push(&root_);
    }
    Topic GetTopic()
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
        while (!topics_stack_.empty() && topics_stack_.top()->Level >= level)
        {
            topics_stack_.pop();
        }
        if (topics_stack_.empty())
        {
            topics_stack_.push(&root_);
        }
        Topic newTopic;
        newTopic.Name = name;
        while (*url == '/') // trim backslash.
        {
            url++;
        }
        newTopic.Url = url;
        newTopic.Level = level;
        auto x = topics_stack_.top();
        x->SubTopics.push_back(newTopic);
        topics_stack_.push(&x->SubTopics[x->SubTopics.size() - 1]);
    }
};
