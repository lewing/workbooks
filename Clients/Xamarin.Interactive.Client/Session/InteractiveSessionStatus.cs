//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.Session
{
    public enum InteractiveSessionStatus
    {
        Uninitialized,
        ConnectingToAgent,
        InitializingWorkspace,
        Ready,
        AgentDisconnected
    }
}