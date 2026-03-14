using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace uTPro.Feature.FormBuilder.Models
{
    [TableName("uTProFormCollectionData")]
    [PrimaryKey("Id")]
    internal class uTProFormCollectionData : uTProBaseModels
    {
        [Column("CollectionId")]
        [NullSetting]
        public Guid CollectionId { get; set; }

        [Column("CollectionName")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? CollectionName { get; set; }

        [Column("CollectionValue")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? CollectionValue { get; set; }

        [Column("Type")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? Type { get; set; }

        [Column("FileName")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? FileName { get; set; }

        [Column("FileExtension")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? FileExtension { get; set; }
    }
}
