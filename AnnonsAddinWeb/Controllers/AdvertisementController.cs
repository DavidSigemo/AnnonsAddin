using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AnnonsAddinWeb.ViewModels;
using System.Text;
using Newtonsoft.Json;
using System.Web.Script.Serialization;

namespace AnnonsAddinWeb.Controllers
{
    public class AdvertisementController : Controller
    {
        [SharePointContextFilter]
        public ActionResult Index()
        {
            var model = new IndexAdvertisementViewModel();
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
            if (spContext != null)
            {
                Session["SpContext"] = spContext;
                Session["SpHostUrl"] = spContext.SPHostUrl;
            }
            else
            {
                spContext = Session["SpContext"] as SharePointContext;
            }
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    ListCollection listCol = clientContext.Web.Lists;
                    TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(clientContext);
                    User currentUser = clientContext.Web.CurrentUser;
                    clientContext.Load(taxonomySession);
                    clientContext.Load(listCol, y => y.Where(x => x.Title == "Annonser"));
                    clientContext.Load(currentUser);
                    clientContext.ExecuteQuery();
                    //List list = clientContext.Web.Lists.GetByTitle("Annonser");     Fungerar utan lista??

                    if (taxonomySession != null)
                    {
                        #region Create Category Taxonomy
                        Session["TaxSession"] = taxonomySession;
                        TermStore termStore = taxonomySession.GetDefaultSiteCollectionTermStore();
                        TermGroupCollection termGroupCol = termStore.Groups;
                        clientContext.Load(termGroupCol, t => t.Where(y => y.Name == "Advertisements"));
                        clientContext.ExecuteQuery();

                        var termGroup = termGroupCol.FirstOrDefault();
                        if (termGroup == null)
                        {
                            TermGroup annonsKategorierGroup = termStore.CreateGroup("Advertisements", Guid.NewGuid());
                            clientContext.ExecuteQuery();
                            TermSet annonsKateGorierTermSet = annonsKategorierGroup.CreateTermSet("Categories", Guid.NewGuid(), 1033);
                            clientContext.ExecuteQuery();

                            annonsKateGorierTermSet.CreateTerm("Electronics", 1033, Guid.NewGuid());
                            annonsKateGorierTermSet.CreateTerm("Appliances", 1033, Guid.NewGuid());
                            annonsKateGorierTermSet.CreateTerm("Clothing", 1033, Guid.NewGuid());
                            annonsKateGorierTermSet.CreateTerm("Books", 1033, Guid.NewGuid());
                            annonsKateGorierTermSet.CreateTerm("Office", 1033, Guid.NewGuid());
                            annonsKateGorierTermSet.CreateTerm("Other", 1033, Guid.NewGuid());
                            clientContext.ExecuteQuery();
                            termGroup = annonsKategorierGroup;
                        }
                        #endregion

                        if (termGroup != null)
                        {
                            TermSet termSet = termGroup.TermSets.GetByName("Categories");
                            TermCollection terms = termSet.GetAllTerms();
                            clientContext.Load(termSet);
                            clientContext.Load(terms);
                            clientContext.ExecuteQuery();

                            foreach (Term term in terms)
                            {
                                SelectListItem newItem = new SelectListItem { Value = term.Id.ToString(), Text = term.Name };
                                model.Categories.Add(newItem);
                            }

                        }
                    }

                    var list = listCol.FirstOrDefault();

                    if (list == null)
                    {
                        #region Create Advertisement List
                        ListCreationInformation listCreationInfo = new ListCreationInformation();
                        listCreationInfo.Title = "Annonser";
                        listCreationInfo.TemplateType = (int)ListTemplateType.GenericList;

                        var newList = clientContext.Web.Lists.Add(listCreationInfo);
                        FieldCollection fieldCol = newList.Fields;


                        Field defaultTitleField = fieldCol.GetByTitle("Title");
                        clientContext.Load(fieldCol);
                        clientContext.Load(defaultTitleField);
                        clientContext.ExecuteQuery();
                        defaultTitleField.Hidden = true;
                        defaultTitleField.SetShowInDisplayForm(false);
                        defaultTitleField.SetShowInEditForm(false);
                        defaultTitleField.SetShowInNewForm(false);
                        defaultTitleField.Required = false;
                        defaultTitleField.Update();

                        Field rubrikField = newList.Fields.AddFieldAsXml("<Field DisplayName='Rubrik' Type='Text' Name='Rubrik' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);
                        Field textField = newList.Fields.AddFieldAsXml("<Field DisplayName='Text' Type='Text' Name='Text' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);
                        Field prisField = newList.Fields.AddFieldAsXml("<Field DisplayName='Pris' Type='Number' Name='Pris' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);
                        Field datumField = newList.Fields.AddFieldAsXml("<Field DisplayName='Datum' Type='DateTime' Name='Datum' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);
                        //Field anvandareField = newList.Fields.AddFieldAsXml("<Field DisplayName='Användare' Type='User' Name='Anvandare' StaticName='Anvandare' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);
                        Field kategoriField = newList.Fields.AddFieldAsXml("<Field DisplayName='Kategori' Type='TaxonomyFieldType' Name='Kategori' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);

                        FieldNumber rubrikFieldNumber = clientContext.CastTo<FieldNumber>(rubrikField);
                        FieldNumber textFieldNumber = clientContext.CastTo<FieldNumber>(textField);
                        FieldNumber prisFieldNumber = clientContext.CastTo<FieldNumber>(prisField);
                        FieldNumber datumFieldNumber = clientContext.CastTo<FieldNumber>(datumField);
                        //FieldNumber anvandareFieldNumber = clientContext.CastTo<FieldNumber>(anvandareField);
                        //FieldNumber kategoryFieldNumber = clientContext.CastTo<FieldNumber>(anvandareField);
                        Guid termStoreId = Guid.Empty;
                        Guid termSetId = Guid.Empty;
                        GetTaxonomyFieldInfo(clientContext, out termStoreId, out termSetId, "Categories");
                        TaxonomyField kategoryFieldNumber = clientContext.CastTo<TaxonomyField>(kategoriField);
                        kategoryFieldNumber.SspId = termStoreId;
                        kategoryFieldNumber.TermSetId = termSetId;
                        kategoryFieldNumber.TargetTemplate = String.Empty;
                        kategoryFieldNumber.AnchorId = Guid.Empty;

                        rubrikFieldNumber.Update();
                        textFieldNumber.Update();
                        prisFieldNumber.Update();
                        datumFieldNumber.Update();
                        //anvandareFieldNumber.Update();
                        kategoryFieldNumber.Update();

                        View view = newList.Views.GetByTitle("All Items");
                        clientContext.Load(view);
                        clientContext.ExecuteQuery();
                        ViewFieldCollection viewFields = view.ViewFields;
                        viewFields.Remove("LinkTitle");
                        view.Update();

                        clientContext.ExecuteQuery();

                        list = newList;
                        #endregion
                    }
                    CamlQuery cQuery = new CamlQuery();
                    cQuery.ViewXml = @"<View>
                                        <Query>
                                        <Where>
                                        <Eq>
                                        <FieldRef Name='Author' LookupId='True'/>
                                        <Value Type='Lookup'>" + currentUser.Id + @"</Value>
                                        </Eq>
                                        </Where>
                                        </Query>
                                        </View>";
                    var listItems = list.GetItems(cQuery);
                    clientContext.Load(listItems);
                    clientContext.ExecuteQuery();

                    foreach (ListItem listItem in listItems)
                    {
                        AdvertisementViewModel tempObj = new AdvertisementViewModel
                        {
                            Title = listItem["Rubrik"].ToString(),
                            Text = listItem["Text"].ToString(),
                            Price = int.Parse(listItem["Pris"].ToString()),
                            Date = DateTime.Parse(listItem["Datum"].ToString()),
                            User = listItem["Author"] as FieldUserValue,
                            Category = listItem["Kategori"] as TaxonomyFieldValue,
                            ListItemId = listItem["ID"].ToString()
                        };
                        model.Advertisements.Add(tempObj);
                    }


                }
                return View(model);
            }
        }

        private void GetTaxonomyFieldInfo(ClientContext clientContext, out Guid termStoreId, out Guid termSetId, string termSetName)
        {
            termStoreId = Guid.Empty;
            termSetId = Guid.Empty;

            TaxonomySession session = TaxonomySession.GetTaxonomySession(clientContext);
            TermStore termStore = session.GetDefaultSiteCollectionTermStore();
            TermSetCollection termSets = termStore.GetTermSetsByName(termSetName, 1033);

            clientContext.Load(termSets, tsc => tsc.Include(ts => ts.Id));
            clientContext.Load(termStore, ts => ts.Id);
            clientContext.ExecuteQuery();

            termStoreId = termStore.Id;
            termSetId = termSets.FirstOrDefault().Id;
        }

        public ActionResult DeleteAdvertisement(int id, string SpHostUrl)
        {
            var model = new AdvertisementViewModel();
            SharePointContext spContext = Session["SpContext"] as SharePointContext;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                List list = clientContext.Web.Lists.GetByTitle("Annonser");
                var listItem = list.GetItemById(id);
                listItem.DeleteObject();

                clientContext.ExecuteQuery();

                return RedirectToAction("Index", new { SpHostUrl = SpHostUrl });
            }
        }

        [HttpPost]
        public JsonResult UpdateListItem(AdvertisementViewModel editedItem)
        {
            var spContext = Session["SpContext"] as SharePointContext;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                ListCollection listCol = clientContext.Web.Lists;
                User currentUser = clientContext.Web.CurrentUser;
                clientContext.Load(listCol, y => y.Where(x => x.Title == "Annonser"));
                clientContext.Load(currentUser);
                clientContext.ExecuteQuery();

                var list = listCol.FirstOrDefault();
                if (list != null)
                {
                    var listItem = list.GetItemById(editedItem.ListItemId);
                    clientContext.Load(listItem);
                    listItem["Rubrik"] = editedItem.Title;
                    listItem["Text"] = editedItem.Text;
                    listItem["Pris"] = editedItem.Price;
                    listItem["Kategori"] = editedItem.SelectedCategory;

                    listItem.Update();
                    clientContext.ExecuteQuery();

                    editedItem = new AdvertisementViewModel
                    {
                        Title = listItem["Rubrik"].ToString(),
                        Text = listItem["Text"].ToString(),
                        Price = int.Parse(listItem["Pris"].ToString()),
                        Date = DateTime.Parse(listItem["Datum"].ToString()),
                        User = listItem["Author"] as FieldUserValue,
                        Category = listItem["Kategori"] as TaxonomyFieldValue,
                        ListItemId = listItem["ID"].ToString()
                    };
                }
            }
            return Json(editedItem, JsonRequestBehavior.AllowGet);
        }

        public ActionResult BuyAuctions()
        {
            var model = new SearchAdvertisementViewModel();

            model.FilterList.Add(new SelectListItem { Value = "PriceAsc", Text = "Pris (Stigande)" });
            model.FilterList.Add(new SelectListItem { Value = "PriceDesc", Text = "Pris (Fallande)" });
            model.FilterList.Add(new SelectListItem { Value = "DateAsc", Text = "Datum (Stigande)" });
            model.FilterList.Add(new SelectListItem { Value = "DateDesc", Text = "Datum (Fallande)" });


            SharePointContext spContext = Session["SpContext"] as SharePointContext;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(clientContext);

                if (taxonomySession != null)
                {
                    TermStore termStore = taxonomySession.GetDefaultSiteCollectionTermStore();
                    TermGroupCollection termGroupCol = termStore.Groups;
                    clientContext.Load(termGroupCol, t => t.Where(y => y.Name == "Advertisements"));
                    clientContext.ExecuteQuery();

                    TermGroup termGroup = termGroupCol.FirstOrDefault();
                    if (termGroup != null)
                    {
                        TermSet termSet = termGroup.TermSets.GetByName("Categories");
                        TermCollection terms = termSet.GetAllTerms();
                        clientContext.Load(termSet);
                        clientContext.Load(terms);
                        clientContext.ExecuteQuery();

                        foreach (Term term in terms)
                        {
                            SelectListItem newItem = new SelectListItem { Value = term.Name, Text = term.Name };
                            model.CategoryList.Add(newItem);
                        }

                    }
                }
            }

            model.CategoryList.OrderBy(x => x.Text);
            model.CategoryList.Insert(0, new SelectListItem { Value = "Alla", Text = "Alla" });
            return View(model);
        }

