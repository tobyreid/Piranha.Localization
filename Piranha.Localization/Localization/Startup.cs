/*
 * Copyright (c) 2014 Håkan Edling
 *
 * See the file LICENSE for copying permission.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WebActivatorEx;
using Piranha.WebPages;

[assembly: PreApplicationStartMethod(typeof(Piranha.Localization.Startup), "PreInit")]
[assembly: PostApplicationStartMethod(typeof(Piranha.Localization.Startup), "Init")]

namespace Piranha.Localization
{
	/// <summary>
	/// Starts the localization module
	/// </summary>
	public sealed class Startup
	{
		/// <summary>
		/// Preforms pre-startup initialization.
		/// </summary>
		public static void PreInit() {
			//
			// Register the HTTP module
			//
			Microsoft.Web.Infrastructure.DynamicModuleHelper.DynamicModuleUtility.RegisterModule(typeof(Module));
		}
	
		/// <summary>
		/// Initializes the module and attaches application hooks.
		/// </summary>
		public static void Init() {
			//
			// Page translations hooks
			//
			Hooks.Model.PageModelLoaded += (model) => {
				Localizer.LocalizePageModel(model);
			};
			Hooks.Manager.PageEditModelLoaded += (controller, menu, model) => {
				Localizer.LocalizePageOnLoad(model);

				// Reset culture
				ResetCulture();

				// Reset title
				if (model.Page.IsNew)
					controller.ViewBag.Title = Piranha.Resources.Page.EditTitleNew;
				else controller.ViewBag.Title = Piranha.Resources.Page.EditTitleExisting;
			};
			Hooks.Manager.PageEditModelBeforeSave += (controller, menu, model, publish) => {
				Localizer.LocalizePageBeforeSave(model, publish);

				// Reset culture
				ResetCulture();

				// Reset title
				if (model.Page.IsNew)
					controller.ViewBag.Title = Piranha.Resources.Page.EditTitleNew;
				else controller.ViewBag.Title = Piranha.Resources.Page.EditTitleExisting;
			};

			//
			// Post translation hooks
			//
			Hooks.Model.PostModelLoaded += (model) => {
				// Do something
			};
			Hooks.Manager.PostEditModelLoaded += (controller, menu, model) => {
				// Do something
			};
			Hooks.Manager.PostEditModelBeforeSave += (controller, menu, model, publish) => { 
				// Do something
			};

			//
			// Category hooks are currently missing in the core framework.
			//

			//
			// Page edit toolbar
			//
			Hooks.Manager.Toolbar.PageEditToolbarRender += (url, str, model) => {
				str.Append(String.Format("<li><a href=\"{0}\">Default</a></li>",
					url.Action("edit", new { id = model.Page.Id })));

				foreach (var lang in Module.Languages) {
					str.Append(String.Format("<li><a href=\"{0}\">{1}</a></li>",
						"/" + lang.UrlPrefix + url.Action("edit", new { id = model.Page.Id }),
						lang.Name));
				}

				//
				// Modify the post action to the currently selected language.
				//
				if (Utils.GetDefaultCulture().Name != CultureInfo.CurrentUICulture.Name) {
					var lang = Module.Languages.Where(l => l.Culture == CultureInfo.CurrentUICulture.Name).SingleOrDefault();

					if (lang != null) {
						str.Append(
							"<script>" +
							"  $(document).ready(function() {" +
							"    var form = $($('form')[0]);" +
							"    form.attr('action', '/" + lang.UrlPrefix + "' + form.attr('action'));" +
							"  });" +
							"</script>"
							);
					}
				}
			};
		}

		private static void ResetCulture() {
			var def = Utils.GetDefaultCulture();

			if (def.Name != CultureInfo.CurrentUICulture.Name) {
				Thread.CurrentThread.CurrentCulture =
					Thread.CurrentThread.CurrentUICulture = def;
			}
		}
	}
}