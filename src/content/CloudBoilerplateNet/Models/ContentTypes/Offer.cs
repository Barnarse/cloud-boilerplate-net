
using System;
using System.Collections.Generic;
using KenticoCloud.Delivery;

namespace CloudBoilerplateNet.Models
{
    public partial class Offer
    {
        public const string Codename = "offer";
    
        public string Title { get; set; }
        public string Description { get; set; }
        public IEnumerable<Asset> Image { get; set; }

        #region "metadata"
        public ContentItemSystemAttributes System { get; set; }
        #endregion
    }
}