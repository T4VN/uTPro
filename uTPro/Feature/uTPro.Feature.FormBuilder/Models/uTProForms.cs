using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace uTPro.Feature.FormBuilder.Models
{
    [TableName("uTProForms")]
    [PrimaryKey("Id")]
    internal class uTProForms : uTProBaseModels
    {
        [Column("Name")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? Name { get; set; }

        [Column("EnableFallback")]
        [NullSetting]
        public bool? EnableFallback { get; set; }

        [Column("Language")]
        [NullSetting]
        public string? Language { get; set; }

        [ResultColumn]
        public virtual IEnumerable<uTProFormFields>? Fields { get; set; }
    }
}
