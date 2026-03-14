using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace uTPro.Feature.FormBuilder.Models
{
    internal class uTProBaseModels
    {
        public uTProBaseModels()
        {
            Id = Guid.NewGuid();
        }

        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column("Id")]
        public Guid Id { get; set; }

        [Column("CreatedDate")]
        public DateTime? CreatedDate { get; set; }

        [Column("CreatedBy")]
        public int? CreatedBy { get; set; }

        [Column("ModifiedDate")]
        [NullSetting]
        public DateTime? ModifiedDate { get; set; }

        [Column("ModifiedBy")]
        [NullSetting]
        public int? ModifiedBy { get; set; }
    }
}
