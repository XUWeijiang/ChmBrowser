using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChmBrowser.Common
{
    public class HtmlPageInfo: NotifyPropertyChangedBased
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public int Score { get; set; }
    }
}
