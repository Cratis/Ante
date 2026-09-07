// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Component, ErrorInfo, ReactNode } from 'react';
import strings from 'Strings';
import './RenderRecoveryBoundary.css';

interface RenderRecoveryBoundaryProps {
    children: ReactNode;
}

interface RenderRecoveryBoundaryState {
    hasFailed: boolean;
}

/**
 * The outermost boundary in `index.tsx` (`Cratis/Ante#21`) - wrapping every provider (PrimeReact,
 * Cratis Components, Arc) as well as the routed pages, so a render failure anywhere below, including
 * inside branding or provider initialization itself, degrades to a minimal, safe-language recovery
 * screen instead of a blank or half-rendered page.
 *
 * A React class component is the one framework integration point that requires extending a base class
 * (`getDerivedStateFromError`/`componentDidCatch` only exist on class components) - see
 * `code-quality.md`'s exception for framework extension mechanisms.
 *
 * This is Ante's own boundary rather than `@cratis/components`' `ErrorBoundary` - that one renders the
 * caught error's message and stack directly into the page and offers no recovery action, which is
 * exactly what this component must not do. Filed as `Cratis/Components#250`; revisit using the shared
 * one if/when it gains a safe, recoverable fallback.
 *
 * Deliberately does not depend on PrimeReact/Cratis Components rendering successfully: the fallback is
 * plain markup styled by its own small stylesheet with hard-coded neutral colors, so it still renders
 * correctly when the failure is inside the component library's own provider. It never surfaces the
 * caught error's message or stack to the person using it - only a generic, translated notice - because
 * a caught render error can be carrying request/form state that must not be echoed back as diagnostic
 * text. `componentDidCatch` still logs the real error to the console for whoever is watching
 * devtools/logs.
 *
 * Network and command failures are not render errors - they already have their own explicit journey
 * states (`ErrorSummary`, the timed-out/waiting phases described in `Documentation/wizards.md`) and
 * never reach this boundary; it does not, and must not, claim to catch them.
 */
export class RenderRecoveryBoundary extends Component<RenderRecoveryBoundaryProps, RenderRecoveryBoundaryState> {
    state: RenderRecoveryBoundaryState = { hasFailed: false };

    static getDerivedStateFromError(): RenderRecoveryBoundaryState {
        return { hasFailed: true };
    }

    componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
        console.error('Ante: a render error was caught by the recovery boundary.', error, errorInfo);
    }

    render(): ReactNode {
        if (!this.state.hasFailed) return this.props.children;

        const s = strings.renderRecovery;
        return (
            <div className='render-recovery' role='alert'>
                <h1 className='render-recovery__title'>{s.title}</h1>
                <p className='render-recovery__message'>{s.message}</p>
                {/* A plain, unstyled-library button - deliberately not Cratis's Button/IconButton -
                    since the component library itself may be what failed. */}
                <button type='button' className='render-recovery__action' onClick={() => window.location.reload()}>
                    {s.reload}
                </button>
            </div>
        );
    }
}
