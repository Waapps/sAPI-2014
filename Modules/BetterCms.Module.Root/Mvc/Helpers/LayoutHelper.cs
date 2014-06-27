﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.WebPages;

using BetterCms.Core.Modules;
using BetterCms.Core.Modules.Projections;
using BetterCms.Module.Root.ViewModels.Cms;

namespace BetterCms.Module.Root.Mvc.Helpers
{
    /// <summary>
    /// A helper to render CMS page content.
    /// </summary>
    public static class LayoutHelper
    {
        /// <summary>
        /// Renders the section contents.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="webPage">The web page.</param>
        /// <param name="model">The model.</param>
        public static void RenderSectionContents(this HtmlHelper htmlHelper, WebPageBase webPage, RenderPageViewModel model)
        {
            foreach (var region in model.Regions)
            {
                var contentsBuilder = new StringBuilder();
                var projections = model.Contents.Where(c => c.RegionId == region.RegionId).OrderBy(c => c.Order).ToList();

                using (new LayoutRegionWrapper(contentsBuilder, region, model.AreRegionsEditable))
                {
                    foreach (var projection in projections)
                    {
                        // Add Html
                        using (new RegionContentWrapper(contentsBuilder, projection, model.CanManageContent && model.AreRegionsEditable))
                        {
                            var content = projection.GetHtml(htmlHelper);
                            contentsBuilder.Append(content);
                        }
                    }
                }

                var pageHtmlHelper = new PageHtmlRenderer.PageHtmlRenderer(contentsBuilder, model);
                if (model.AreRegionsEditable)
                {
                    pageHtmlHelper.ReplaceRegionRepresentationHtml();
                }
                var html = pageHtmlHelper.GetReplacedHtml().ToString();

                if (!string.IsNullOrWhiteSpace(html))
                {
                    RenderSectionAsLayoutRegion(webPage, html, region.RegionIdentifier);
                }                
            }
        }

        /// <summary>
        /// Renders the section as layout region.
        /// </summary>
        /// <param name="webPage">The web page.</param>
        /// <param name="partialViewHtml">The partial view HTML.</param>
        /// <param name="sectionName">Name of the section.</param>
        private static void RenderSectionAsLayoutRegion(WebPageBase webPage, string partialViewHtml, string sectionName)
        {
            webPage.DefineSection(
                sectionName,
                () =>
                {
                    Action<TextWriter> writerAction = tw => tw.Write(partialViewHtml);
                    var result = new HelperResult(writerAction);
                    webPage.Write(result);
                });
        }
        
        /// <summary>
        /// Renders the page custom JavaScript.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="styles">The styles.</param>
        /// <returns></returns>
        public static IHtmlString RenderPageCustomCss(this HtmlHelper htmlHelper, IEnumerable<IStylesheetAccessor> styles)
        {
            if (styles != null)
            {
                var inlineCssBuilder = new StringBuilder();
                var cssIncludesBuilder = new StringBuilder();

                foreach (var content in styles)
                {
                    var css = content.GetCustomStyles(htmlHelper);
                    if (!string.IsNullOrWhiteSpace(css))
                    {
                        inlineCssBuilder.AppendLine(css);
                    }

                    var includes = content.GetStylesResources(htmlHelper);
                    if (includes != null)
                    {
                        foreach (var include in includes)
                        {
                            cssIncludesBuilder.AppendLine(string.Format(@"<link rel=""stylesheet"" type=""text/css"" href=""{0}"" />", include));
                        }
                    }
                }

                var inlineCss = inlineCssBuilder.ToString();
                var includedCss = cssIncludesBuilder.ToString();
                if (!string.IsNullOrWhiteSpace(inlineCss) || !string.IsNullOrWhiteSpace(includedCss))
                {
                    if (!string.IsNullOrWhiteSpace(inlineCss))
                    {
                        inlineCss = string.Format(@"<style type=""text/css"">{0}</style>", inlineCss);
                    }

                    return new HtmlString(string.Concat(includedCss, inlineCss));
                }
            }

            return null;
        }
        
        /// <summary>
        /// Renders the page custom JavaScript.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="scripts">The scripts.</param>
        /// <returns></returns>
        public static IHtmlString RenderPageCustomJavaScript(this HtmlHelper htmlHelper, IEnumerable<IJavaScriptAccessor> scripts)
        {
            if (scripts != null)
            {
                var inlineJsBuilder = new StringBuilder();
                var jsIncludesBuilder = new StringBuilder();

                foreach (var content in scripts)
                {
                    var jScript = content.GetCustomJavaScript(htmlHelper);
                    if (!string.IsNullOrWhiteSpace(jScript))
                    {
                        inlineJsBuilder.Append(@"<script type=""text/javascript"" language=""javascript"">");
                        inlineJsBuilder.Append(jScript);
                        inlineJsBuilder.AppendLine(@"</script>");
                    }

                    var includes = content.GetJavaScriptResources(htmlHelper);
                    if (includes != null)
                    {
                        foreach (var include in includes)
                        {
                            jsIncludesBuilder.AppendLine(string.Format(@"<script src=""{0}"" type=""text/javascript""></script>", include));
                        }
                    }
                }

                var inlineJs = inlineJsBuilder.ToString();
                var includedJs = jsIncludesBuilder.ToString();
                if (!string.IsNullOrWhiteSpace(inlineJs) || !string.IsNullOrWhiteSpace(includedJs))
                {
                    return new HtmlString(string.Concat(includedJs, inlineJs));
                }
            }

            return null;
        }

        /// <summary>
        /// The render style sheets.
        /// </summary>
        /// <param name="htmlHelper">The html helper.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The <see cref="IHtmlString"/>.</returns>
        public static IHtmlString RenderStyleSheets<T>(this HtmlHelper htmlHelper) where T : ModuleDescriptor
        {
            return htmlHelper.Action("RenderModuleStyleSheetIncludes", "Rendering", new { moduleDescriptorType = typeof(T) });
        }

        /// <summary>
        /// Renders the body attributes.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <returns>Rendered body attributes, required for page work with CMS panel</returns>
        public static IHtmlString RenderBodyAttributes(this HtmlHelper htmlHelper)
        {
            var attributes = string.Empty;
            var model = htmlHelper.ViewContext.ViewData.Model as RenderPageViewModel;

            if (model != null && model.CanManageContent)
            {
                // Set page id
                attributes = string.Format(@" data-page-id=""{0}""", model.Id);

                // Set culture
                if (!string.IsNullOrWhiteSpace(model.LanguageCode))
                {
                    var culture = System.Globalization.CultureInfo
                        .GetCultures(System.Globalization.CultureTypes.AllCultures)
                        .FirstOrDefault(c => c.Name == model.LanguageCode);
                    
                    string cultureCode;
                    if (culture != null && !culture.IsNeutralCulture)
                    {
                        cultureCode = culture.Parent.Name;
                    }
                    else
                    {
                        cultureCode = model.LanguageCode;
                    }

                    attributes = string.Format(@"{0} data-language=""{1}""", attributes, cultureCode);
                }
            }

            return new MvcHtmlString(attributes);
        }
    }
}