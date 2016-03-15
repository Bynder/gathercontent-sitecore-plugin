using System;
using System.Collections.Generic;

namespace GatherContent.Connector.IRepositories.Models.Import
{
    public class CmsField
    {
        public CmsTemplateField TemplateField { get; set; }

        public object Value { get; set; }
    }

    public class FieldValueOptions
    {
        public List<string> Options { get; set; }
    }

    public class FieldValueFiles
    {
        public List<File> Files { get; set; }
    }

    public class File
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        //public string FieldId { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
