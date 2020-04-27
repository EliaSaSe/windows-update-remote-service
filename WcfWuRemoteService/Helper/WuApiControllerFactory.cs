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
using WindowsUpdateApiController;
using WindowsUpdateApiController.Helper;

namespace WcfWuRemoteService.Helper
{
    internal class WuApiControllerFactory
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        virtual public IWuApiController GetController()
        {
            WuApiController controller;
#if (!DEBUG)
            controller = new WuApiController();
#endif
#if DEBUG
            // When remove this part, remove the following references in csproj too:
            // COMReference Include="WUApiLib" Condition=" '$(Configuration)' == 'DEBUG' "
            // ProjectReference Include="..\WuApiMocks\WuApiMocks.csproj" Condition=" '$(Configuration)' == 'DEBUG' "
            Log.Warn($"This is a debug build. It uses {nameof(WuApiMocks.WuApiSimulator)} instead of {nameof(WUApiLib.UpdateSession)} to search, download and install updates. No real changes will be made.");
            var simulator = new WuApiMocks.WuApiSimulator().SetSearchTime(100).SetDownloadTime(10000).SetInstallTime(10000).Configure();
            controller = new WuApiController(simulator.UpdateSession, simulator.UpdateCollectionFactory, new SystemInfo());
#endif
            return controller;
        }
    }
}
