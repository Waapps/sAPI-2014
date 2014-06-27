﻿using System.Collections.Generic;
using System.Linq;

using BetterCms.Core.DataAccess;
using BetterCms.Core.DataAccess.DataContext;
using BetterCms.Core.DataContracts.Enums;
using BetterCms.Core.Exceptions.Mvc;
using BetterCms.Core.Security;
using BetterCms.Core.Services;

using BetterCms.Module.Pages.Content.Resources;
using BetterCms.Module.Pages.Models;

using BetterCms.Module.Root;
using BetterCms.Module.Root.Models;
using BetterCms.Module.Root.Mvc;
using BetterCms.Module.Root.Mvc.Helpers;

using NHibernate.Linq;

namespace BetterCms.Module.Pages.Services
{
    public class DefaultPageCloneService : IPageCloneService
    {
        private readonly IPageService pageService;

        private readonly IUrlService urlService;
        
        private readonly ISecurityService securityService;

        private readonly IAccessControlService accessControlService;
        
        private readonly IRepository repository;
        
        private readonly IUnitOfWork unitOfWork;

        public DefaultPageCloneService(IPageService pageService, IUrlService urlService, ISecurityService securityService, 
            IAccessControlService accessControlService, IRepository repository, IUnitOfWork unitOfWork)
        {
            this.pageService = pageService;
            this.urlService = urlService;
            this.securityService = securityService;
            this.accessControlService = accessControlService;
            this.unitOfWork = unitOfWork;
            this.repository = repository;
        }

        public PageProperties ClonePage(System.Guid pageId, string pageTitle, string pageUrl, IEnumerable<IAccessRule> userAccessList, bool cloneAsMasterPage)
        {
            return ClonePage(pageId, pageTitle, pageUrl, userAccessList, cloneAsMasterPage, null, null);
        }

        public PageProperties ClonePageWithLanguage(System.Guid pageId, string pageTitle, string pageUrl, IEnumerable<IAccessRule> userAccessList, System.Guid languageId, System.Guid languageGroupIdentifier)
        {
            return ClonePage(pageId, pageTitle, pageUrl, userAccessList, false, languageId, languageGroupIdentifier);
        }

        private PageProperties ClonePage(System.Guid pageId, string pageTitle, string pageUrl,
            IEnumerable<IAccessRule> userAccessList, bool cloneAsMasterPage, System.Guid? languageId, System.Guid? languageGroupIdentifier)
        {
            var principal = securityService.GetCurrentPrincipal();

            if (cloneAsMasterPage)
            {
                accessControlService.DemandAccess(principal, RootModuleConstants.UserRoles.Administration);
            }
            else
            {
                accessControlService.DemandAccess(principal, RootModuleConstants.UserRoles.EditContent);
            }

            // Create / fix page url
            if (pageUrl == null && !string.IsNullOrWhiteSpace(pageTitle))
            {
                pageUrl = pageService.CreatePagePermalink(pageTitle, null);
            }
            else
            {
                pageUrl = urlService.FixUrl(pageUrl);
                pageService.ValidatePageUrl(pageUrl);
            }

            var page = repository
                .AsQueryable<PageProperties>()
                .Where(f => f.Id == pageId)
                .FetchMany(f => f.Options)
                .FetchMany(f => f.PageContents).ThenFetch(f => f.Region)
                .FetchMany(f => f.PageContents).ThenFetch(f => f.Content)
                .FetchMany(f => f.PageContents).ThenFetchMany(f => f.Options)
                .FetchMany(f => f.PageTags).ThenFetch(f => f.Tag)
                .FetchMany(f => f.MasterPages).ThenFetch(f => f.Master)
                .ToList().FirstOne();

            ValidateCloningPage(page, languageId, languageGroupIdentifier);

            unitOfWork.BeginTransaction();

            // Detach page to avoid duplicate saving.            
            repository.Detach(page);
            page.PageContents.ForEach(repository.Detach);
            page.PageTags.ForEach(repository.Detach);
            page.Options.ForEach(repository.Detach);
            page.SaveUnsecured = true;

            var pageContents = page.PageContents.Distinct().ToList();
            var pageTags = page.PageTags.Distinct().ToList();
            var pageOptions = page.Options.Distinct().ToList();

            var masterPages = page.MasterPages != null ? page.MasterPages.Distinct().ToList() : new List<MasterPage>();

            // Clone page with security
            var newPage = ClonePageOnly(page, userAccessList, pageTitle, pageUrl, cloneAsMasterPage);
            if (languageId.HasValue)
            {
                if (languageId.Value.HasDefaultValue())
                {
                    newPage.Language = null;
                }
                else
                {
                    newPage.Language = repository.AsProxy<Language>(languageId.Value);
                }
            }
            if (languageGroupIdentifier.HasValue)
            {
                newPage.LanguageGroupIdentifier = languageGroupIdentifier.Value;
            }
            else
            {
                newPage.LanguageGroupIdentifier = null;
            }
            repository.Save(newPage);

            // Clone contents.
            pageContents.ForEach(pageContent => ClonePageContent(pageContent, newPage));

            // Clone tags.
            pageTags.ForEach(pageTag => ClonePageTags(pageTag, newPage));

            // Clone options.
            pageOptions.ForEach(pageOption => ClonePageOption(pageOption, newPage));

            // Clone master pages
            masterPages.ForEach(masterPage => CloneMasterPages(masterPage, newPage));

            // Set language identifier for parent page, if it hasn't and child is cloned from the parent.
            if (languageGroupIdentifier.HasValue && !page.LanguageGroupIdentifier.HasValue)
            {
                page.LanguageGroupIdentifier = languageGroupIdentifier.Value;
                repository.Save(page);
            }

            unitOfWork.Commit();

            Events.PageEvents.Instance.OnPageCloned(newPage);

            return newPage;
        }

