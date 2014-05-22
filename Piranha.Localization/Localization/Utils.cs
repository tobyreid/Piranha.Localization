/*
 * Copyright (c) 2014 Håkan Edling
 *
 * See the file LICENSE for copying permission.
 */

using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace Piranha.Localization
{
	/// <summary>
	/// Internal utility methods.
	/// </summary>
	internal static class Utils
	{
		/// <summary>
		/// Gets the configured default culture.
		/// </summary>
		/// <returns>The culture</returns>
		public static CultureInfo GetDefaultCulture() {
			return new CultureInfo(((GlobalizationSection)WebConfigurationManager.GetSection("system.web/globalization")).UICulture);
		}

		/// <summary>
		/// Gets the localization culture of the current request.
		/// </summary>
		/// <returns>The culture</returns>
		public static CultureInfo GetCurrentCulture() {
			var culture = (string)HttpContext.Current.Items["LOCALIZATION_CULTURE"];

			if (culture != null)
				return new CultureInfo(culture);
			return GetDefaultCulture();
		}
	}
}
