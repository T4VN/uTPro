using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace uTPro.Feature.FormBuilder.Models
{
    [TableName("uTProFormFields")]
    [PrimaryKey("Id")]
    internal class uTProFormFields : uTProBaseModels
    {
        [Column("FormId")]
        public int FormId { get; set; }

        [Column("Type")]
        [NullSetting]
        public string? Type { get; set; }

        [Column("Subtype")]
        [NullSetting]
        public string? Subtype { get; set; }

        [Column("Name")]
        [NullSetting]
        public string? Name { get; set; }

        [Column("Label")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? Label { get; set; }

        [Column("Description")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? Description { get; set; }

        [Column("Placeholder")]
        [NullSetting]
        public string? Placeholder { get; set; }

        [Column("Value")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? Value { get; set; }

        [Column("ClassName")]
        [NullSetting]
        public string? ClassName { get; set; }

        [Column("Style")]
        [NullSetting]
        public string? Style { get; set; }

        [Column("Required")]
        [NullSetting]
        public bool? Required { get; set; }

        [Column("Min")]
        [NullSetting]
        public int? Min { get; set; }

        [Column("Max")]
        [NullSetting]
        public int? Max { get; set; }

        [Column("Maxlength")]
        [NullSetting]
        public int? Maxlength { get; set; }

        [Column("Rows")]
        [NullSetting]
        public int? Rows { get; set; }

        [Column("Multiple")]
        [NullSetting]
        public bool? Multiple { get; set; }

        [Column("Toggle")]
        [NullSetting]
        public bool? Toggle { get; set; }

        [Column("Inline")]
        [NullSetting]
        public bool? Inline { get; set; }

        [Column("Other")]
        [NullSetting]
        public bool? Other { get; set; }

        [Column("Action")]
        [NullSetting]
        public string? Action { get; set; }

        [Column("ActionRedirect")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? ActionRedirect { get; set; }

        [Column("ActionSendemail")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? ActionSendemail { get; set; }

        [Column("ActionJavascript")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? ActionJavascript { get; set; }

        [Column("Sequence")]
        [NullSetting]
        public int? Sequence { get; set; }

        [ResultColumn]
        public virtual List<uTProFormFieldValues>? Values { get; set; }
    }
}
