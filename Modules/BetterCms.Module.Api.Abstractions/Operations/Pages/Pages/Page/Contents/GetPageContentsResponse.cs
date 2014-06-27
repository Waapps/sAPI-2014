﻿using System.Runtime.Serialization;

using BetterCms.Module.Api.Infrastructure;

namespace BetterCms.Module.Api.Operations.Pages.Pages.Page.Contents
{
    [DataContract]
    public class GetPageContentsResponse : ListResponseBase<PageContentModel>
    {
    }
}