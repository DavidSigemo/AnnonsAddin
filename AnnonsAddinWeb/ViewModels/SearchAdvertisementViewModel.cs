using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AnnonsAddinWeb.ViewModels
{
    public class SearchAdvertisementViewModel
    {
        public string SearchText { get; set; }
        public string SelectedCategory { get; set; }
        public string SelectedFilter { get; set; }
        public List<SelectListItem> CategoryList { get; set; }
        public List<SelectListItem> FilterList { get; set; }

        public SearchAdvertisementViewModel()
        {
            CategoryList = new List<SelectListItem>();
            FilterList = new List<SelectListItem>();
        }
    }
}