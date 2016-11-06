﻿using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AnnonsAddinWeb.Controllers
{
    public class AdvertisementController : Controller
    {
        [SharePointContextFilter]
        public ActionResult Index()
        {
            var model = new List<AdvertisementViewModel>();
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
            Session["SpContext"] = spContext;
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
                        Field anvandareField = newList.Fields.AddFieldAsXml("<Field DisplayName='Användare' Type='User' Name='Anvandare' StaticName='Anvandare' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);
                        Field kategoriField = newList.Fields.AddFieldAsXml("<Field DisplayName='Kategori' Type='TaxonomyFieldType' Name='Kategori' Required='TRUE' />", true, AddFieldOptions.AddFieldToDefaultView);

                        FieldNumber rubrikFieldNumber = clientContext.CastTo<FieldNumber>(rubrikField);
                        FieldNumber textFieldNumber = clientContext.CastTo<FieldNumber>(textField);
                        FieldNumber prisFieldNumber = clientContext.CastTo<FieldNumber>(prisField);
                        FieldNumber datumFieldNumber = clientContext.CastTo<FieldNumber>(datumField);
                        FieldNumber anvandareFieldNumber = clientContext.CastTo<FieldNumber>(anvandareField);
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
                        anvandareFieldNumber.Update();
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
                        model.Add(tempObj);
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

        public ActionResult BuyAuctions()
        {
            //SharePointContext spContext = Session["SpContext"] as SharePointContext;
            //using (var clientContext = spContext.CreateUserClientContextForSPHost())
            //{

            //}
            return View();
        }

        public PartialViewResult SearchAuctions(string searchInput)
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
                    cQuery.ViewXml = @"<View>
	                                        <Query>
		                                        <Where>
			                                        <And>
				                                        <Neq>
					                                        <FieldRef Name='Author' LookupId='True'/>
					                                        <Value Type='Lookup'>" + currentUser.Id + @"</Value>
				                                        </Neq>
			                                        </And>
			                                        <And>
				                                        <Or>
					                                        <Contains>
						                                        <FieldRef Name='Rubrik'></FieldRef>
						                                        <Value Type='Text'>" + searchInput + @"</Value>
					                                        </Contains>
				                                        </Or>
				                                        <Or>
					                                        <Contains>
						                                        <FieldRef Name='Text'></FieldRef>
						                                        <Value Type='Text'>" + searchInput + @"</Value>
					                                        </Contains>
				                                        </Or>
				                                        <Or>
					                                        <Contains>
						                                        <FieldRef Name='Author'></FieldRef>
						                                        <Value Type='Lookup'>" + searchInput + @"</Value>
					                                        </Contains>
				                                        </Or>
				                                        <Or>
					                                        <Contains>
						                                        <FieldRef Name='Kategori'></FieldRef>
						                                        <Value Type='Text'>" + searchInput + @"</Value>
					                                        </Contains>
				                                        </Or>
			                                        </And>
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
                        model.Add(tempObj);
                    }
                }
            }
            return PartialView("_AuctionSearchPartial", model);
        }

        public ActionResult ViewAuction(int id)
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
                var listItem = list.GetItemById(id);
                if (list != null)
                {

                }
                return View();
            }
        }

        public ActionResult CreateAuction()
        {
            var model = new AdvertisementViewModel();

            return View(model);
        }

        [HttpPost]
        public ActionResult PostAuction(AdvertisementViewModel model)
        {
            return RedirectToAction("Index");
        }
    }
}