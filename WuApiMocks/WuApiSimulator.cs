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
    public class WuApiSimulator
    {
        Random _rnd = new Random();
        UpdateCollectionFake _updates;
        UpdateSessionFake _uSession;
        UpdateCollectionFake.Factory _collectionFactory = new UpdateCollectionFake.Factory();

        #region behavior variables
        int _searchTimeMs = 0;
        int _downloadTimeMs = 0;
        int _installTimeMs = 0;
        #endregion
        #region configuration methods
        public WuApiSimulator SetSearchTime(int ms) => ThrowIfReady(() => { _searchTimeMs = ms; });
        public WuApiSimulator SetDownloadTime(int ms) => ThrowIfReady(() => { _downloadTimeMs = ms; });
        public WuApiSimulator SetInstallTime(int ms) => ThrowIfReady(()=> { _installTimeMs = ms; });
        #endregion

        public WuApiSimulator()
        {
            IsReady = false;
        }

        public bool IsReady {
            get; private set;
        }

        public IUpdateSession UpdateSession {
            get {
                if(!IsReady) throw new InvalidOperationException("Simulation is not configured");
                return _uSession;
            }
        }

        public UpdateCollectionFake.Factory UpdateCollectionFactory => _collectionFactory;

        private WuApiSimulator ThrowIfReady(Action valueSetter)
        {
            if (IsReady) throw new InvalidOperationException("Simulation is already configured");
            valueSetter();
            return this;
        }

        public WuApiSimulator Configure()
        {
            if (IsReady) throw new InvalidOperationException("Simulation is already configured");
            _updates = GenerateUpdateCollection();
            var uSearcher = new UpdateSearcherFake();
            uSearcher.FakeSearchResult = CommonMocks.GetSearchResult(_updates, OperationResultCode.orcSucceeded);
            uSearcher.FakeSearchTimeMs = (_searchTimeMs == 0)?_rnd.Next(10000, 20000):_searchTimeMs;
            var uDownloader = new UpdateDownloaderFake();
            uDownloader.FakeDownloadTimeMs = (_downloadTimeMs == 0) ? _rnd.Next(10000, 20000) : _downloadTimeMs;
            var uInstaller = new UpdateInstallerFake();
            uInstaller.FakeInstallTimeMs = (_installTimeMs == 0) ? _rnd.Next(10000, 20000) : _installTimeMs;
            _uSession = new UpdateSessionFake(false);
            _uSession.Downloader = uDownloader;
            _uSession.Searcher = uSearcher;
            _uSession.Installer = uInstaller;
            IsReady = true;
            return this;
        }

        private UpdateCollectionFake GenerateUpdateCollection()
        {
            int updateCount = _rnd.Next(3, 10);
            UpdateCollectionFake coll = new UpdateCollectionFake();

            for (int i = 0; i < updateCount; i++)
            {

                var update = new Update3Fake(Guid.NewGuid().ToString(), _rnd.Next(2) == 0);
                update.Title = "Simulated update " + i;
                update.Description = "This is for test scenarios. No changes on the system when this update gets installed.";
                update.MinDownloadSize = _rnd.Next(1000, 1000000);
                update.MaxDownloadSize = _rnd.Next(1000, 100000) + update.MinDownloadSize;
                update.RecommendedHardDiskSpace = (int)update.MaxDownloadSize;
                update.IsDownloaded = false;
                update.IsInstalled = false;
                coll.Add(update);
            }
            return coll;
        }

    }
}
