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
using System.Threading;
using WindowsUpdateApiController.Helper;

namespace WcfWuRemoteServiceUnitTest
{
    [TestClass]
    public class LockWithLogTest
    {
        [TestMethod]
        public void Should_PreventRaceCondition_When_UseLockWithLog()
        {
            LockObj locker = new LockObj("test locker");
            int count = 100;
            Action plusOne = () => { using (LockWithLog.Lock(locker)) { count++; }};
            Action incrementer = () =>
            {
                for (int i = 0; i <= 100; i++)
                {
                    using (LockWithLog.Lock(locker))
                    {
                        count++;
                        Thread.Sleep(1);
                    }

                }
            };
            Action decrementer = () =>
            {
                for (int i = 0; i <= 100; i++)
                {
                    using (LockWithLog.Lock(locker))
                    {
                        count--;
                        Thread.Sleep(1);
                    }

                }
            };
            
            Thread thread1 = new Thread(new ThreadStart(incrementer));
            Thread thread2 = new Thread(new ThreadStart(decrementer));
            Thread thread3 = new Thread(new ThreadStart(plusOne));
            thread1.Start();
            thread2.Start();
            thread3.Start();

            Assert.IsTrue(thread1.Join(20000), "Thread is blocked.");
            Assert.IsTrue(thread2.Join(20000), "Thread is blocked.");
            Assert.IsTrue(thread3.Join(20000), "Thread is blocked.");
            Assert.AreEqual(101, count);
        }
    }
}
