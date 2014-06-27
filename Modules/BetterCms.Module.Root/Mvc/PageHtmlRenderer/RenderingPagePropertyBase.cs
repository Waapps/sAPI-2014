﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterCms.Module.Root.Mvc.PageHtmlRenderer
{
    public abstract class RenderingPagePropertyBase : IRenderingPageProperty
    {
        /// <summary>
        /// The identifier
        /// </summary>
        protected readonly string identifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderingPagePropertyBase" /> class.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        public RenderingPagePropertyBase(string identifier)
        {
            this.identifier = identifier;
        }

        /// <summary>
        /// Gets the replaced HTML.
        /// </summary>
        /// <param name="stringBuilder">The string builder.</param>
        /// <param name="model">The model.</param>
        /// <returns>HTML with replaced model values</returns>
        public abstract StringBuilder GetReplacedHtml(StringBuilder stringBuilder, ViewModels.Cms.RenderPageViewModel model);

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Identifier
        {
            get
            {
                return identifier;
            }
        }

        /// <summary>
        /// Gets the string builder with replaced HTML.
        /// </summary>
        /// <param name="stringBuilder">The string builder.</param>
        /// <param name="replaceWith">The object to replace within string.</param>
        /// <returns>
        /// The string builder with replaced HTML.
        /// </returns>
        protected StringBuilder GetReplacedHtml(StringBuilder stringBuilder, string replaceWith)
        {
            foreach (var match in FindAllMatches(stringBuilder))
            {
                stringBuilder.Replace(match.GlobalMatch, replaceWith);
            }

            return stringBuilder;
        }

        /// <summary>
        /// Gets the string builder with replaced HTML.
        /// </summary>
        /// <param name="stringBuilder">The string builder.</param>
        /// <param name="replaceWith">The object to replace within string.</param>
        /// <returns>
        /// The string builder with replaced HTML.
        /// </returns>
        protected StringBuilder GetReplacedHtml(StringBuilder stringBuilder, DateTime? replaceWith)
        {
            foreach (var match in FindAllMatches(stringBuilder))
            {
                string date;
                if (replaceWith.HasValue)
                {
                    if (match.Parameters != null && match.Parameters.Length > 0)
                    {
                        try
                        {
                            date = replaceWith.Value.ToString(match.Parameters[0]);
                        }
                        catch
                        {
                            date = string.Empty;
                        }
                    }
                    else
                    {
                        date = replaceWith.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    date = null;
                }

                stringBuilder.Replace(match.GlobalMatch, date);
            }

            return stringBuilder;
        }

        /// <summary>
        /// Finds all matches within given HTML.
        /// </summary>
        /// <param name="stringBuilder">The string builder.</param>
        /// <returns>
        /// List of all found matches
        /// </returns>
        protected IEnumerable<PropertyMatch> FindAllMatches(StringBuilder stringBuilder)
        {
            var matches = new List<PropertyMatch>();
            var pattern = string.Concat("{{", identifier, "(:([^\\:\\{\\}]*))*}}");

            foreach (Match match in Regex.Matches(stringBuilder.ToString(), pattern, RegexOptions.IgnoreCase))
            {
                var propertyMatch = new PropertyMatch
                {
                    GlobalMatch = match.Value
                };
                if (match.Groups.Count > 2)
                {
                    propertyMatch.Parameters = new string[match.Groups[2].Captures.Count];
                    var i = 0;

                    foreach (Capture capture in match.Groups[2].Captures)
                    {
                        propertyMatch.Parameters[i] = capture.Value;
                        i++;
                    }
                }

                matches.Add(propertyMatch);
            }

            return matches;
        }

        /// <summary>
        /// Helper class for passing the matches between functions
        /// </summary>
        protected class PropertyMatch
        {
            public string GlobalMatch { get; set; }

            public string[] Parameters { get; set; }
        }
    }
}