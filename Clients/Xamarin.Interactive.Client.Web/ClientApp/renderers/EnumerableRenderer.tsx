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
    ResultRendererRepresentation
} from '../rendering'
import { randomReactKey } from '../utils';
import { WorkbookShellContext } from '../components/WorkbookShell';
import { RepresentedObjectRepresentation } from './RepresentedObjectRenderer';

export default function EnumerableRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === EnumerableRenderer.typeName)
        ? new EnumerableRenderer
        : null
}

interface EnumerableValue {
    $type: string
    handle: string
    slice: any[]
}

interface EnumerableProps {
    object: EnumerableValue
    context: WorkbookShellContext
}

class EnumerableRenderer implements ResultRenderer {
    static typeName = "Xamarin.Interactive.Representations.InteractiveEnumerable"
    getRepresentations(result: RepresentedResult, context: WorkbookShellContext) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== EnumerableRenderer.typeName)
                continue

            const object = (value as EnumerableValue)

            reps.push({
                displayName: 'Enumerable',
                key: randomReactKey(),
                component: EnumerableRepresentation,
                componentProps: {
                    object: object,
                    context: context
                }
            })
        }

        return reps
    }
    async interact(rep: ResultRendererRepresentation):
        Promise<ResultRendererRepresentation>
    {
        const props = rep.componentProps as EnumerableProps

        if (!props.context)
            return rep;

        const obj = await props.context.session.interact(props.object.handle)
        return ({
            ...rep,
            componentProps: {
                object: obj,
                context: props.context
            },
            interact: undefined
        })
    }
}

class EnumerableRepresentation extends React.Component<EnumerableProps> {
    render() {
        const slice = this.props.object.slice || []

        return (<ul>
            {Object.keys(slice).map((sliceKey, i) => {
                const obj = slice[i] as any
                const ro = obj.$type === "Xamarin.Interactive.Representations.RepresentedObject"
                const props = {
                    object: obj,
                    context: this.props.context
                }
                return (
                    [<li>{i}: {ro && <RepresentedObjectRepresentation {...props} />}</li>,
                        <ol>
                            {!ro && Object.keys(obj).map(key => {
                                return <li key={key}><b>"{key}":</b> {obj[key].toString()}</li>
                            })}
                    </ol>])
            })}
        </ul>)
    }
}