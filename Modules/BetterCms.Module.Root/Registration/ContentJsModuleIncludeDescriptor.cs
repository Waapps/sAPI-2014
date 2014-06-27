﻿using BetterCms.Core.Modules;
using BetterCms.Core.Modules.Projections;
using BetterCms.Module.Root.Content.Resources;

namespace BetterCms.Module.Root.Registration
{
    /// <summary>
    /// bcms.content.js module descriptor.
    /// </summary>
    public class ContentJsModuleIncludeDescriptor : JsIncludeDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentJsModuleIncludeDescriptor" /> class.
        /// </summary>
        /// <param name="module">The container module.</param>
        public ContentJsModuleIncludeDescriptor(RootModuleDescriptor module)
            : base(module, "bcms.content")
        {

            Links = new IActionProjection[]
                {                       
                };

            Globalization = new IActionProjection[]
                {              
                    new JavaScriptModuleGlobalization(this, "showMasterPagesPath", () => RootGlobalization.MasterPagesPath_ShowPath_Button),        
                    new JavaScriptModuleGlobalization(this, "hideMasterPagesPath", () => RootGlobalization.MasterPagesPath_HidePath_Button)        
                };
        }
    }
}