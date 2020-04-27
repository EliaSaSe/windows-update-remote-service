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
using Moq;
using System;
using WUApiLib;

namespace WuApiMocks
{
    public class Update3Fake : UpdateFake, IUpdate3
    {
        public Update3Fake(string updateID, bool browseOnly) : base(updateID)
        {
            BrowseOnly = browseOnly;
        }

        public bool BrowseOnly
        {
            get; set;
        }

        public StringCollection CveIDs
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsPresent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool RebootRequired
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void CopyToCache(StringCollection pFiles)
        {
            throw new NotImplementedException();
        }
    }

    public class UpdateFake : IUpdate
    {
        IUpdateIdentity _identity;
        IInstallationBehavior _installationBehavior;
        string _title;
        decimal _maxUpdateSize = 100;
        decimal _minUpdateSize = 0;

        public UpdateFake(string updateID, bool mandatory) : this(updateID)
        {
            this.IsMandatory = mandatory;
        }

        public UpdateFake(string updateID)
        {
            _identity = CommonMocks.GetUpdateIdentity(updateID);

            var behavMock = new Mock<IInstallationBehavior>();
            behavMock.Setup(b => b.CanRequestUserInput).Returns(false);
            behavMock.Setup(b => b.Impact).Returns(InstallationImpact.iiNormal);
            behavMock.Setup(b => b.RebootBehavior).Returns(InstallationRebootBehavior.irbNeverReboots);
            _installationBehavior = behavMock.Object;
        }


        public bool AutoSelectOnWebSites { get; set; }

        public UpdateCollection BundledUpdates { get; set; }

        public bool CanRequireSource
        {
            get
            {
                return false;
            }
        }

        public ICategoryCollection Categories { get; set; }

        public dynamic Deadline
        {
            get
            {
                return String.Empty;
            }
        }

        public bool DeltaCompressedContentAvailable
        {
            get
            {
                return false;
            }
        }

        public bool DeltaCompressedContentPreferred
        {
            get
            {
                return false;
            }
        }

        public DeploymentAction DeploymentAction
        {
            get
            {
                return DeploymentAction.daNone;
            }
        }

        public string Description
        {
            get; set;
        }

        public IUpdateDownloadContentCollection DownloadContents
        {
            get
            {
                return null;
            }
        }

        public DownloadPriority DownloadPriority
        {
            get
            {
                return DownloadPriority.dpNormal;
            }
        }

        public bool EulaAccepted { get; set; }

        public string EulaText
        {
            get
            {
                return "Mock Object";
            }
        }

        public string HandlerID { get; set; }

        public IUpdateIdentity Identity
        {
            get
            {
                return _identity;
            }
            set
            {
                _identity = value;
            }
        }

        public IImageInformation Image
        {
            get
            {
                return null;
            }
        }

        public IInstallationBehavior InstallationBehavior
        {
            get { return _installationBehavior; }
            set { _installationBehavior = value; }
        }

        public bool IsBeta { get; set; }

        public bool IsDownloaded { get; set; }

        public bool IsHidden { get; set; }

        public bool IsInstalled { get; set; }

        public bool IsMandatory { get; set; }

        public bool IsUninstallable { get; set; }

        public StringCollection KBArticleIDs { get; set; }

        public StringCollection Languages { get; set; }

        public DateTime LastDeploymentChangeTime
        {
            get
            {
                return new DateTime(2000, 1, 1);
            }
        }

        public decimal MaxDownloadSize
        {
            get
            {
                return _maxUpdateSize;
            }
            set
            {
                _maxUpdateSize = value;
            }
        }

        public decimal MinDownloadSize
        {
            get
            {
                return _minUpdateSize;
            }
            set
            {
                _minUpdateSize = value;
            }
        }

        public StringCollection MoreInfoUrls { get; set; }

        public string MsrcSeverity
        {
            get
            {
                return String.Empty;
            }
        }

        public int RecommendedCpuSpeed
        {
            get { return 100; }
        }

        public int RecommendedHardDiskSpace
        {
            get; set;
        }

        public int RecommendedMemory
        {
            get { return 100; }
        }

        public string ReleaseNotes
        {
            get
            {
                return "relase note mock";
            }
        }

        public StringCollection SecurityBulletinIDs { get; set; }

        public StringCollection SupersededUpdateIDs { get; set; }

        public string SupportUrl
        {
            get
            {
                return "http://update.mock";
            }
        }

        public string Title
        {
            get
            {
                if (_title == null)
                {
                    return Identity.UpdateID;
                }
                return _title;
            }
            set
            {
                _title = value;
            }
        }

        public UpdateType Type
        {
            get
            {
                return UpdateType.utSoftware;
            }
        }

        public IInstallationBehavior UninstallationBehavior { get; set; }

        public string UninstallationNotes
        {
            get
            {
                return String.Empty;
            }
        }

        public StringCollection UninstallationSteps { get; set; }

        public void AcceptEula()
        {
            EulaAccepted = true;
        }

        public void CopyFromCache(string path, bool toExtractCabFiles)
        {
            throw new NotImplementedException();
        }
    }
}
