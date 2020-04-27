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
using System;
using WUApiLib;

namespace WuApiMocks
{
    public class  UpdateSessionFake : IUpdateSession
    {
        public IUpdateDownloader Downloader { get; set; }
        public IUpdateSearcher Searcher { get; set; }
        public IUpdateInstaller Installer { get; set; }

        public UpdateSessionFake(bool createMocksForCreateMethods = false)
        {
            if (createMocksForCreateMethods)
            {
                Downloader = new UpdateDownloaderFake();
                Searcher = new UpdateSearcherFake();
                Installer = new UpdateInstallerFake();
            }
        }

        public UpdateDownloaderFake DownloaderMock
        {
            get { return Downloader as UpdateDownloaderFake; }
        }
        public UpdateSearcherFake SearcherMock
        {
            get { return Searcher as UpdateSearcherFake;}
        }
        public UpdateInstallerFake InstallerMock
        {
            get { return Installer as UpdateInstallerFake; }
        }

        #region IUpdateSession interface

        public string ClientApplicationID { get; set; }

        public bool ReadOnly => false;

        public WebProxy WebProxy
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public UpdateDownloader CreateUpdateDownloader() => (UpdateDownloader)Downloader;

        public IUpdateInstaller CreateUpdateInstaller() => Installer;

        public IUpdateSearcher CreateUpdateSearcher() => Searcher;

        #endregion
    }
}
