using System;
using System.Collections.Generic;
using System.Text;

namespace GuideViewer
{
    class Channel
    {
        public string id;
        public string name;

        public override string ToString()
        {
            return id + " - " + name;
        }
    }
}
