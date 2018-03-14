//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import * as React from 'react'
import { RepresentedResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'
import {
    GroupedList,
    IGroup
  } from 'office-ui-fabric-react/lib/components/GroupedList/index';
  import { IColumn } from 'office-ui-fabric-react/lib/DetailsList';
  import { DetailsRow } from 'office-ui-fabric-react/lib/components/DetailsList/DetailsRow';
  import {
    FocusZone
  } from 'office-ui-fabric-react/lib/FocusZone';
  import {
    Selection,
    SelectionMode,
    SelectionZone
  } from 'office-ui-fabric-react/lib/utilities/selection/index';
import { randomReactKey } from '../utils';
import { WorkbookShellContext } from '../components/WorkbookShell';
import { RepresentedObjectRepresentation } from './RepresentedObjectRenderer';

export default function InteractiveObjectRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
        ? new InteractiveObjectRenderer
        : null
}

interface InteractiveObjectProps {
    object: InteractiveObjectValue
    context: WorkbookShellContext
}

interface InteractiveObjectValue {
    $type: string
    handle: string
    isExpanded: boolean | null
}

class InteractiveObjectRenderer implements ResultRenderer {
    getRepresentations(result: RepresentedResult, context: WorkbookShellContext) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
                continue

            const interactiveObject = value as InteractiveObjectValue
            reps.push({
                displayName: 'Object Properties',
                key: randomReactKey(),
                component: InteractiveObjectRepresentation,
                componentProps: {
                    object: interactiveObject,
                    context: context,
                },
                interact: this.interact
            })
        }
        return reps
    }
    async interact(rep: ResultRendererRepresentation):
        Promise<ResultRendererRepresentation>
    {
        const props = rep.componentProps as InteractiveObjectProps

        if (!props.context)
            return rep;

        if (props.object.isExpanded)
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

class InteractiveObjectRepresentation extends React.Component<InteractiveObjectProps, {}> {
    constructor(props: InteractiveObjectProps) {
        super(props);
    }

    render() {
        const obj = this.props.object as any
        const props = {
            object: obj,
            context: this.props.context
        }
        return (
            <ul>
                {Object.keys(obj).map(key => {
                    var member = obj[key]
                    const props = {
                        object: member,
                        context: this.props.context
                    }
                    const ro = member.$type === "Xamarin.Interactive.Representations.RepresentedObject"

                    if (ro)
                        return <li key={key}><b>"{key}":</b> <RepresentedObjectRepresentation {...props} /></li>
                    else
                        return <li key={key}><b>"{key}":</b> {member.toString()}</li>
                })}
            </ul>
        )
    }
}