        public PartialViewResult SearchAuctions(SearchAdvertisementViewModel searchData)
        {
            var model = new List<AdvertisementViewModel>();
            SharePointContext spContext = Session["SpContext"] as SharePointContext;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                ListCollection listCol = clientContext.Web.Lists;
                User currentUser = clientContext.Web.CurrentUser;
                clientContext.Load(listCol, y => y.Where(x => x.Title == "Annonser"));
                clientContext.Load(currentUser);
                clientContext.ExecuteQuery();

                var list = listCol.FirstOrDefault();

                if (list != null)
                {
                    CamlQuery cQuery = new CamlQuery();

                    cQuery.ViewXml = CamlSearchQueryBuilder(searchData.SearchText, searchData.SelectedCategory);
                    var listItems = list.GetItems(cQuery);
                    clientContext.Load(listItems);
                    clientContext.ExecuteQuery();

                    foreach (ListItem listItem in listItems)
                    {
                        AdvertisementViewModel tempObj = new AdvertisementViewModel
                        {
                            Title = listItem["Rubrik"].ToString(),
                            Text = listItem["Text"].ToString(),
                            Price = int.Parse(listItem["Pris"].ToString()),
                            Date = DateTime.Parse(listItem["Datum"].ToString()),
                            User = listItem["Author"] as FieldUserValue,
                            Category = listItem["Kategori"] as TaxonomyFieldValue,
                            ListItemId = listItem["ID"].ToString()
                        };
                        model.Add(tempObj);
                    }
                }
            }
            if (searchData.SelectedFilter == "PriceAsc")
            {
                model.OrderBy(x => x.Price);
            }
            else if (searchData.SelectedFilter == "PriceDesc")
            {
                model.OrderByDescending(x => x.Price);
            }
            else if (searchData.SelectedFilter == "DateAsc")
            {
                model.OrderBy(x => x.Date);
            }
            else if (searchData.SelectedFilter == "DateDesc")
            {
                model.OrderByDescending(x => x.Date);
            }
            return PartialView("_AuctionSearchPartial", model);
        }

