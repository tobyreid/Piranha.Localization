using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Web;
using System.Web.Script.Serialization;
using Piranha.Areas.Manager.Views.Shared.EditorTemplates;
using Piranha.Data;
using Piranha.Localization.Dto;
using Piranha.Localization.Entities;

namespace Piranha.Localization
{
	public static class Localizer
	{
		/// <summary>
		/// Localizes the client page model depending on the current UI culture.
		/// </summary>
		/// <param name="model">The page model</param>
		public static void LocalizePageModel(Models.PageModel model) {
			var def = Utils.GetDefaultCulture();
			
			//
			// Check that we have a culture other than the default culture
			//
			if (def.Name != CultureInfo.CurrentUICulture.Name)
			{
			    var cachename = string.Format("Localizer_{0}_{1}", CultureInfo.CurrentUICulture.Name, model.Page.Id);
			    

			    PageTranslation translation;
			    if (!Application.Current.CacheProvider.Contains(cachename))
			    {

			        using (var db = new Db())
			        {
			            translation = db.PageTranslations
			                .Include(p => p.Regions)
			                .SingleOrDefault(
			                    p => p.PageId == model.Page.Id && !p.IsDraft && p.Culture == CultureInfo.CurrentUICulture.Name);
			            Application.Current.CacheProvider[cachename] = translation;
			        }
			    }
                translation = (PageTranslation)Application.Current.CacheProvider[cachename];
			    var js = new JavaScriptSerializer();

			    if (translation == null) return;
					// Map page values
					((Models.Page)model.Page).Title = translation.Title;
					((Models.Page)model.Page).NavigationTitle = translation.NavigationTitle;
					((Models.Page)model.Page).Keywords = translation.Keywords;
					((Models.Page)model.Page).Description = translation.Description;
					((Models.Page)model.Page).IsHidden = translation.IsHidden;

					// Map regions
					foreach (var reg in translation.Regions)
					{
					    if (!Application.Current.CacheProvider.Contains(reg.TemplateId.ToString()))
					        Application.Current.CacheProvider[reg.TemplateId.ToString()] = Models.RegionTemplate.GetSingle(reg.TemplateId);
                        var template = (Models.RegionTemplate)Application.Current.CacheProvider[reg.TemplateId.ToString()];
						if (template != null) {
							var internalId = template.InternalId;
							var type = Extend.ExtensionManager.Current.GetType(reg.Type);
							object val;

							if (typeof(IHtmlString).IsAssignableFrom(type)) {
								val = new HtmlString(reg.Body);
							} else {
								val = js.Deserialize(reg.Body, type);

								val = ((Extend.IExtension)val).GetContent(model);
							}
							((IDictionary<string, object>)model.Regions)[internalId] = val;
						}
					}
				
			}
		}

        /// <summary>
        /// Returns a list of alternative hrefs for the sitemap
        /// </summary>
        /// <param name="sitemap">The sitemap.</param>
        /// <returns></returns>
	    public static IEnumerable<SiteMapHrefAlternative> LocalizeSitemapHrefAlternatives(Models.Sitemap sitemap)
	    {
	        var def = Utils.GetDefaultCulture();

	        yield return
	            new SiteMapHrefAlternative
	            {
	                Culture = def.Name,
	                IsHidden = sitemap.IsHidden,
	                PageId = sitemap.Id,
	                PermaLink = sitemap.Permalink
	            };
	        using (var db = new Db())
	        {
	            var translation = db.PageTranslations
	                .Include(p => p.Regions)
	                .Where(
	                    p => p.PageId == sitemap.Id && !p.IsDraft);
	            foreach (var pageTranslation in translation)
	            {
	                yield return new SiteMapHrefAlternative
	                {
	                    Culture = pageTranslation.Culture,
	                    IsHidden = pageTranslation.IsHidden,
	                    PageId = pageTranslation.Id,
	                    PermaLink = sitemap.Permalink
	                };
	            }
	        }
	    }


