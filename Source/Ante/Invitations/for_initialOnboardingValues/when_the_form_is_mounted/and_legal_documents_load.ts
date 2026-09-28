// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import React, { act } from 'react';
import { createRoot, Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, it } from 'vitest';
import { CommandForm, useCommandFormContext } from '@cratis/arc.react/commands';
import { CommandResult } from '@cratis/arc/commands';
import { Constructor, Guid } from '@cratis/fundamentals';
import { RegisterOrganization } from '../../../Organization/Registration/Registration';
import { SetupOrganization } from '../../OrganizationSetup/OrganizationSetup';
import { AcceptInvitation } from '../../UserSetup/UserSetup';
import { OrganizationNameStepValidation } from '../../../Organization/OrganizationNameStepValidation';
import type { OrganizationNameCommand } from '../../../Organization/OrganizationNameCommand';
import { LegalVersionValues } from '../../../Legal/LegalVersionValues';
import { LocaleProvider } from '../../../Locale/LocaleContext';
import { initialRegistrationValues, initialOrganizationSetupValues, initialUserSetupValues } from '../../initialOnboardingValues';

type OnboardingCommand = RegisterOrganization | SetupOrganization | AcceptInvitation;
const id = Guid.parse('ff6c55e9-f696-4f9a-a17b-03115977b70a');
Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });

function specifyMountedForm<T extends OnboardingCommand>(flow: string, commandType: Constructor<T>, initialValues: Partial<T>, hasOrganizationName: boolean) {
    describe(`when the ${flow} form loads legal documents`, () => {
        let root: Root;
        let container: HTMLDivElement;
        let command: T;
        let update: (values: T) => void;
        let executeForm: () => Promise<unknown>;
        let version: string | undefined;
        let isChecking: boolean;
        let locale: 'en' | 'nb-NO';
        let submittedVersion: string | undefined;

        const CaptureCommand = () => {
            const context = useCommandFormContext<T>();
            command = context.commandInstance;
            update = context.setCommandValues;
            executeForm = context.onExecute!;
            return null;
        };

        const renderForm = async () => {
            const Form = CommandForm<T>;
            await act(async () => {
                root.render(React.createElement(LocaleProvider, {
                    locale, settings: { defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] }, onChange: () => { }
                }, React.createElement('div', { hidden: isChecking, inert: isChecking },
                    React.createElement(Form, { command: commandType, initialValues },
                        React.createElement(CaptureCommand),
                        version && React.createElement(LegalVersionValues, { version })))));
            });
        };

        beforeEach(async () => {
            container = document.createElement('div');
            document.body.append(container);
            root = createRoot(container);
            version = undefined;
            isChecking = false;
            locale = 'en';
            submittedVersion = undefined;
            await renderForm();
            act(() => {
                command.firstName = 'Ada';
                command.middleName = 'Marie';
                command.lastName = 'Lovelace';
                if (hasOrganizationName) (command as RegisterOrganization).organizationName = 'Acme Labs';
                update(command);
            });
            version = 'legal-v4';
            await renderForm();
        });

        afterEach(async () => {
            await act(async () => root.unmount());
            container.remove();
        });

        it('should send the configured legal version with the submitted command', async () => {
            command.execute = (async () => {
                submittedVersion = command.acceptedLegalVersion;
                return CommandResult.empty;
            }) as typeof command.execute;
            await act(async () => {
                command.acceptedLegalTerms = true;
                update(command);
                await executeForm();
            });
            submittedVersion!.should.equal('legal-v4');
        });

        it('should keep entered names and acceptance when checking the same legal version again', async () => {
            act(() => {
                command.acceptedLegalTerms = true;
                update(command);
            });
            isChecking = true;
            await renderForm();
            command.firstName.should.equal('Ada');
            command.acceptedLegalTerms.should.be.true;
            isChecking = false;
            await renderForm();
            command.firstName.should.equal('Ada');
            command.middleName!.should.equal('Marie');
            command.lastName.should.equal('Lovelace');
            command.acceptedLegalTerms.should.be.true;
        });

        it('should preserve entered names through pre-flight and a language switch', async () => {
            // Type again after the legal document is configured, not only while it is loading.
            act(() => {
                command.firstName = 'Grace';
                command.middleName = 'Murray';
                command.lastName = 'Hopper';
                if (hasOrganizationName) (command as RegisterOrganization).organizationName = 'Acme Labs';
                update(command);
            });
            if (hasOrganizationName) {
                const probe: OrganizationNameCommand = {
                    organizationName: 'Acme Labs', firstName: '', lastName: '', acceptedLegalTerms: false,
                    acceptedLegalVersion: '', validateClientSide: () => CommandResult.empty,
                    validate: async () => CommandResult.empty
                };
                const validation = new OrganizationNameStepValidation(() => ({ ...probe }), () => 'Unavailable');
                validation.onFieldChange(probe, 'organizationName', '', 'Acme Labs');
                await new Promise(resolve => setTimeout(resolve, 350));
                validation.dispose();
            }
            locale = 'nb-NO';
            await renderForm();
            command.firstName.should.equal('Grace');
            command.middleName!.should.equal('Murray');
            command.lastName.should.equal('Hopper');
            if (hasOrganizationName) (command as RegisterOrganization).organizationName.should.equal('Acme Labs');
            act(() => { command.acceptedLegalTerms = true; update(command); });
            await renderForm();
            command.acceptedLegalTerms.should.be.true;
            version = 'legal-v5';
            await renderForm();
            command.acceptedLegalTerms.should.be.false;
            command.firstName.should.equal('Grace');
        });
    });
}

specifyMountedForm('registration', RegisterOrganization, initialRegistrationValues(id), true);
specifyMountedForm('invited organization setup', SetupOrganization, initialOrganizationSetupValues(id), true);
specifyMountedForm('join', AcceptInvitation, initialUserSetupValues(id), false);
