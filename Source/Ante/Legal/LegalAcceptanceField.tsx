// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId } from 'react';
import { Checkbox } from '@cratis/components/Common';
import { asCommandFormField, WrappedFieldProps } from '@cratis/arc.react/commands';
import { LegalDocumentKind } from './LegalDocumentKind';
import { buildLegalAcceptanceLabel } from './legalAcceptanceLabel';
import strings from 'Strings';
import './LegalAcceptanceField.css';

interface LegalAcceptanceFieldComponentProps extends WrappedFieldProps<boolean> {

    /** Called when one of the document links in the label is activated. */
    onShowDocument: (kind: LegalDocumentKind) => void;
}

/**
 * The acceptance checkbox for the legal document set, with the terms and the privacy policy as links inside its
 * own label.
 *
 * Built with `asCommandFormField` rather than the plain `CheckboxField` because the label has to carry links,
 * and `CheckboxField` only accepts a string. Going through `asCommandFormField` keeps it a real CommandForm
 * field, so validation still re-runs and the submit button is not permanently disabled.
 *
 * It deliberately renders no message of its own for `errors`: the CommandForm field wrapper it is composed
 * into already renders them below the field, so doing it here too showed every rejection twice.
 */
export const LegalAcceptanceField = asCommandFormField(
    ({ value, onChange, onBlur, invalid, required, onShowDocument }: LegalAcceptanceFieldComponentProps) => {
        const inputId = useId();
        const legalStrings = strings.legal;
        const segments = buildLegalAcceptanceLabel(legalStrings.acceptance, legalStrings.documents);

        return (
            <div className='legal-acceptance'>
                <div className='legal-acceptance__row'>
                    <Checkbox
                        id={inputId}
                        checked={value}
                        onChange={onChange}
                        onBlur={onBlur}
                        invalid={invalid}
                        required={required} />
                    <label className='legal-acceptance__label' htmlFor={inputId}>
                        {segments.map(({ text, kind }, index) => kind === undefined
                            ? <span key={index}>{text}</span>
                            : (
                                <button
                                    key={index}
                                    type='button'
                                    className='legal-acceptance__link'
                                    // Without preventDefault the click would bubble to the surrounding label and
                                    // toggle acceptance as a side effect of opening the document.
                                    onClick={event => { event.preventDefault(); onShowDocument(kind); }}
                                >
                                    {text}
                                </button>
                            ))}
                    </label>
                </div>
            </div>
        );
    },
    {
        defaultValue: false,
        extractValue: checked => checked as boolean
    });
