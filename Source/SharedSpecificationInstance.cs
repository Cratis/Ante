// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Opt every spec assembly into the shared-instance specification runner: the spec class is
// constructed once per class, Establish/Because run once, every [Fact] asserts against that one
// execution, and Destroy runs after the last fact. This is the BDD semantics the corpus already
// mandates (facts are assertion-only) and it removes the per-fact rebuild of scenario pipelines.
// Injected into every spec project by Source/Directory.Build.props (Debug only).
[assembly: Xunit.TestFramework("Cratis.Specifications.SpecificationTestFramework", "Cratis.Specifications.XUnit")]
