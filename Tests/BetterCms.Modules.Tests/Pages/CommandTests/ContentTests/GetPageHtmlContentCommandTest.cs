﻿using System;
using System.Security.Principal;

using Autofac;

using BetterCms.Core.DataAccess;
using BetterCms.Core.DataAccess.DataContext;
using BetterCms.Core.DataContracts.Enums;
using BetterCms.Core.Mvc.Commands;
using BetterCms.Core.Services;

using BetterCms.Module.Root.Models;
using BetterCms.Module.Root.Services;
using BetterCms.Module.Pages.Command.Content.GetPageHtmlContent;

using Moq;

using NUnit.Framework;

namespace BetterCms.Test.Module.Pages.CommandTests.ContentTests
{
    [TestFixture]
    public class GetPageHtmlContentCommandTest : TestBase
    {
        [Test]
        public void Should_Return_Page_Html_Content()
        {
            // Create html content
            var htmlContent = TestDataProvider.CreateNewHtmlContent();
            var pageContent = TestDataProvider.CreateNewPageContent(htmlContent);
            htmlContent.Status = ContentStatus.Published;

            // Create command
            var command = new GetPageHtmlContentCommand();
            command.UnitOfWork = new Mock<IUnitOfWork>().Object;
            command.Repository = new Mock<IRepository>().Object;
            command.Configuration = Container.Resolve<ICmsConfiguration>();
            command.Context = new Mock<ICommandContext>().Object;
            
            // Mock security service
            var securityMock = new Mock<ISecurityService>();
            securityMock.Setup(s => s.IsAuthorized(It.IsAny<IPrincipal>(), It.IsAny<string>())).Returns(true);
            command.SecurityService = securityMock.Object;

            // Mock content service
            var serviceMock = new Mock<IContentService>();
            serviceMock
                .Setup(f => f.GetPageContentForEdit(pageContent.Id))
                .Returns(new Tuple<PageContent, Content>(pageContent, htmlContent));
            command.ContentService = serviceMock.Object;

            // Execute command
            var result = command.Execute(pageContent.Id);                     

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Id, pageContent.Id);
            Assert.AreEqual(result.ContentId, htmlContent.Id);
            Assert.AreEqual(result.ContentName, htmlContent.Name);
            Assert.AreEqual(result.ContentVersion, htmlContent.Version);
            Assert.AreEqual(result.CurrentStatus, htmlContent.Status);
            Assert.AreEqual(result.PageId, pageContent.Page.Id);
            Assert.AreEqual(result.RegionId, pageContent.Region.Id);
            Assert.AreEqual(result.LiveFrom, htmlContent.ActivationDate);
            Assert.AreEqual(result.LiveTo, htmlContent.ExpirationDate);
        }
    }
}
