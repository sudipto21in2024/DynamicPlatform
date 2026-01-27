using System.Collections.Generic;

namespace Platform.Engine.Models
{
    public class FormMetadata
    {
        public string Name { get; set; }
        public string EntityTarget { get; set; } // Name of the Entity this form is based on
        public FormLayout Layout { get; set; } = FormLayout.Vertical;
        public List<FormSection> Sections { get; set; } = new List<FormSection>();
        public List<FormField> Fields { get; set; } = new List<FormField>();
        public FormContext Context { get; set; } = new FormContext();
    }

    public class FormSection
    {
        public string Title { get; set; }
        public List<string> FieldNames { get; set; } = new List<string>(); // References fields in the parent Fields list
        public int Order { get; set; }
    }

    public class FormField
    {
        public string Name { get; set; } // Must match Entity Property
        public string Type { get; set; } // string, int, datetime, bool, etc.
        public string Label { get; set; }
        public string Placeholder { get; set; }
        public string Tooltip { get; set; }
        public string DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public string ValidationPattern { get; set; } // Regex
        public string EnumReference { get; set; } // If backed by an Enum
        public int Order { get; set; }
    }

    public enum FormLayout
    {
        Vertical,
        Horizontal,
        Inline
    }
}
