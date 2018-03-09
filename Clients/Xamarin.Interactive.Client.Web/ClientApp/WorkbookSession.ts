//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { HubConnection } from '@aspnet/signalr'
import * as catalog from './i18n'
import { Event } from './utils/Events'
import { CodeCellResult, CapturedOutputSegment, ICodeCellEvent, CodeCellUpdate } from './evaluation'
import { Message, StatusUIAction, StatusUIActionWithMessage, MessageKind, MessageSeverity } from './messages'

export interface DotNetSdk {
    name: string
    profile: string
    targetFramework: string
    version: string
}

export interface WorkbookTarget {
    id: string
    flavor: string
    icon: string
    optionalFeatures: string[]
    sdk: DotNetSdk
}

export interface LanguageDescription {
    name: string
    version?: string
}

export interface SessionDescription {
    languageDescription: LanguageDescription,
    targetPlatformIdentifier: string
}

export const enum SessionStatus {
    Uninitialized = 'Uninitialized',
    ConnectingToAgent = 'ConnectingToAgent',
    InitializingWorkspace = 'InitializingWorkspace',
    Ready = 'Ready',
    AgentDisconnected = 'AgentDisconnected'
}

export interface SessionStatusEvent {
    status: SessionStatus
}

export interface PackageSource {
    source: string
}

export interface PackageDescription {
    packageId: string
    version?: string
    source?: PackageSource
}

export class WorkbookSession {
    private hubConnection = new HubConnection('/session')

    readonly sessionStatusEvent: Event<WorkbookSession, SessionStatusEvent>
    readonly statusUIActionEvent: Event<WorkbookSession, StatusUIActionWithMessage>
    readonly codeCellEvent: Event<WorkbookSession, ICodeCellEvent>

    private _availableWorkbookTargets: WorkbookTarget[] = []
    get availableWorkbookTargets() {
        return this._availableWorkbookTargets
    }

    constructor() {
        this.sessionStatusEvent = new Event(<WorkbookSession>this)
        this.statusUIActionEvent = new Event(<WorkbookSession>this)
        this.codeCellEvent = new Event(<WorkbookSession>this)

        this.hubConnection.on(
            'SessionStatusEvent',
            (e: SessionStatusEvent) => {
                console.debug('Hub: SessionStatusEvent: %O', e.status)
                this.sessionStatusEvent.dispatch(e)

                let message: StatusUIActionWithMessage = {
                    action: StatusUIAction.DisplayMessage,
                    message: {
                        kind: MessageKind.Status,
                        severity: MessageSeverity.Info,
                        showSpinner: true
                    }
                }

                switch (e.status) {
                    case SessionStatus.ConnectingToAgent:
                        message.message!.text = catalog.getString('Connecting to agent…')
                        break
                    case SessionStatus.InitializingWorkspace:
                        message.message!.text = catalog.getString('Initializing workspace…')
                        break
                    case SessionStatus.Ready:
                        message.action = StatusUIAction.DisplayIdle
                        break
                    case SessionStatus.AgentDisconnected:
                        message.message!.severity = MessageSeverity.Error
                        message.message!.text = catalog.getString('Agent disconnected')
                        message.message!.showSpinner = false
                        break
                    default:
                        message.message = undefined
                        break
                }

                if (message.message)
                    this.statusUIActionEvent.dispatch(message)
            })

        this.hubConnection.on(
            'StatusUIAction',
            (action: StatusUIAction, message: Message) => {
                console.debug('Hub: StatusUIAction: action: %O, message: %O', action, message)
                this.statusUIActionEvent.dispatch({
                    action: action,
                    message: message
                })
            })

        this.hubConnection.on(
            'CodeCellEvent',
            (e: ICodeCellEvent) => {
                console.debug('Hub: CodeCellEvent: %O: %O', e.$type, e)
                this.codeCellEvent.dispatch(e)
            })
    }

    async connect(sessionDescription: SessionDescription = {
        languageDescription: {
            name: "C#"
        },
        targetPlatformIdentifier: 'console'
    }): Promise<void> {
        await this.hubConnection.start()

        this._availableWorkbookTargets = <WorkbookTarget[]>await this.hubConnection.invoke(
            'GetAvailableWorkbookTargets')

        console.log('GetAvailableWorkbookTargets: %O', this.availableWorkbookTargets)

        await this.hubConnection.invoke(
            'OpenSession',
            sessionDescription)
    }

    disconnect(): Promise<void> {
        return this.hubConnection.stop()
    }

    insertCodeCell(buffer: string, relativeToCodeCellId: string | null): Promise<string> {
        return this.hubConnection.invoke('InsertCodeCell', buffer, relativeToCodeCellId, false)
    }

    updateCodeCell(codeCellId: string, buffer: string): Promise<CodeCellUpdate> {
        return this.hubConnection.invoke('UpdateCodeCell', codeCellId, buffer)
    }

    evaluate(codeCellId: string): Promise<void> {
        return this.hubConnection.invoke('Evaluate', codeCellId, false)
    }

    evaluateAll(): Promise<void> {
        return this.hubConnection.invoke('Evaluate', null, true)
    }

    getCompletions(codeCellId: string, position: monaco.Position): Promise<monaco.languages.CompletionItem[]> {
        return this.hubConnection.invoke("GetCompletions", codeCellId, position)
    }

    getHover(codeCellId: string, position: monaco.Position): Promise<monaco.languages.Hover> {
        return this.hubConnection.invoke("GetHover", codeCellId, position)
    }

    getSignatureHelp(codeCellId: string, position: monaco.Position): Promise<monaco.languages.SignatureHelp> {
        return this.hubConnection.invoke("GetSignatureHelp", codeCellId, position)
    }

    installPackage(packageDescription: PackageDescription): Promise<PackageDescription[]> {
        return this.hubConnection.invoke("InstallPackages", [packageDescription])
    }
}