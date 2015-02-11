using System;

namespace Piranha.Localization.Dto
{
    public class SiteMapHrefAlternative
    {
        public Guid PageId { get; set; }
        public string PermaLink { get; set; }
        public bool IsHidden { get; set; }
        public string Culture { get; set; }
    }
}
