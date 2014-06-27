﻿using System;
using System.Collections.Generic;
using System.Linq;

using BetterCms.Core.DataContracts.Enums;
using BetterCms.Core.Mvc.Commands;
using BetterCms.Core.Security;

using BetterCms.Module.MediaManager.Services;
using BetterCms.Module.MediaManager.ViewModels;
using BetterCms.Module.Pages.Content.Resources;
using BetterCms.Module.Pages.Models;
using BetterCms.Module.Pages.Services;
using BetterCms.Module.Pages.ViewModels.Page;

using BetterCms.Module.Root;
using BetterCms.Module.Root.Models;
using BetterCms.Module.Root.Mvc;
using BetterCms.Module.Root.Mvc.Helpers;
using BetterCms.Module.Root.Services;
using BetterCms.Module.Root.ViewModels.Security;

using NHibernate.Linq;

using ITagService = BetterCms.Module.Pages.Services.ITagService;

namespace BetterCms.Module.Pages.Command.Page.GetPageProperties
{
    /// <summary>
    /// Command for getting view model with page properties
    /// </summary>
    public class GetPagePropertiesCommand : CommandBase, ICommand<Guid, EditPagePropertiesViewModel>
    {
        /// <summary>
        /// The category service
        /// </summary>
        private ICategoryService categoryService;

        /// <summary>
        /// The language service
        /// </summary>
        private ILanguageService languageService;

        /// <summary>
        /// The tag service
        /// </summary>
        private readonly ITagService tagService;
        
        /// <summary>
        /// The options service
        /// </summary>
        private readonly IOptionService optionService;

        /// <summary>
        /// The CMS configuration
        /// </summary>
        private readonly ICmsConfiguration cmsConfiguration;
        
        /// <summary>
        /// The layout service
        /// </summary>
        private readonly ILayoutService layoutService;

        /// <summary>
        /// The file URL resolver
        /// </summary>
        private readonly IMediaFileUrlResolver fileUrlResolver;

        /// <summary>
        /// The page service
        /// </summary>
        private readonly IPageService pageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPagePropertiesCommand" /> class.
        /// </summary>
        /// <param name="tagService">The tag service.</param>
        /// <param name="categoryService">The category service.</param>
        /// <param name="optionService">The option service.</param>
        /// <param name="cmsConfiguration">The CMS configuration.</param>
        /// <param name="layoutService">The layout service.</param>
        /// <param name="fileUrlResolver">The file URL resolver.</param>
        /// <param name="languageService">The language service.</param>
        /// <param name="pageService">The page service.</param>
        public GetPagePropertiesCommand(ITagService tagService, ICategoryService categoryService, IOptionService optionService,
            ICmsConfiguration cmsConfiguration, ILayoutService layoutService, IMediaFileUrlResolver fileUrlResolver,
            ILanguageService languageService, IPageService pageService)
        {
            this.tagService = tagService;
            this.categoryService = categoryService;
            this.languageService = languageService;
            this.optionService = optionService;
            this.cmsConfiguration = cmsConfiguration;
            this.layoutService = layoutService;
            this.fileUrlResolver = fileUrlResolver;
            this.pageService = pageService;
        }

