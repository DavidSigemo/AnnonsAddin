using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AnnonsAddinWeb.ViewModels
{
    public class AdvertisementViewModel
    {
        public string ListItemId { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required!")]
        public string Title { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Text is required!")]
        public string Text { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Price is required!")]
        [Range(0, 9999999, ErrorMessage = "Price is not in the allowed range!")]
        [DataType(DataType.Currency)]
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