// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ICommandResult } from '@cratis/arc/commands';
import type { FieldValidationInfo } from '@cratis/arc.react/commands';
import type { ValidationResult } from '@cratis/arc/validation';
import type { OrganizationNameCommand } from './OrganizationNameCommand';
import type { OrganizationNameValidationState } from './OrganizationNameValidationState';

const nameError = (results: ValidationResult[]): string | undefined =>
    results.find(result => result.members.some(member => member.toLowerCase() === 'organizationname'))?.message;

/** Validate a copy: the wizard must never acquire temporary values for fields on later steps. */
export const validateOrganizationName = async <TCommand extends OrganizationNameCommand>(
    current: TCommand,
    createProbe: () => TCommand,
    organizationName: string
): Promise<ICommandResult<unknown>> => {
    const probe = createProbe();
    probe.organizationName = organizationName;
    // Arc validates required fields on the client before calling /validate. Temporary values for
    // later steps let the server's name rules run before those steps have been filled in.
    probe.firstName = current.firstName || 'Validation';
    probe.lastName = current.lastName || 'Only';
    probe.acceptedLegalTerms = current.acceptedLegalTerms ?? false;
    probe.acceptedLegalVersion = current.acceptedLegalVersion ?? '';
    return probe.validate();
};

/** Owns the name step's asynchronous pre-flight, including navigation gating and stale responses. */
export class OrganizationNameStepValidation<TCommand extends OrganizationNameCommand> {
    private readonly _listeners = new Set<() => void>();
    private _state: OrganizationNameValidationState = { isValidating: false };
    private _timer?: ReturnType<typeof setTimeout>;
    private _revision = 0;
    private _validatedName?: string;

    constructor(
        private readonly _createProbe: () => TCommand,
        private readonly _unavailableMessage: string,
        private readonly _onValidationFailure?: (results: ValidationResult[]) => void
    ) { }

    readonly subscribe = (listener: () => void): (() => void) => {
        this._listeners.add(listener);
        return () => { this._listeners.delete(listener); };
    };

    readonly getSnapshot = (): OrganizationNameValidationState => this._state;

    dispose(): void {
        this.cancel();
        this._listeners.clear();
    }

    readonly onFieldChange = (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown, validationInfo?: FieldValidationInfo): void => {
        if (fieldName !== 'organizationName') return;
        const organizationName = command.organizationName;
        if (oldValue === newValue) {
            if (validationInfo?.isValid && organizationName && this._validatedName !== organizationName) {
                this.schedule(command, organizationName, 0);
            }
            return;
        }

        this._validatedName = undefined;
        const clientError = nameError(command.validateClientSide().validationResults);
        if (clientError || !organizationName) {
            this.cancel();
            this.update({ isValidating: false, error: clientError });
            return;
        }
        this.schedule(command, organizationName, 300);
    };

    private schedule(command: TCommand, organizationName: string, delay: number): void {
        this.cancel();
        const revision = this._revision;
        this.update({ isValidating: true });
        this._timer = setTimeout(() => { void this.validate(command, organizationName, revision); }, delay);
    }

    private async validate(command: TCommand, organizationName: string, revision: number): Promise<void> {
        try {
            const result = await validateOrganizationName(command, this._createProbe, organizationName);
            if (revision !== this._revision) return;
            this._onValidationFailure?.(result.validationResults);
            if (result.hasExceptions || !result.isAuthorized) {
                this.update({ isValidating: false, error: this._unavailableMessage });
                return;
            }
            this._validatedName = organizationName;
            this.update({ isValidating: false, error: nameError(result.validationResults) });
        } catch {
            if (revision === this._revision) this.update({ isValidating: false, error: this._unavailableMessage });
        }
    }

    private cancel(): void {
        this._revision++;
        if (this._timer) clearTimeout(this._timer);
        this._timer = undefined;
    }

    private update(state: OrganizationNameValidationState): void {
        this._state = state;
        this._listeners.forEach(listener => listener());
    }
}
