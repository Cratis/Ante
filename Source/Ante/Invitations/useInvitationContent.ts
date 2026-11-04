// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { GetInvitationContent } from '../Configuration/Configuration';
import { useLocale } from '../Locale/LocaleContext';

/** Read only the current invitation journey's copy; a locale change never resets wizard values. */
export const useInvitationContent = (journey: 'userSetup' | 'organizationSetup') => {
    const locale = useLocale();
    const previousLocale = useRef(locale);
    const [result, refresh] = GetInvitationContent.use();
    useEffect(() => {
        if (previousLocale.current === locale) return;
        previousLocale.current = locale;
        void refresh();
    }, [locale, refresh]);
    return result.isSuccess ? result.data?.[journey] : undefined;
};
