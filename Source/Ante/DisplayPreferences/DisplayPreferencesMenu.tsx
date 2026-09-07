// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId, useState } from 'react';
import { Button, IconButton, Radio } from '@cratis/components/Common';
import { Dialog } from '@cratis/components/Dialogs';
import { DisplayContrast, DisplayControlSize, DisplayMotion, DisplaySpacing, DisplayTextSize } from './DisplayPreferences';
import { useDisplayPreferences } from './useDisplayPreferences';
import strings from 'Strings';
import './DisplayPreferencesMenu.css';

/**
 * The onboarding display-preferences trigger and panel described by `Cratis/Ante#20` - a small,
 * neutral settings menu for text size, contrast, spacing, control size and motion. It is not a
 * command: nothing here is submitted or validated, it only ever reads and writes
 * {@link useDisplayPreferences}, which applies every change immediately.
 *
 * Rendered once per onboarding frame (`OrganizationSetupFrame`/`UserSetupFrame`), which is shared by
 * all three onboarding journeys (organization setup, user setup/invitation acceptance, and
 * self-service registration).
 */
export const DisplayPreferencesMenu = () => {
    const [visible, setVisible] = useState(false);
    const { preferences, setTextSize, setContrast, setSpacing, setControlSize, setMotion, reset } = useDisplayPreferences();
    const s = strings.displayPreferences;
    const groupNamePrefix = useId();

    return (
        <>
            <IconButton
                icon={<span className='pi pi-cog' aria-hidden='true' />}
                aria-label={s.trigger}
                variant='ghost'
                className='display-preferences-menu__trigger'
                onClick={() => setVisible(true)}
            />
            {visible && (
                <Dialog title={s.title} width='28rem' buttons={null} onCancel={() => setVisible(false)}>
                    <div className='display-preferences-menu__content'>
                        <p className='display-preferences-menu__description'>{s.description}</p>
                        <OptionGroup<DisplayTextSize>
                            name={`${groupNamePrefix}-text-size`}
                            legend={s.textSize.label}
                            value={preferences.textSize}
                            onChange={setTextSize}
                            options={[
                                { value: 'default', label: s.textSize.default },
                                { value: 'large', label: s.textSize.large },
                                { value: 'largest', label: s.textSize.largest }
                            ]}
                        />
                        <OptionGroup<DisplayContrast>
                            name={`${groupNamePrefix}-contrast`}
                            legend={s.contrast.label}
                            value={preferences.contrast}
                            onChange={setContrast}
                            options={[
                                { value: 'default', label: s.contrast.default },
                                { value: 'high', label: s.contrast.high }
                            ]}
                        />
                        <OptionGroup<DisplaySpacing>
                            name={`${groupNamePrefix}-spacing`}
                            legend={s.spacing.label}
                            value={preferences.spacing}
                            onChange={setSpacing}
                            options={[
                                { value: 'default', label: s.spacing.default },
                                { value: 'relaxed', label: s.spacing.relaxed }
                            ]}
                        />
                        <OptionGroup<DisplayControlSize>
                            name={`${groupNamePrefix}-control-size`}
                            legend={s.controlSize.label}
                            value={preferences.controlSize}
                            onChange={setControlSize}
                            options={[
                                { value: 'default', label: s.controlSize.default },
                                { value: 'large', label: s.controlSize.large }
                            ]}
                        />
                        <OptionGroup<DisplayMotion>
                            name={`${groupNamePrefix}-motion`}
                            legend={s.motion.label}
                            value={preferences.motion}
                            onChange={setMotion}
                            options={[
                                { value: 'system', label: s.motion.system },
                                { value: 'reduced', label: s.motion.reduced }
                            ]}
                        />
                        <div className='display-preferences-menu__actions'>
                            <Button variant='ghost' label={s.reset} onClick={reset} />
                            <Button label={s.close} onClick={() => setVisible(false)} />
                        </div>
                    </div>
                </Dialog>
            )}
        </>
    );
};

interface OptionGroupProps<TValue extends string> {
    name: string;
    legend: string;
    value: TValue;
    onChange: (value: TValue) => void;
    options: { value: TValue; label: string }[];
}

/** One `<fieldset>`/`<legend>`-grouped set of mutually-exclusive radio options - a labeled, keyboard-navigable native radio group for a single preference. */
const OptionGroup = <TValue extends string>({ name, legend, value, onChange, options }: OptionGroupProps<TValue>) => (
    <fieldset className='display-preferences-menu__group'>
        <legend>{legend}</legend>
        {options.map(option => (
            <Radio
                key={option.value}
                name={name}
                value={option.value}
                label={option.label}
                checked={value === option.value}
                onChange={() => onChange(option.value)}
            />
        ))}
    </fieldset>
);
