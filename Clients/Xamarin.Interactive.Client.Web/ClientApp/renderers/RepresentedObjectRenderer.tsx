//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { RepresentedResult } from '../evaluation'
import {
    ResultRenderer,
    ResultRendererRepresentation,
    ResultRendererRepresentationOptions
} from '../rendering'
import { randomReactKey } from '../utils';
import { WorkbookShellContext, WorkbookShell } from '../components/WorkbookShell';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export default function RepresentedObjectRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === RepresentedObjectRenderer.typeName)
        ? new RepresentedObjectRenderer
        : null
}

interface RepresentedObjectValue {
    $type: string
    representedType: string
    representations: any []
}

interface RepresentedObjectProps {
    object: RepresentedObjectValue
    context: WorkbookShellContext
}

export interface MyMap<K,V> {
    [K: string]: V
}

interface RepresentedObjectState {
    representations: MyMap<string, ResultRendererRepresentation>
    selectedRepresentation: string
}

class RepresentedObjectRenderer implements ResultRenderer {
    public static typeName = "Xamarin.Interactive.Representations.RepresentedObject"
    getRepresentations(result: RepresentedResult, context: WorkbookShellContext) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== RepresentedObjectRenderer.typeName)
                continue

            const object = value as RepresentedObjectValue

            reps.push({
                displayName: 'RepresentedObject',
                key: randomReactKey(),
                component: RepresentedObjectRepresentation,
                componentProps: {
                    object: object,
                    context: context
                }
            })
        }
        return reps
    }
}

export class RepresentedObjectRepresentation extends React.Component<RepresentedObjectProps, RepresentedObjectState> {
    constructor(props: RepresentedObjectProps) {
        super(props)

        const result = {
            valueRepresentations: props.object.representations,
            type: props.object.$type
        }

        const reps = props.context.rendererRegistry
            .getRenderers(result)
            .map(r => r.getRepresentations(result, props.context))

        const flatReps = reps.length === 0
            ? []
            : reps.reduce((a, b) => a.concat(b))

        const mapReps: MyMap<string, ResultRendererRepresentation> = {}
        flatReps.map((r, i) => {
            mapReps[r.key] = r
        })

        this.state = {
            representations: mapReps,
            selectedRepresentation: flatReps[0].key
        }
    }
    render() {
        const options = this.props.object.representations;
        const state = this.state
        const dropdownOptions = Object.keys(state.representations).length > 1
            ? Object.keys(state.representations).map(key => {
                return {
                    key: key,
                    text: state.representations[key].displayName
                }
            })
            : null

        let repElem = null
        if (state.selectedRepresentation) {
            const rep = state.representations[state.selectedRepresentation]
            //rep.interact && this.props.interact(state.selectedRepresentation).then(r => console.log("updated"))

            repElem = <rep.component key={randomReactKey()} {...rep.componentProps} />
        }

        return (
            <div
                className="CodeCell-result">
                <div key={randomReactKey()} className="CodeCell-result-renderer-container">
                    {repElem}
                </div>
                {dropdownOptions && <Dropdown
                    options={dropdownOptions}
                    defaultSelectedKey={state.selectedRepresentation}
                    onChanged={item => {
                       this.setState({ selectedRepresentation: item.key as string })
                    }} />}
            </div>
        )

    }
}