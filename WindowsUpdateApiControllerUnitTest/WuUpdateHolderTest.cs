/*
    Windows Update Remote Service
    Copyright(C) 2016-2020  Elia Seikritt

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.If not, see<https://www.gnu.org/licenses/>.
*/
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using WindowsUpdateApiController;
using WindowsUpdateApiController.Exceptions;
using WUApiLib;
using WuApiMocks;
using WuDataContract.DTO;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{

    [TestClass]
    public class WuUpdateHolderTest : WuApiControllerTestBase
    {

        [TestMethod, TestCategory("Settings"), TestCategory("UpdateHolder")]
        public void Should_SetupInitialSettings_When_CallConstructor()
        {
            Assert.IsTrue((new WuUpdateHolder(true)).AutoSelectUpdates);
            Assert.IsFalse((new WuUpdateHolder(false)).AutoSelectUpdates);
            Assert.IsNull((new WuUpdateHolder(true)).ApplicableUpdates);
        }

        [TestMethod, TestCategory("UpdateHolder")]
        public void Should_ReplaceApplicableUpdates_When_AddNewSearchResult()
        {
            UpdateFake update1 = new UpdateFake("update1");
            UpdateFake update2 = new UpdateFake("update2");
            var searchresult1 = ToUpdateCollection(update1);
            var searchresult2 = ToUpdateCollection(update2);

            var holder = new WuUpdateHolder();

            holder.SetApplicableUpdates(searchresult1);
            Assert.IsTrue(holder.ApplicableUpdates.OfType<IUpdate>().Single() == update1);
            holder.SetApplicableUpdates(searchresult2);
            Assert.IsTrue(holder.ApplicableUpdates.OfType<IUpdate>().Single() == update2);
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Auto select updates")]
        public void Should_ResetSelectedUpdates_When_AddNewSearchResult()
        {
            Update3Fake update1 = new Update3Fake("update1", true);
            var holder = new WuUpdateHolder();
            holder.SetApplicableUpdates(ToUpdateCollection(update1));
            holder.SelectUpdate("update1");
            Assert.IsNotNull(holder.GetSelectedUpdates().Single());
            holder.SetApplicableUpdates(ToUpdateCollection(update1));
            Assert.IsFalse(holder.GetSelectedUpdates().Any());
        }

        [TestMethod, TestCategory("UpdateHolder")]
        public void Should_ReturnTrue_When_VerifySelectedUpdate()
        {
            UpdateFake update1 = new UpdateFake("update1");
            var holder = new WuUpdateHolder();
            holder.SetApplicableUpdates(ToUpdateCollection(update1));        
            Assert.IsFalse(holder.IsSelected("update1"));
            holder.SelectUpdate("update1");
            Assert.IsTrue(holder.IsSelected("update1"));
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("No Null")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Should_NotAllowNull_When_AddNewSearchResult()
        {
            var holder = new WuUpdateHolder();
            holder.SetApplicableUpdates(null);
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Auto select updates")]
        public void Should_ApplyUpdateSelection_When_UseValidUpdateId()
        {
            UpdateFake update1 = new UpdateFake("update1");
            UpdateFake update2 = new UpdateFake("update2");
            UpdateFake update3 = new UpdateFake("update3");
            var holder = new WuUpdateHolder();
            holder.SetApplicableUpdates(ToUpdateCollection(update1, update2, update3));
            holder.SelectUpdate("update2");
            Assert.IsTrue(holder.GetSelectedUpdates().Single() == update2);
            holder.UnselectUpdate("update2");
            Assert.IsFalse(holder.GetSelectedUpdates().Any());
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("UpdateDescription")]
        public void Should_ConvertValues_When_ConvertUpdateToTransferObject()
        {
            Update3Fake update = new Update3Fake("update1", false);
            update.IsInstalled = false;
            update.Identity = CommonMocks.GetUpdateIdentity("update");
            update.Description = "some text about me";
            update.IsDownloaded = false;
            update.MaxDownloadSize = 5;
            update.MinDownloadSize = 1;
            update.Title = "i am the update";
            update.EulaAccepted = true;

            IUpdateCollection updateCollection = ToUpdateCollection(update);

            var session = new UpdateSessionFake(true);
            session.SearcherMock.FakeSearchResult = CommonMocks.GetSearchResult(updateCollection);
            using (WuApiController wu = new WuApiController(session, UpdateCollectionFactory, SystemInfo))
            {
                wu.AutoSelectUpdates = true;
                wu.BeginSearchUpdates();
                WaitForStateChange(wu, WuStateId.SearchCompleted);

                UpdateDescription ud = wu.GetAvailableUpdates().Single();

                Assert.AreEqual(ud.IsImportant, !update.BrowseOnly);
                Assert.AreEqual(ud.Description, update.Description);
                Assert.AreEqual(ud.ID, update.Identity.UpdateID);
                Assert.AreEqual(ud.IsDownloaded, update.IsDownloaded);
                Assert.AreEqual(ud.IsInstalled, update.IsInstalled);
                Assert.AreEqual(ud.MaxByteSize, update.MaxDownloadSize);
                Assert.AreEqual(ud.MinByteSize, update.MinDownloadSize);
                Assert.AreEqual(ud.Title, update.Title);
                Assert.AreEqual(ud.EulaAccepted, update.EulaAccepted);
                Assert.AreEqual(ud.SelectedForInstallation, !update.BrowseOnly);
            }
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("UpdateDescription")]
        public void Should_BeOptional_When_UpdateIsBrowseOnly()
        {
            Update3Fake update = new Update3Fake("update1", true);
            var holder = new WuUpdateHolder();
            var ud = holder.ToUpdateDescription(update);
            Assert.IsFalse(ud.IsImportant);
        }


        [TestMethod, TestCategory("UpdateHolder"), TestCategory("UpdateDescription")]
        public void Should_BeImportant_When_UpdateIsNotBrowseOnly()
        {
            Update3Fake update = new Update3Fake("update1", false);
            var holder = new WuUpdateHolder();
            var ud = holder.ToUpdateDescription(update);
            Assert.IsTrue(ud.IsImportant);
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("UpdateDescription")]
        public void Should_BeImportant_When_UpdateIsMandatory()
        {
            UpdateFake update = new UpdateFake("update1");
            update.IsMandatory = true;
            var holder = new WuUpdateHolder();
            var ud = holder.ToUpdateDescription(update);
            Assert.IsTrue(ud.IsImportant);
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("UpdateDescription")]
        public void Should_BeImportant_When_UpdateIsAutoSelectOnWebSites()
        {
            UpdateFake update = new UpdateFake("update1", false);
            update.AutoSelectOnWebSites = true;
            var holder = new WuUpdateHolder();
            var ud = holder.ToUpdateDescription(update);
            Assert.IsTrue(ud.IsImportant);
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Exception")]
        [ExpectedException(typeof(UpdateNotFoundException))]
        public void Should_ThrowException_When_SelectUnkownUpdate()
        {
            UpdateFake update1 = new UpdateFake("update1");
            var holder = new WuUpdateHolder();
            holder.SetApplicableUpdates(ToUpdateCollection(update1));
            holder.SelectUpdate("update2");
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Exception")]
        [ExpectedException(typeof(UpdateNotFoundException))]
        public void Should_ThrowException_When_UnselectUnkownUpdate()
        {
            UpdateFake update1 = new UpdateFake("update1");
            var holder = new WuUpdateHolder();
            holder.SetApplicableUpdates(ToUpdateCollection(update1));
            holder.UnselectUpdate("update2");
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Exception")]
        [ExpectedException(typeof(UpdateNotFoundException))]
        public void Should_ThrowException_When_SelectAndNoApplUpdates()
        {
            var holder = new WuUpdateHolder();
            holder.SelectUpdate("update2");
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Exception")]
        [ExpectedException(typeof(UpdateNotFoundException))]
        public void Should_ThrowException_When_UnselectAndNoApplUpdates()
        {
            var holder = new WuUpdateHolder();
            holder.UnselectUpdate("update2");
        }

        [TestMethod, TestCategory("UpdateHolder")]
        public void Should_ReturnEmtpyEnum_When_NoUpdatesAreSelected()
        {
            UpdateFake update1 = new UpdateFake("update1");
            var holder = new WuUpdateHolder();
            Assert.IsFalse(holder.GetSelectedUpdates().Any());
            holder.SetApplicableUpdates(ToUpdateCollection(update1));
            Assert.IsFalse(holder.GetSelectedUpdates().Any());
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Auto select updates")]
        public void Should_SelectUpdates_When_AutoSelectIsEnabled()
        {
            UpdateFake update1 = new UpdateFake("update1");
            UpdateFake update2 = new UpdateFake("update2");
            UpdateFake update3 = new UpdateFake("update3");
            update2.IsMandatory = true;
            update3.IsMandatory = true;
            var holder = new WuUpdateHolder();
            holder.AutoSelectUpdates = true;
            holder.SetApplicableUpdates(ToUpdateCollection(update1, update2, update3));
            Assert.IsFalse(holder.GetSelectedUpdates().Contains(update1));
            Assert.IsTrue(holder.GetSelectedUpdates().Contains(update2));
            Assert.IsTrue(holder.GetSelectedUpdates().Contains(update3));
        }

        [TestMethod, TestCategory("UpdateHolder"), TestCategory("Auto select updates")]
        public void Should_NotSelectUpdates_When_AutoSelectIsDisabled()
        {
            UpdateFake update1 = new UpdateFake("update1");
            UpdateFake update2 = new UpdateFake("update2");
            UpdateFake update3 = new UpdateFake("update3");
            update2.IsMandatory = true;
            update3.IsMandatory = true;
            var holder = new WuUpdateHolder();
            holder.AutoSelectUpdates = false;
            holder.SetApplicableUpdates(ToUpdateCollection(update1, update2, update3));
            Assert.IsFalse(holder.GetSelectedUpdates().Any());
        }

    }
}
