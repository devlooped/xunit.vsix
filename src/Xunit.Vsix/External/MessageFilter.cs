﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using Windows.Win32.Media;
    using IMessageFilter = Windows.Win32.Media.Audio.IMessageFilter;
    using INTERFACEINFO = Windows.Win32.System.Com.INTERFACEINFO;
    using PENDINGMSG = Windows.Win32.System.Com.PENDINGMSG;
    using SERVERCALL = Windows.Win32.System.Com.SERVERCALL;

    internal class MessageFilter : IMessageFilter, IDisposable
    {
        protected const uint CancelCall = ~0U;

        private readonly MessageFilterSafeHandle _messageFilterRegistration;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _retryDelay;

        public MessageFilter()
            : this(timeout: TimeSpan.FromSeconds(60), retryDelay: TimeSpan.FromMilliseconds(150))
        {
        }

        public MessageFilter(TimeSpan timeout, TimeSpan retryDelay)
        {
            _timeout = timeout;
            _retryDelay = retryDelay;
            _messageFilterRegistration = MessageFilterSafeHandle.Register(this);
        }

        public virtual uint HandleInComingCall(uint dwCallType, HTASK htaskCaller, uint dwTickCount, in INTERFACEINFO lpInterfaceInfo)
        {
            return (uint)SERVERCALL.SERVERCALL_ISHANDLED;
        }

        public virtual uint RetryRejectedCall(HTASK htaskCallee, uint dwTickCount, uint dwRejectType)
        {
            if ((SERVERCALL)dwRejectType != SERVERCALL.SERVERCALL_RETRYLATER
                && (SERVERCALL)dwRejectType != SERVERCALL.SERVERCALL_REJECTED)
            {
                return CancelCall;
            }

            if (dwTickCount >= _timeout.TotalMilliseconds)
            {
                return CancelCall;
            }

            return (uint)_retryDelay.TotalMilliseconds;
        }

        public virtual uint MessagePending(HTASK htaskCallee, uint dwTickCount, uint dwPendingType)
        {
            return (uint)PENDINGMSG.PENDINGMSG_WAITDEFPROCESS;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageFilterRegistration.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