        /// <summary>
        /// Clones the page tags.
        /// </summary>
        /// <param name="pageTag">The page tag.</param>
        /// <param name="newPage">The new page.</param>
        private void ClonePageTags(PageTag pageTag, PageProperties newPage)
        {
            var newPageHtmlControl = new PageTag
            {
                Page = newPage,
                Tag = pageTag.Tag
            };

            repository.Save(newPageHtmlControl);
        }

        /// <summary>
        /// Clones the page only.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="userAccess">The user access.</param>
        /// <param name="newPageTitle">The new page title.</param>
        /// <param name="newPageUrl">The new page URL.</param>
        /// <param name="cloneAsMasterPage">if set to <c>true</c> clone page as master page.</param>
        /// <returns>
        /// Copy for <see cref="PageProperties" />.
        /// </returns>
        private PageProperties ClonePageOnly(PageProperties page, IEnumerable<IAccessRule> userAccess, string newPageTitle, string newPageUrl, bool cloneAsMasterPage)
        {
            var newPage = page.Duplicate();

            newPage.Title = newPageTitle;
            newPage.MetaTitle = newPageTitle;
            newPage.PageUrl = newPageUrl;
            newPage.PageUrlHash = newPageUrl.UrlHash();
            newPage.Status = PageStatus.Unpublished;

            if (cloneAsMasterPage)
            {
                newPage.IsMasterPage = true;
            }

            if (newPage.IsMasterPage)
            {
                newPage.Status = PageStatus.Published;
                newPage.Language = null;
            }

            // Add security.
            AddAccessRules(newPage, userAccess);

            return newPage;
        }

        /// <summary>
        /// Clones the content of the page.
        /// </summary>
        /// <param name="pageContent">Content of the page.</param>
        /// <param name="newPage">The new page.</param>
        private void ClonePageContent(PageContent pageContent, PageProperties newPage)
        {
            var newPageContent = new PageContent();
            newPageContent.Page = newPage;
            newPageContent.Order = pageContent.Order;
            newPageContent.Region = pageContent.Region;

            if (pageContent.Content is HtmlContentWidget || pageContent.Content is ServerControlWidget)
            {
                // Do not need to clone widgets.
                newPageContent.Content = pageContent.Content;
            }
            else
            {
                newPageContent.Content = pageContent.Content.Clone();

                var draft = pageContent.Content.History.FirstOrDefault(c => c.Status == ContentStatus.Draft && !c.IsDeleted);
                if (pageContent.Content.Status == ContentStatus.Published && draft != null)
                {
                    if (newPageContent.Content.History == null)
                    {
                        newPageContent.Content.History = new List<Root.Models.Content>();
                    }

                    var draftClone = draft.Clone();
                    draftClone.Original = newPageContent.Content;
                    newPageContent.Content.History.Add(draftClone);
                    repository.Save(draftClone);
                }
            }

            // Clone page content options.
            foreach (var option in pageContent.Options.Distinct())
            {
                if (newPageContent.Options == null)
                {
                    newPageContent.Options = new List<PageContentOption>();
                }

                var newOption = new PageContentOption
                {
                    Key = option.Key,
                    Value = option.Value,
                    Type = option.Type,
                    PageContent = newPageContent,
                    CustomOption = option.CustomOption
                };
                newPageContent.Options.Add(newOption);
                repository.Save(newOption);
            }

            repository.Save(newPageContent);
        }

        private void AddAccessRules(PageProperties newPage, IEnumerable<IAccessRule> userAccess)
        {
            if (userAccess == null)
            {
                return;
            }

            newPage.AccessRules = new List<AccessRule>();
            foreach (var rule in userAccess)
            {
                newPage.AccessRules.Add(new AccessRule
                {
                    Identity = rule.Identity,
                    AccessLevel = rule.AccessLevel,
                    IsForRole = rule.IsForRole
                });
            }
        }

        private void ClonePageOption(PageOption pageOption, PageProperties newPage)
        {
            var newPageOption = new PageOption
            {
                Key = pageOption.Key,
                Type = pageOption.Type,
                Value = pageOption.Value,
                Page = newPage,
                CustomOption = pageOption.CustomOption
            };

            if (newPage.Options == null)
            {
                newPage.Options = new List<PageOption>();
            }

            newPage.Options.Add(newPageOption);
            repository.Save(newPageOption);
        }

        private void CloneMasterPages(MasterPage masterPage, PageProperties newPage)
        {
            var newMasterPage = new MasterPage
            {
                Master = masterPage.Master,
                Page = newPage
            };

            if (newPage.MasterPages == null)
            {
                newPage.MasterPages = new List<MasterPage>();
            }

            newPage.MasterPages.Add(newMasterPage);
            repository.Save(newMasterPage);
        }

        private void ValidateCloningPage(PageProperties page, System.Guid? languageId, System.Guid? languageGroupIdentifier)
        {
            // Validate request, if cloning page with language
            if (languageGroupIdentifier.HasValue)
            {
                var query = repository.AsQueryable<Page>().Where(p => p.LanguageGroupIdentifier == languageGroupIdentifier);
                if (languageId.HasValue && !languageId.Value.HasDefaultValue())
                {
                    var language = repository.AsProxy<Language>(languageId.Value);
                    query = query.Where(p => p.Language == language);
                }
                else
                {
                    query = query.Where(p => p.Language == null);
                }

                if (query.Any())
                {
                    var logMessage = string.Format("Page already has translations for language. Id: {0}, LanguageId: {1}", page.Id, languageId);
                    throw new ValidationException(() => PagesGlobalization.ClonePageWithLanguage_PageAlreadyHasSuchTranslation_Message, logMessage);
                }
            }
        }
    }
}