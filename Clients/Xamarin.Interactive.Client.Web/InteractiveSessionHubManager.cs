//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Xamarin.Interactive.Client.Web.Models;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Completion;
using Xamarin.Interactive.CodeAnalysis.Hover;
using Xamarin.Interactive.CodeAnalysis.SignatureHelp;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Session;

namespace Xamarin.Interactive.Client.Web
{
    sealed class InteractiveSessionHubManager : DefaultHubLifetimeManager<InteractiveSessionHub>
    {
        internal sealed class SessionState : IDisposable
        {
            public InteractiveSession Session { get; set; }
            public Observer<ICodeCellEvent> EvaluationEventObserver { get; set; }
            public CompletionController CompletionController { get; set; }
            public HoverController HoverController { get; set; }
            public SignatureHelpController SignatureHelpController { get; set; }

            public void Dispose ()
            {
                Session?.Dispose ();
                Session = null;
            }
        }

        readonly ConcurrentDictionary<InteractiveSessionId, SessionState> sessions
            = new ConcurrentDictionary<InteractiveSessionId, SessionState> ();

        public override Task OnConnectedAsync (HubConnectionContext connection)
        {
            sessions.TryAdd (connection.ConnectionId, new SessionState ());

            return base.OnConnectedAsync (connection);
        }

        public override Task OnDisconnectedAsync (HubConnectionContext connection)
        {
            if (sessions.TryRemove (connection.ConnectionId, out var sessionState))
                sessionState.Dispose ();

            return base.OnDisconnectedAsync (connection);
        }

        internal void BindClientSession (InteractiveSession session)
        {
            if (sessions.TryGetValue (session.SessionId, out var sessionState))
                sessionState.Session = session;
        }

        internal void SendStatusUIAction (
            ClientConnectionId connectionId,
            StatusUIAction action,
            Message message = null)
            => SendConnectionAsync (
                connectionId,
                "StatusUIAction",
                new object [] { action, message }).Forget ();

        internal SessionState GetSession (InteractiveSessionId sessionId)
            => sessions.TryGetValue (sessionId, out var session) ? session : null;
    }
}