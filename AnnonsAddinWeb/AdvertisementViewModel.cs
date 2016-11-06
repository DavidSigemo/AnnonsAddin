using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AnnonsAddinWeb
{
    public class AdvertisementViewModel
    {
        public string ListItemId { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public int Price { get; set; }
        public TaxonomyFieldValue Category { get; set; }
        public DateTime Date { get; set; }
        public FieldUserValue User { get; set; }
    }
}