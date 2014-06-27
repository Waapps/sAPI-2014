﻿using System;
using System.Linq;

using BetterCms.Core.Mvc.Commands;
using BetterCms.Module.Blog.Models;
using BetterCms.Module.Blog.ViewModels.Blog;
using BetterCms.Module.Root.Models;
using BetterCms.Module.Root.Mvc;

namespace BetterCms.Module.Blog.Commands.SaveDefaultTemplate
{
    public class SaveDefaultTemplateCommand : CommandBase, ICommand<DefaultTemplateViewModel, bool>
    {
        /// <summary>
        /// Executes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>True</c>, if save successful</returns>
        public bool Execute(DefaultTemplateViewModel request)
        {
            var option = Repository.AsQueryable<Option>().OrderByDescending(o => o.CreatedOn).FirstOrDefault(o => !o.IsDeleted);
            if (option == null)
            {
                option = new Option();
            }

            if (!request.MasterPageId.HasDefaultValue())
            {
                option.DefaultMasterPage = Repository.AsProxy<Page>(request.MasterPageId);
                option.DefaultLayout = null;
            }
            else
            {
                option.DefaultLayout = Repository.AsProxy<Layout>(request.TemplateId);
                option.DefaultMasterPage = null;
            }

            Repository.Save(option);
            UnitOfWork.Commit();

            return true;
        }
    }
}