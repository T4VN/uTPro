using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace uTPro.Feature.FormBuilder.Models
{

    [TableName("uTProFormFieldValues")]
    [PrimaryKey("Id")]
    internal class uTProFormFieldValues : uTProBaseModels
    {
        [Column("FieldId")]
        public int FieldId { get; set; }

        [Column("Label")]
        [NullSetting]
        public string? label { get; set; }

        [Column("Value")]
        [NullSetting]
        public string? value { get; set; }

        [Column("Selected")]
        public bool selected { get; set; }

        [Column("Sequence")]
        public int? Sequence { get; set; }
    }
}
