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
using System.Threading;

namespace WindowsUpdateApiController.Helper
{
    /// <summary>
    /// Helperclass to log locking operations to the logfile.
    /// The purpose for this class, is to be able to find the source of a deadlock in the log files.  
    /// </summary>
    internal class LockWithLog : IDisposable
    {
        private readonly LockObj _lockObj;
        private readonly string _callerName;
        private readonly log4net.ILog _log;
        private static int _nextLogNumber = 0;
        private object _lockNumberLock = new object();
        private readonly int _lockNumber;

        private LockWithLog(LockObj lockObj, string callerName)
        {
            if (lockObj == null) throw new ArgumentNullException(nameof(lockObj));
            if (callerName == null) throw new ArgumentNullException(nameof(callerName));
            _lockObj = lockObj;
            _callerName = callerName;
            _log = log4net.LogManager.GetLogger(callerName);
            lock (_lockNumberLock)
            {
                _lockNumber = _nextLogNumber;
                _nextLogNumber++;
            }
        }

        /// <summary>
        /// Tries to acqurie a lock on <paramref name="lockObj"/> and logs this occurens in to the logfile.
        /// </summary>
        /// <param name="lockObj">The lock object to wait for.</param>
        /// <param name="callerName">Caller member name. Automatically filled out by the compiler.</param>
        /// <returns>A <see cref="LockWithLog"/> object with reference to the lock object. Call <see cref="Dispose"/> to release the lock.</returns>
        /// <example>
        /// This example shows the intended usage of this method.
        /// <code>
        /// var locker = new LockObj("Name for the lock");
        /// using(LockWithLog.Lock(locker))
        /// {
        ///  // Critical code
        /// } // Calls <see cref="Dispose"/> to release the lock.
        /// </code>
        /// </example>
        public static LockWithLog Lock(LockObj lockObj, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
            var lwl = new LockWithLog(lockObj, callerName);
            lwl._log.Debug($"{lwl._lockNumber} - Try to acquire lock {lockObj.Name}.");
            Monitor.Enter(lwl._lockObj);
            lwl._log.Debug($"{lwl._lockNumber} - Acquired lock {lockObj.Name}.");
            return lwl;
        }

        /// <summary>
        /// Releases the lock and logs this occurens in to the logfile.
        /// </summary>
        public void Dispose()
        {
            _log.Debug($"{_lockNumber} - Release lock {_lockObj.Name}.");
            Monitor.Exit(_lockObj);
        }
    }

    /// <summary>
    /// Helperclass to log locking operations to the logfile.
    /// Used by <see cref="LockWithLog"/> as locking object.
    /// </summary>
    internal class LockObj
    {
        /// <summary>
        /// Name for the lock. Will be written to the logfile.
        /// </summary>
        public string Name { get; private set; } 

        /// <param name="name">Name for the lock. Will be written to the logfile.</param>
        public LockObj(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
