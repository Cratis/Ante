// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from '@cratis/components/Common';
import strings from 'Strings';

export const RegistrationStartFailed = ({ onRetry, onStartNew }: { onRetry: () => void; onStartNew: () => void }) => (
    <div role='alert'>
        <p>{strings.registration.startUnavailable}</p>
        <Button label={strings.onboarding.checkAgain} onClick={onRetry} />
        <Button label={strings.registration.startNewRegistration} variant='ghost' onClick={onStartNew} />
    </div>
);
