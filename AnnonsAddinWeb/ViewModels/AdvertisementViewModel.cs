using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AnnonsAddinWeb.ViewModels
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

        public string SelectedCategory { get; set; }
        public List<SelectListItem> CategoryList { get; set; }

        public AdvertisementViewModel()
        {
            CategoryList = new List<SelectListItem>();
        }
    }
}