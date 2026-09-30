// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import './FieldError.css';

interface FieldErrorProps {

    /** The validation messages for the field. */
    errors: string[];

    /** The property the messages belong to. */
    fieldName?: string;
}

/**
 * How a command form shows a field's validation message on screen - handed to the form as its
 * `errorDisplayComponent`.
 *
 * Cratis Components already gives every field a visually hidden error element that the control
 * points at with `aria-describedby`, so assistive technology reads the message from there. The
 * default on-screen message is one more copy in the accessibility tree, which a screen reader reads
 * again in browse mode (`Cratis/Ante#151`). This one is for sighted people only and is hidden from
 * assistive technology.
 */
export const FieldError = ({ errors }: FieldErrorProps) => (
    <small className='ante-field-error' aria-hidden='true'>
        {errors.join(' ')}
    </small>
);
