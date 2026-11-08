// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.for_BackchannelCredentials.given;

namespace Ante.for_BackchannelCredentials.when_validating;

public class and_the_credential_is_not_visible_ascii : Specification
{
    Exception? _lineBreak;
    Exception? _space;
    Exception? _nonAscii;
    Exception? _control;

    void Because()
    {
        _lineBreak = Validate(Credentials.Valid + "\r\nX-Injected: 1");
        _space = Validate(Credentials.Valid + " tail");
        _nonAscii = Validate(Credentials.Valid + "\u00e6\u00f8\u00e5");
        _control = Validate(Credentials.Valid + "\u0007");
    }

    static Exception? Validate(string credential) => Cratis.Specifications.Catch.Exception(() => BackchannelCredentials.Validate(
        new AnteOptions { IdentityBackchannelUrl = "https://h.example.com", IdentityBackchannelCredential = credential }, false));

    [Fact] void should_reject_a_line_break() => Assert.IsType<BackchannelCredentialInvalid>(_lineBreak);
    [Fact] void should_reject_a_space() => Assert.IsType<BackchannelCredentialInvalid>(_space);
    [Fact] void should_reject_non_ascii() => Assert.IsType<BackchannelCredentialInvalid>(_nonAscii);
    [Fact] void should_reject_a_control_character() => Assert.IsType<BackchannelCredentialInvalid>(_control);
}
#endif
