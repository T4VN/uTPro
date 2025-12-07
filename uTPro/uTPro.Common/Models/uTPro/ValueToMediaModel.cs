namespace uTPro.Common.Models.uTPro
{
    public class ValueToMediaModel
    {
        /// <summary>
        /// max-width: 768px
        /// </summary>
        public string? UrlMobile { get; set; }

        /// <summary>
        /// max-width: 1024px
        /// </summary>
        public string? UrlTablet { get; set; }

        /// <summary>
        /// max-width: 1920px
        /// </summary>
        public string? UrlDesktop { get; set; }

        /// <summary>
        /// </summary>
        public string? Url { get; set; }

        public string? UrlDataSrcSet { get; set; }
    }
}
