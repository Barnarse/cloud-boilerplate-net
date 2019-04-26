
using System;
using System.Collections.Generic;
using KenticoCloud.Delivery;

namespace CloudBoilerplateNet.Models
{
    public partial class CreditCard
    {
        public const string Codename = "credit_card";
    
        public string Name { get; set; }
        public string Summary { get; set; }
        public string ShortTagline { get; set; }
        public string LongTagLine { get; set; }
        public decimal AnnualFee { get; set; }
        public decimal PurchaseInterestRate { get; set; }
        public string FlybuysPoints { get; set; }
        public IEnumerable<Asset> HeroImage { get; set; }
        public string Url{get;set;}

        public IEnumerable<Offer> Offers { get; set; }

        #region "metadata"
        public ContentItemSystemAttributes System { get; set; }
        #endregion
    }
}