        /// <summary>
        /// Executes the specified request.
        /// </summary>
        /// <param name="id">The page id.</param>
        /// <returns></returns>
        public EditPagePropertiesViewModel Execute(Guid id)
        {            
            var modelQuery = Repository
                .AsQueryable<PageProperties>()
                .Where(p => p.Id == id)
                .Select(page =>
                    new
                    {
                        Model = new EditPagePropertiesViewModel
                          {
                              Id = page.Id,
                              Version = page.Version,
                              PageName = page.Title,
                              PageUrl = page.PageUrl,
                              PageCSS = page.CustomCss,
                              PageJavascript = page.CustomJS,
                              UseNoFollow = page.UseNoFollow,
                              UseNoIndex = page.UseNoIndex,
                              UseCanonicalUrl = page.UseCanonicalUrl,
                              IsPagePublished = page.Status == PageStatus.Published,
                              IsArchived = page.IsArchived,
                              IsMasterPage = page.IsMasterPage,
                              TemplateId = page.Layout.Id,
                              MasterPageId = page.MasterPage.Id,
                              CategoryId = page.Category.Id,
                              LanguageId = page.Language.Id,
                              AccessControlEnabled = cmsConfiguration.Security.AccessControlEnabled,
                              Image = page.Image == null || page.Image.IsDeleted ? null :
                                  new ImageSelectorViewModel
                                          {
                                              ImageId = page.Image.Id,
                                              ImageVersion = page.Image.Version,
                                              ImageUrl = fileUrlResolver.EnsureFullPathUrl(page.Image.PublicUrl),
                                              ThumbnailUrl = fileUrlResolver.EnsureFullPathUrl(page.Image.PublicThumbnailUrl),
                                              ImageTooltip = page.Image.Caption,
                                              FolderId = page.Image.Folder != null ? page.Image.Folder.Id : (Guid?)null
                                          },
                              SecondaryImage = page.SecondaryImage == null || page.SecondaryImage.IsDeleted ? null :
                                  new ImageSelectorViewModel
                                          {
                                              ImageId = page.SecondaryImage.Id,
                                              ImageVersion = page.SecondaryImage.Version,
                                              ImageUrl = fileUrlResolver.EnsureFullPathUrl(page.SecondaryImage.PublicUrl),
                                              ThumbnailUrl = fileUrlResolver.EnsureFullPathUrl(page.SecondaryImage.PublicThumbnailUrl),
                                              ImageTooltip = page.SecondaryImage.Caption,
                                              FolderId = page.SecondaryImage.Folder != null ? page.SecondaryImage.Folder.Id : (Guid?)null
                                          },
                              FeaturedImage = page.FeaturedImage == null || page.FeaturedImage.IsDeleted ? null :
                                  new ImageSelectorViewModel
                                          {
                                              ImageId = page.FeaturedImage.Id,
                                              ImageVersion = page.FeaturedImage.Version,
                                              ImageUrl = fileUrlResolver.EnsureFullPathUrl(page.FeaturedImage.PublicUrl),
                                              ThumbnailUrl = fileUrlResolver.EnsureFullPathUrl(page.FeaturedImage.PublicThumbnailUrl),
                                              ImageTooltip = page.FeaturedImage.Caption,
                                              FolderId = page.FeaturedImage.Folder != null ? page.FeaturedImage.Folder.Id : (Guid?)null
                                          }
                          },
                        LanguageGroupIdentifier = page.LanguageGroupIdentifier
                    })
                .ToFuture();

            var inSitemapFuture = Repository.AsQueryable<SitemapNode>().Where(node => node.Page.Id == id && !node.IsDeleted && !node.Sitemap.IsDeleted).Select(node => node.Id).ToFuture();
            var tagsFuture = tagService.GetPageTagNames(id);
            var categories = categoryService.GetCategories();
            var customOptionsFuture = optionService.GetCustomOptionsFuture();
            var languagesFuture = (cmsConfiguration.EnableMultilanguage) ? languageService.GetLanguages() : null;

            IEnumerable<AccessRule> userAccessFuture;
            if (cmsConfiguration.Security.AccessControlEnabled)
            {
                userAccessFuture = Repository
                    .AsQueryable<Root.Models.Page>()
                    .Where(x => x.Id == id && !x.IsDeleted)
                    .SelectMany(x => x.AccessRules)
                    .OrderBy(x => x.Identity)
                    .ToFuture();
            }
            else
            {
                userAccessFuture = null;
            }

            var model = modelQuery.ToList().FirstOrDefault();
            if (model != null && model.Model != null)
            {
                if (model.Model.IsMasterPage)
                {
                    AccessControlService.DemandAccess(Context.Principal, RootModuleConstants.UserRoles.Administration);
                }
                else
                {
                    AccessControlService.DemandAccess(Context.Principal, RootModuleConstants.UserRoles.MultipleRoles(RootModuleConstants.UserRoles.EditContent, RootModuleConstants.UserRoles.PublishContent));
                }

                model.Model.Tags = tagsFuture.ToList();
                model.Model.RedirectFromOldUrl = true;
                model.Model.Categories = categories;
                var urlHash = model.Model.PageUrl.UrlHash();
                model.Model.IsInSitemap = inSitemapFuture.Any() || Repository.AsQueryable<SitemapNode>().Any(node => node.UrlHash == urlHash && !node.IsDeleted && !node.Sitemap.IsDeleted);
                model.Model.UpdateSitemap = true;
                model.Model.CustomOptions = customOptionsFuture.ToList();
                model.Model.ShowTranslationsTab = cmsConfiguration.EnableMultilanguage && !model.Model.IsMasterPage;
                if (model.Model.ShowTranslationsTab)
                {
                    model.Model.Languages = languagesFuture.ToList();
                    if (!model.Model.Languages.Any())
                    {
                        model.Model.TranslationMessages = new UserMessages();
                        model.Model.TranslationMessages.AddInfo(PagesGlobalization.EditPageProperties_TranslationsTab_NoLanguagesCreated_Message);
                    }
                    if (model.LanguageGroupIdentifier.HasValue)
                    {
                        model.Model.Translations = pageService.GetPageTranslations(model.LanguageGroupIdentifier.Value).ToList();
                    }
                }

                // Get layout options, page options and merge them
                model.Model.OptionValues = optionService.GetMergedMasterPagesOptionValues(model.Model.Id, model.Model.MasterPageId, model.Model.TemplateId);

                if (userAccessFuture != null)
                {
                    model.Model.UserAccessList = userAccessFuture.Select(x => new UserAccessViewModel(x)).ToList();

                    var rules = model.Model.UserAccessList.Cast<IAccessRule>().ToList();

                    SetIsReadOnly(model.Model, rules);
                }

                model.Model.CanPublishPage = SecurityService.IsAuthorized(Context.Principal, RootModuleConstants.UserRoles.PublishContent);

                // Get templates
                model.Model.Templates = layoutService.GetAvailableLayouts(id).ToList();
                model.Model.Templates
                    .Where(x => x.TemplateId == model.Model.TemplateId || x.TemplateId == model.Model.MasterPageId)
                    .Take(1).ToList()
                    .ForEach(x => x.IsActive = true);
            }

            return model != null ? model.Model : null;
        }
    }
}