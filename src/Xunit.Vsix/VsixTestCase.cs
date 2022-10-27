﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using EnvDTE;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    class VsixTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public VsixTestCase() { }

        [Obsolete]
        public VsixTestCase(IMessageSink messageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod,
            string vsVersion, string rootSuffix, bool? newIdeInstance, int timeout, bool? recycleOnFailure, bool? runOnUIThread, object[] testMethodArguments = null)
            : base(messageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            VisualStudioVersion = vsVersion;
            RootSuffix = rootSuffix;
            NewIdeInstance = newIdeInstance;
            Timeout = timeout;
            RecycleOnFailure = recycleOnFailure;

            var name = testMethod.Method.Name;
            RunOnUIThread = runOnUIThread;
        }

        public string VisualStudioVersion { get; private set; }

        public string RootSuffix { get; private set; }

        public bool? NewIdeInstance { get; private set; }

        public bool? RecycleOnFailure { get; private set; }

        public bool? RunOnUIThread { get; private set; }

        public new string SkipReason
        {
            get => base.SkipReason;
            set => base.SkipReason = value;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Register VS version as a trait, so that it can be used to group runs.
            Traits["VisualStudioVersion"] = new List<string>(new[] { VisualStudioVersion });
            Traits["RootSuffix"] = new List<string>(new[] { RootSuffix });
            Traits["Vsix"] = new List<string>(new[] { "true" });
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        {
            return base.GetDisplayName(factAttribute, displayName) + " > vs" + VisualStudioVersion + RootSuffix;
        }

        protected override string GetUniqueID()
        {
            return base.GetUniqueID() + "-" + VisualStudioVersion + RootSuffix;
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue("VisualStudioVersion", VisualStudioVersion);
            data.AddValue(nameof(IVsixAttribute.RootSuffix), RootSuffix);
            data.AddValue(nameof(IVsixAttribute.NewIdeInstance), NewIdeInstance);
            data.AddValue(nameof(IVsixAttribute.RecycleOnFailure), RecycleOnFailure);
            data.AddValue(nameof(IVsixAttribute.RunOnUIThread), RunOnUIThread);
            data.AddValue(nameof(SkipReason), SkipReason, typeof(string));
        }

        /// <inheritdoc/>
        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            VisualStudioVersion = data.GetValue<string>("VisualStudioVersion");
            RootSuffix = data.GetValue<string>(nameof(IVsixAttribute.RootSuffix));
            NewIdeInstance = data.GetValue<bool?>(nameof(IVsixAttribute.NewIdeInstance));
            RecycleOnFailure = data.GetValue<bool?>(nameof(IVsixAttribute.RecycleOnFailure));
            RunOnUIThread = data.GetValue<bool?>(nameof(IVsixAttribute.RunOnUIThread));
            SkipReason = data.GetValue<string>(nameof(SkipReason));
        }
    }
}