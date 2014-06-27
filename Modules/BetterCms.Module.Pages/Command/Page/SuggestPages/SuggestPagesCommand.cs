﻿using System;
using System.Collections.Generic;
using System.Linq;

using BetterCms.Core.DataAccess;
using BetterCms.Core.Mvc.Commands;
using BetterCms.Module.Pages.Models;
using BetterCms.Module.Pages.Services;
using BetterCms.Module.Pages.ViewModels.Page;

using BetterCms.Module.Root.Mvc;

namespace BetterCms.Module.Pages.Command.Page.SuggestPages
{
    /// <summary>
    /// A command for getting list of pages by filter.
    /// </summary>
    public class SuggestPagesCommand : CommandBase, ICommand<PageSuggestionViewModel, List<PageLookupKeyValue>>
    {
        /// <summary>
        /// The configuration
        /// </summary>
        private readonly ICmsConfiguration configuration;

        /// <summary>
        /// The page service
        /// </summary>
        private readonly IPageService pageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuggestPagesCommand" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="pageService">The page service.</param>
        public SuggestPagesCommand(ICmsConfiguration configuration, IPageService pageService)
        {
            this.configuration = configuration;
            this.pageService = pageService;
        }

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>
        /// A list of pages.
        /// </returns>
        public List<PageLookupKeyValue> Execute(PageSuggestionViewModel model)
        {
            var query = Repository.AsQueryable<PageProperties>()
                   .Where(page => page.Title.Contains(model.Query) || page.PageUrl.Contains(model.Query));

            if (model.ExistingItemsArray.Length > 0)
            {
                var ids = new List<Guid>();
                foreach (string idValue in model.ExistingItemsArray)
                {
                    var guid = idValue.ToGuidOrDefault();
                    if (!guid.HasDefaultValue())
                    {
                        ids.Add(guid);
                    }
                }
                if (ids.Any())
                {
                    query = query.Where(page => !ids.Contains(page.Id));
                }
            }

            if (model.ExcludedLanguageId.HasValue)
            {
                var languageProxy = Repository.AsProxy<Root.Models.Language>(model.ExcludedLanguageId.Value);
                query = query.Where(q => (q.Language != languageProxy || q.Language == null));
            }

            if (!model.IncludeMasterPages)
            {
                query = query.Where(q => !q.IsMasterPage);
            }
            
            var predicateBuilder = PredicateBuilder.False<PageProperties>();
            if (model.OnlyUntranslatedPages)
            {
                predicateBuilder = predicateBuilder.Or(page => page.LanguageGroupIdentifier == null);
            }
            var includeIds = model.ExcplicitlyIncludedPagesArray;
            if (includeIds.Any())
            {
                predicateBuilder = predicateBuilder.Or(page => includeIds.Contains(page.Id));
            }
            query = query.Where(predicateBuilder);

            if (configuration.Security.AccessControlEnabled)
            {
                IEnumerable<Guid> deniedPages = pageService.GetDeniedPages();
                foreach (var deniedPageId in deniedPages)
                {
                    query = query.Where(f => f.Id != deniedPageId);
                }
            }

            return query.OrderBy(page => page.Title)
                .Select(page => new PageLookupKeyValue
                                    {
                                        Key = page.Id.ToString().ToLowerInvariant(),
                                        Value = page.Title,
                                        LanguageId = page.Language != null ? page.Language.Id : (Guid?) null,
                                        PageUrl = page.PageUrl
                                    })
                .ToList();
        }
    }
}