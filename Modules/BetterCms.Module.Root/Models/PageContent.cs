using System;
using System.Collections.Generic;

using BetterCms.Core.DataContracts;
using BetterCms.Core.Models;
using BetterCms.Core.Security;

namespace BetterCms.Module.Root.Models
{
    [Serializable]
    public class PageContent : EquatableEntity<PageContent>, IPageContent, IAccessSecuredObjectDependency
    {
        public virtual int Order { get; set; }

        public virtual Content Content { get; set; }

        public virtual Page Page { get; set; }

        public virtual Region Region { get; set; }

        public virtual IList<PageContentOption> Options { get; set; }       

        IPage IPageContent.Page
        {
            get
            {
                return Page;
            }
        }

        IContent IPageContent.Content
        {
            get
            {
                return Content;
            }
        }

        IRegion IPageContent.Region
        {
            get
            {
                return Region;
            }
        }

        IAccessSecuredObject IAccessSecuredObjectDependency.SecuredObject
        {
            get
            {
                return Page;
            }
        }
    }
}