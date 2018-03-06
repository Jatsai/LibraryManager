// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class ErrorListUtil
    {
        static public readonly List<string> ErrorCodes = new List<string> { PredefinedErrors.ProviderIsUndefined().Code };

        static public __VSERRORCATEGORY ToVSERRORCATEGORY(string errorCode)
        {
            if (ErrorCodes.Contains(errorCode))
            {
                return __VSERRORCATEGORY.EC_ERROR;
            }

            return __VSERRORCATEGORY.EC_MESSAGE;
        }
    }
}
