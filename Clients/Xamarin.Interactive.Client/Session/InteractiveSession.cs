// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Session
{
    public sealed class InteractiveSession : IMessageService, IDisposable
    {
        public static InteractiveSession CreateWorkbookSession (InteractiveSessionId sessionId = default)
            => new InteractiveSession (sessionId, ClientSessionKind.Workbook, null);

        internal static InteractiveSession CreateLiveInspectionSession (
            ClientSessionUri liveInspectAgentUri,
            InteractiveSessionId sessionId = default)
            => new InteractiveSession (
                sessionId,
                ClientSessionKind.LiveInspection,
                liveInspectAgentUri
                    ?? throw new ArgumentNullException (nameof (liveInspectAgentUri)));

        readonly ClientSessionKind sessionKind;
        readonly ClientSessionUri liveInspectAgentUri;
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();

        readonly Observable<InteractiveSessionStatus> status = new Observable<InteractiveSessionStatus> ();
        public IObservable<InteractiveSessionStatus> Status => status;

        bool isDisposed;

        InteractiveSessionState state = InteractiveSessionState.Create ();
        InteractiveSessionState State {
            get {
                if (isDisposed)
                    throw new ObjectDisposedException ($"{nameof (InteractiveSession)}: {SessionId}");
                return state;
            }

            set => state = value;
        }

        public InteractiveSessionId SessionId { get; }

        internal EvaluationService EvaluationService => State.EvaluationService;
        internal IWorkspaceService WorkspaceService => State.WorkspaceService;
        internal PackageManagerService PackageManagerService => State.PackageManagerService;

        InteractiveSession (
            InteractiveSessionId sessionId,
            ClientSessionKind sessionKind,
            ClientSessionUri liveInspectAgentUri)
        {
            SessionId = sessionId == default
                ? Guid.NewGuid ()
                : sessionId;
            this.sessionKind = sessionKind;
            this.liveInspectAgentUri = liveInspectAgentUri;
        }

        public void Dispose ()
        {
            isDisposed = true;
            State = default;
            cancellationTokenSource.Cancel ();
            cancellationTokenSource.Dispose ();
        }

        CancellationToken GetCancellationToken (CancellationToken cancellationToken = default)
            => cancellationTokenSource.Token.LinkWith (cancellationToken);

        public async Task InitializeAsync (
            InteractiveSessionDescription sessionDescription,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = GetCancellationToken (cancellationToken);

            State = State.WithSessionDescription (sessionDescription);

            status.Observers.OnNext (InteractiveSessionStatus.ConnectingToAgent);

            await InitializeAgentConnectionAsync (
                cancellationToken).ConfigureAwait (false);

            status.Observers.OnNext (InteractiveSessionStatus.InitializingWorkspace);

            var workspaceConfiguration = await WorkspaceConfiguration.CreateAsync (
                State.AgentConnection,
                sessionKind,
                cancellationToken).ConfigureAwait (false);

            var workspaceService = await WorkspaceServiceFactory.CreateWorkspaceServiceAsync (
                sessionDescription.LanguageDescription,
                workspaceConfiguration,
                cancellationToken).ConfigureAwait (false);

            var evaluationService = new EvaluationService (
                workspaceService,
                sessionDescription.EvaluationEnvironment
                    ?? new EvaluationEnvironment (null));

            evaluationService.NotifyAgentConnected (state.AgentConnection);

            PackageManagerService packageManagerService = null;

            if (State.WorkbookApp?.Sdk != null) {
                packageManagerService = new PackageManagerService (
                    workspaceService.Configuration.DependencyResolver,
                    evaluationService,
                    PackageManager_GetAgentConnectionHandler);

                await packageManagerService.InitializeAsync (
                    state.WorkbookApp.Sdk,
                    state.PackageManagerService?.GetInstalledPackages (),
                    cancellationToken).ConfigureAwait (false);
            }

            State = State.WithServices (
                workspaceService,
                evaluationService,
                packageManagerService);

            status.Observers.OnNext (InteractiveSessionStatus.Ready);
        }

        #region Agent Connection

        async Task<IAgentConnection> PackageManager_GetAgentConnectionHandler (
            bool refreshForAgentIntegration,
            CancellationToken cancellationToken)
        {
            if (refreshForAgentIntegration) {
                State = state.WithAgentConnection (
                    await State
                        .AgentConnection
                        .RefreshFeaturesAsync ().ConfigureAwait (false));
                // PostEvent (ClientSessionEventKind.AgentFeaturesUpdated);
            }

            return State.AgentConnection;
        }

        public void TerminateAgentConnection ()
            => State = State.WithAgentConnection (State.AgentConnection.TerminateConnection ());

        async Task InitializeAgentConnectionAsync (CancellationToken cancellationToken = default)
        {
            void ResetAgentConnection ()
            {
                var agentType = AgentType.Unknown;

                if (State.AgentConnection != null) {
                    agentType = State.AgentConnection.Type;
                    ((IDisposable)State.AgentConnection).Dispose ();
                }

                State.EvaluationService?.NotifyAgentDisconnected ();

                State = State.WithAgentConnection (new AgentConnection (agentType));
            }

            void HandleAgentDisconnected ()
            {
                ResetAgentConnection ();
                EvaluationService?.OutdateAllCodeCells ();
            }

            ResetAgentConnection ();

            if (State.AgentConnection?.IsConnected == true)
                TerminateAgentConnection ();

            WorkbookAppInstallation workbookApp = null;

            if (sessionKind == ClientSessionKind.Workbook)
                workbookApp = WorkbookAppInstallation.LookupById (
                    State.SessionDescription.TargetPlatformIdentifier);

            var agentConnection = await State.AgentConnection.ConnectAsync (
                workbookApp,
                liveInspectAgentUri,
                this,
                HandleAgentDisconnected,
                GetCancellationToken (cancellationToken)).ConfigureAwait (false);

            await agentConnection
                .Api
                .SetLogLevelAsync (Log.GetLogLevel ()).ConfigureAwait (false);

            State = State.WithAgentConnection (
                agentConnection,
                workbookApp);
        }

        #endregion

        #region IMessageService

        bool IMessageService.CanHandleMessage (Message message)
            => true;

        Message IMessageService.PushMessage (Message message)
            => message;

        void IMessageService.DismissMessage (int messageId)
        {
        }

        #endregion
    }
}