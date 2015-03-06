using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocusignEntity
{
    public class compositeTemplate
    {
        public List<serverTemplate> serverTemplates { get; set; }
        public List<inlineTemplate> inlineTemplates { get; set; }
    }
}
