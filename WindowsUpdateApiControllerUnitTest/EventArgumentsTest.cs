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
using WindowsUpdateApiController.EventArguments;
using WuDataContract.DTO;
using WuDataContract.Enums;

namespace WindowsUpdateApiControllerUnitTest
{
    [TestClass]
    public class EventArgumentsTest
    {
        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateStateChangedEventArgs()
        {
            WuStateId s1 = WuStateId.Downloading;
            WuStateId s2 = WuStateId.DownloadCompleted;

            StateChangedEventArgs eventArgs = new StateChangedEventArgs(s1, s2);

            Assert.AreEqual(s1, eventArgs.OldState);
            Assert.AreEqual(s2, eventArgs.NewState);
        }

        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateAsyncOperationCompletedEventArgs()
        {
            WuStateId result = WuStateId.DownloadFailed;
            AsyncOperation op = AsyncOperation.Installing;

            AsyncOperationCompletedEventArgs eventArgs = new AsyncOperationCompletedEventArgs(op, result);

            Assert.AreEqual(result, eventArgs.Result);
            Assert.AreEqual(op, eventArgs.Operation);
        }

        [TestMethod]
        public void Should_ContainSpecifiedValues_When_CreateProgressChangedEventArgs()
        {
            WuStateId state = WuStateId.DownloadFailed;
            ProgressDescription progress = new ProgressDescription();

            ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs(state, progress);

            Assert.AreEqual(state, eventArgs.StateId);
            Assert.AreEqual(progress, eventArgs.Progress);
        }
    }
}