	    /// <summary>
		/// Loads the localized content depending on the current UI culture
		/// </summary>
		/// <param name="model">The page edit model</param>
		public static void LocalizePageOnLoad(Models.Manager.PageModels.EditModel model) {
			var def = Utils.GetDefaultCulture();
			
			//
			// Check that we have a culture other than the default culture
			//
			if (def.Name != CultureInfo.CurrentUICulture.Name) {
				var js = new JavaScriptSerializer();

				using (var db = new Db()) {
					var translation = db.PageTranslations
					    .Include(p => p.Regions).SingleOrDefault(p => p.PageId == model.Page.Id && p.IsDraft && p.Culture == CultureInfo.CurrentUICulture.Name);

					if (translation != null) {
						// Map page values
						model.Page.Title = translation.Title;
						model.Page.NavigationTitle = translation.NavigationTitle;
						model.Page.Keywords = translation.Keywords;
						model.Page.Description = translation.Description;
                        model.Page.IsHidden = translation.IsHidden;

						for (var n = 0; n < model.Regions.Count; n++) {
							var region = model.Regions[n];

							// Get the translated region
							var reg = translation.Regions.SingleOrDefault(r => r.RegionId == region.Id && r.IsDraft == region.IsDraft && r.Culture ==  CultureInfo.CurrentUICulture.Name);
							if (reg != null) {
								if (region.Body is IHtmlString) {
									region.Body = new Extend.Regions.HtmlRegion(reg.Body);
								} else {
									region.Body = (Extend.IExtension)js.Deserialize(reg.Body, region.Body.GetType());
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Stores the localized content depending on the current UI culture.
		/// </summary>
		/// <param name="model">The page edit model</param>
		/// <param name="publish">If the page should be published</param>
		public static void LocalizePageBeforeSave(Models.Manager.PageModels.EditModel model, bool publish) {
			var def = Utils.GetDefaultCulture();
			
			//
			// Check that we have a culture other than the default culture
			//
			if (def.Name != CultureInfo.CurrentUICulture.Name) {
				var old = Models.Manager.PageModels.EditModel.GetById(model.Page.Id);

				SaveModel(model, false);
				if (publish)
					SaveModel(model, true);

				// Restore original values
				if (!model.Page.IsNew) {
					model.Page.Title = old.Page.Title;
					model.Page.NavigationTitle = old.Page.NavigationTitle;
					model.Page.Keywords = old.Page.Keywords;
					model.Page.Description = old.Page.Description;
                    model.Page.IsHidden = old.Page.IsHidden;
				}

				// Restore original regions
				if (!model.Page.IsNew) {
					for (var n = 0; n < model.Regions.Count; n++) {
						model.Regions[n].Body = old.Regions[n].Body;
					}
				}	
			}
		}

		#region Private methods
		/// <summary>
		/// Saves the given model.
		/// </summary>
		/// <param name="model">The model</param>
		/// <param name="publish">The state of the model</param>
		private static void SaveModel(Models.Manager.PageModels.EditModel model, bool publish) {
            var cachename = string.Format("Localizer_{0}_{1}", CultureInfo.CurrentUICulture.Name, model.Page.Id);
            Application.Current.CacheProvider.Remove(cachename);
			var js = new JavaScriptSerializer();

			using (var db = new Db()) {
				var translation = db.PageTranslations
				    .Include(p => p.Regions).SingleOrDefault(p => p.PageId == model.Page.Id && p.IsDraft != publish && p.Culture == CultureInfo.CurrentUICulture.Name);

				if (translation == null) {
					translation = new Entities.PageTranslation
					{
						Id = Guid.NewGuid(),
						PageId = model.Page.Id,
						IsDraft = !publish,
						Culture = CultureInfo.CurrentUICulture.Name,
                        IsHidden = model.Page.IsHidden
					};
					db.PageTranslations.Add(translation);
				}

				// Map page values
				translation.Title = model.Page.Title;
				translation.NavigationTitle = model.Page.NavigationTitle;
				translation.Keywords = model.Page.Keywords;
				translation.Description = model.Page.Description;
			    translation.IsHidden = model.Page.IsHidden;

				// Delete old region translations for simplicity - no need to expire cache as new ids created.
				while (translation.Regions.Count > 0)
					db.RegionTranslations.Remove(translation.Regions[0]);

				// Map regions
				for (var n = 0; n < model.Regions.Count; n++) {
					var region = model.Regions[n];

					var reg = new Entities.RegionTranslation
					{
						Id = Guid.NewGuid(),
						PageId = translation.Id,
						RegionId = region.Id,
						TemplateId = region.RegiontemplateId,
						IsDraft = !publish,
						Type = region.Body.GetType().FullName,
						Culture = CultureInfo.CurrentUICulture.Name
					};
					translation.Regions.Add(reg);

					if (region.Body is IHtmlString)
						reg.Body = ((IHtmlString)region.Body).ToHtmlString();
					else reg.Body = js.Serialize(region.Body);
				}
				db.SaveChanges();
			}
		}
		#endregion
	}
}
