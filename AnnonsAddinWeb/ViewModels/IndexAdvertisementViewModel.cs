using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AnnonsAddinWeb.ViewModels
{
    public class IndexAdvertisementViewModel
    {
        public List<AdvertisementViewModel> Advertisements { get; set; }
        public string SelectedCategory { get; set; }
        public List<SelectListItem> Categories { get; set; }

        public IndexAdvertisementViewModel()
        {
            Advertisements = new List<AdvertisementViewModel>();
            Categories = new List<SelectListItem>();
        }
    }
}