        private string CamlSearchQueryBuilder(string searchInput, string selectedCategory)
        {
            StringBuilder stringBuilder = new StringBuilder("<View><Query><Where>");
            if (string.IsNullOrEmpty(searchInput) && selectedCategory == "Alla")
            {
                stringBuilder.AppendLine(@"<Neq>
                                                <FieldRef Name='Author' LookupId='True'/>
                                                <Value Type='Integer'>
                                                    <UserID/>
                                                </Value>
                                            </Neq>");
            }
            else if (string.IsNullOrEmpty(searchInput) && selectedCategory != "Alla")
            {
                stringBuilder.AppendLine(@"<And>
                                                <Neq>
                                                    <FieldRef Name='Author' LookupId='True'/>
                                                    <Value Type='Integer'>
                                                        <UserID/>
                                                    </Value>
                                                </Neq>
                                                <Eq>
                                                    <FieldRef Name='Kategori'/>
                                                    <Value Type='Text'>" + selectedCategory + @"</Value>
                                                </Eq>
                                            </And>");
            }
            else if (!string.IsNullOrEmpty(searchInput) && selectedCategory == "Alla")
            {
                stringBuilder.AppendLine(@"                                
                                             <And>
                                                <Neq>
                                                    <FieldRef Name='Author' LookupId='True'/>
                                                    <Value Type='Integer'><UserID/></Value>
                                                </Neq>
                                                <Or>
                                                    <Contains>
                                                        <FieldRef Name='Rubrik'/>
                                                        <Value Type='Text'>" + searchInput + @"</Value>
                                                    </Contains>
                                                    <Contains>
                                                        <FieldRef Name='Text'/>
                                                        <Value Type='Text'>" + searchInput + @"</Value>
                                                    </Contains>
                                                </Or>
                                            </And> 
                            ");
            }
            else if (!string.IsNullOrEmpty(searchInput) && selectedCategory != "Alla")
            {
                stringBuilder.AppendLine(@"                                
                                             <And>
                                                <Neq>
                                                    <FieldRef Name='Author' LookupId='True'/>
                                                    <Value Type='Integer'><UserID/></Value>
                                                </Neq>
                                                <And>
                                                    <Or>
                                                        <Contains>
                                                            <FieldRef Name='Rubrik'/>
                                                            <Value Type='Text'>" + searchInput + @"</Value>
                                                        </Contains>
                                                        <Contains>
                                                            <FieldRef Name='Text'/>
                                                            <Value Type='Text'>" + searchInput + @"</Value>
                                                        </Contains>
                                                    </Or>
                                                    <Eq>
                                                        <FieldRef Name='Kategori'/>
                                                        <Value Type='Text'>" + selectedCategory + @"</Value>
                                                    </Eq>
                                                </And>
                                            </And> 
                            ");
            }
            stringBuilder.AppendLine("</Where></Query></View>");
            return stringBuilder.ToString();
        }

        public ActionResult ViewAuction(int id, string SpHostUrl)
        {
            var model = new AdvertisementViewModel();
            SharePointContext spContext = Session["SpContext"] as SharePointContext;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                ListCollection listCol = clientContext.Web.Lists;
                User currentUser = clientContext.Web.CurrentUser;
                clientContext.Load(listCol, y => y.Where(x => x.Title == "Annonser"));
                clientContext.Load(currentUser);
                clientContext.ExecuteQuery();

                var list = listCol.FirstOrDefault();
                var listItem = list.GetItemById(id);
                clientContext.Load(listItem);
                clientContext.ExecuteQuery();

                if (listItem != null)
                {
                    model = new AdvertisementViewModel
                    {
                        Title = listItem["Rubrik"].ToString(),
                        Text = listItem["Text"].ToString(),
                        Price = int.Parse(listItem["Pris"].ToString()),
                        Date = DateTime.Parse(listItem["Datum"].ToString()),
                        User = listItem["Author"] as FieldUserValue,
                        Category = listItem["Kategori"] as TaxonomyFieldValue,
                        ListItemId = listItem["ID"].ToString()

                    };
                }
                return View(model);
            }
        }



        public ActionResult CreateAuction()
        {
            var model = new AdvertisementViewModel();
            var spContext = Session["SpContext"] as SharePointContext;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(clientContext);

                if (taxonomySession != null)
                {

                    TermStore termStore = taxonomySession.GetDefaultSiteCollectionTermStore();
                    TermGroupCollection termGroupCol = termStore.Groups;
                    clientContext.Load(termGroupCol, t => t.Where(y => y.Name == "Advertisements"));
                    clientContext.ExecuteQuery();

                    TermGroup termGroup = termGroupCol.FirstOrDefault();
                    if (termGroup != null)
                    {
                        TermSet termSet = termGroup.TermSets.GetByName("Categories");
                        TermCollection terms = termSet.GetAllTerms();
                        clientContext.Load(termSet);
                        clientContext.Load(terms);
                        clientContext.ExecuteQuery();

                        foreach (Term term in terms)
                        {
                            SelectListItem newItem = new SelectListItem { Value = term.Id.ToString(), Text = term.Name };
                            model.CategoryList.Add(newItem);
                        }

                    }
                }
            }
            ModelState.Clear();
            return View(model);
        }

        [HttpPost]
        public ActionResult PostAuction(AdvertisementViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var spContext = Session["SpContext"] as SharePointContext;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                ListCollection listCol = clientContext.Web.Lists;
                User currentUser = clientContext.Web.CurrentUser;
                clientContext.Load(listCol, y => y.Where(x => x.Title == "Annonser"));
                clientContext.Load(currentUser);
                clientContext.ExecuteQuery();

                var list = listCol.FirstOrDefault();
                if (list != null)
                {
                    ListItemCreationInformation newAdvertisementInfo = new ListItemCreationInformation();
                    ListItem newAdvertisement = list.AddItem(newAdvertisementInfo);
                    newAdvertisement["Rubrik"] = model.Title;
                    newAdvertisement["Text"] = model.Text;
                    newAdvertisement["Pris"] = model.Price;
                    newAdvertisement["Datum"] = DateTime.Now;
                    newAdvertisement["Anv_x00e4_ndare"] = clientContext.Web.CurrentUser;
                    newAdvertisement["Kategori"] = model.SelectedCategory;

                    newAdvertisement.Update();
                    clientContext.ExecuteQuery();
                }
            }
            return RedirectToAction("Index", new { SPHostUrl = spContext.SPHostUrl });
        }
    }
}