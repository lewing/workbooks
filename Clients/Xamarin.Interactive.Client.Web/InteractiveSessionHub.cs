//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Xamarin.Interactive.Client.Monaco;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.SignatureHelp;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Session;

namespace Xamarin.Interactive.Client.Web
{
    sealed class InteractiveSessionHub : Hub
    {
        readonly IServiceProvider serviceProvider;

        public InteractiveSessionHub (IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public override Task OnConnectedAsync ()
        {
            serviceProvider
                .GetInteractiveSessionHubManager ()
                .OnConnectedAsync (Context.Connection);

            return base.OnConnectedAsync ();
        }

        public override Task OnDisconnectedAsync (Exception exception)
        {
            serviceProvider
                .GetInteractiveSessionHubManager ()
                .OnDisconnectedAsync (Context.Connection);

            return base.OnDisconnectedAsync (exception);
        }

        public IEnumerable<WorkbookAppInstallation> GetAvailableWorkbookTargets ()
            => WorkbookAppInstallation.All;

        public async Task OpenSession (InteractiveSessionDescription sessionDescription)
        {
            var hubManager = serviceProvider.GetInteractiveSessionHubManager ();

            var session = InteractiveSession.CreateWorkbookSession (Context.ConnectionId);

            session.Status.Subscribe (
                new Observer<InteractiveSessionStatus> (status =>
                    hubManager.SendConnectionAsync (
                        session.SessionId,
                        "SessionStatusEvent",
                        new [] { new { status } }).Forget ()));

            await session.InitializeAsync (
                sessionDescription,
                Context.Connection.ConnectionAbortedToken);

            session.EvaluationService.Events.Subscribe (
                new Observer<ICodeCellEvent> (evnt =>
                    hubManager.SendConnectionAsync (
                        session.SessionId,
                        "CodeCellEvent",
                        new [] { evnt }).Forget ()));

            hubManager.BindClientSession (session);
        }

        InteractiveSession GetSession ()
            => serviceProvider
                .GetInteractiveSessionHubManager ()
                .GetSession (Context.ConnectionId)
                .Session;

        public Task<CodeCellId> InsertCodeCell (
            string initialBuffer,
            string relativeToCodeCellId,
            bool insertBefore)
            => GetSession ().EvaluationService.InsertCodeCellAsync (
                initialBuffer,
                relativeToCodeCellId,
                insertBefore,
                Context.Connection.ConnectionAbortedToken);

        public Task<CodeCellUpdatedEvent> UpdateCodeCell (
            string codeCellId,
            string updatedBuffer)
            => GetSession ().EvaluationService.UpdateCodeCellAsync (
                codeCellId,
                updatedBuffer,
                Context.Connection.ConnectionAbortedToken);

        public Task Evaluate (string targetCodeCellId, bool evaluateAll)
            => GetSession ().EvaluationService.EvaluateAsync (
                targetCodeCellId,
                evaluateAll,
                Context.Connection.ConnectionAbortedToken);

        public Task<MonacoHover> GetHover (
            string codeCellId,
            Position position)
            => GetSession ().WorkspaceService.GetHoverAsync (
                codeCellId,
                position,
                Context.Connection.ConnectionAbortedToken);

        public Task<IEnumerable<MonacoCompletionItem>> GetCompletions (
            CodeCellId codeCellId,
            Position position)
            => GetSession ().WorkspaceService.GetCompletionsAsync (
                codeCellId,
                position,
                Context.Connection.ConnectionAbortedToken);

        public Task<SignatureHelpViewModel> GetSignatureHelp (
            CodeCellId codeCellId,
            Position position)
            => GetSession ().WorkspaceService.GetSignatureHelpAsync (
                codeCellId,
                position,
                Context.Connection.ConnectionAbortedToken);

        public async Task<IReadOnlyList<InteractivePackageDescription>> InstallPackages (
            IReadOnlyList<InteractivePackageDescription> packages)
        {
            var packageManagerService = GetSession ().PackageManagerService;
            await packageManagerService.InstallAsync (
                packages,
                Context.Connection.ConnectionAbortedToken);
            return packageManagerService.GetInstalledPackages ();
        }
    }
}