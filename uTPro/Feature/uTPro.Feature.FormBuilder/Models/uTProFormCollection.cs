using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace uTPro.Feature.FormBuilder.Models
{
    [TableName("uTProFormCollection")]
    [PrimaryKey("Id")]
    internal class uTProFormCollection : uTProBaseModels
    {
        [Column("FormId")]
        public Guid FormId { get; set; }

        [Column("WebsiteUrl")]
        [NullSetting]
        public string? WebsiteUrl { get; set; }

        [Column("FormPageUrl")]
        [NullSetting]
        public string? FormPageUrl { get; set; }

        [Column("CollectionIp")]
        [NullSetting]
        public string? CollectionIp { get; set; }

        [Column("BrowserName")]
        [NullSetting]
        public string? BrowserName { get; set; }

        [Column("DeviceType")]
        [NullSetting]
        public string? DeviceType { get; set; }

        [Column("IsMobile")]
        [NullSetting]
        public bool? IsMobile { get; set; }

        [Column("MobileDeviceModel")]
        [NullSetting]
        public string? MobileDeviceModel { get; set; }

        [Column("MobileCompanyName")]
        [NullSetting]
        public string? MobileCompanyName { get; set; }

        [Column("OsName")]
        [NullSetting]
        public string? OsName { get; set; }

        [Column("BrowserVersion")]
        [NullSetting]
        public string? BrowserVersion { get; set; }

        [Column("BrowserId")]
        [NullSetting]
        public string? BrowserId { get; set; }

        [Column("FormLog")]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        [NullSetting]
        public string? FormLog { get; set; }

        [ResultColumn]
        public virtual uTProForms? Form { get; set; }
    }
}
