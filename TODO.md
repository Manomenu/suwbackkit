# TODO

Decided, not built yet; each arrives as its own change when it is needed.

- Server options: a shared refusal format (code/fix), OpenTelemetry, .NET 10 AddValidation.
- `Suwbackkit.Cli`: the CLI and SDK skeleton that suwagcms and suwageval each carry a twin of today.
- `Suwbackkit.Ai`: the LLM client wrappers from suwageval (about 100 lines), when a second app needs them.
- Procedures: the AI procedure engine lives in suwchatbot (repos/suwchatbot); alfusion has its own, deterministic procedure engine. Not worth merging into suwbackkit now. Revisit when a second app needs procedures, e.g. suwstockdrill, whose plan builds AI procedures as nodes like suwchatbot: then compare suwchatbot's and alfusion's engines and decide whether a shared `Suwbackkit.Procedures` pays off.
- Package validation against the last published version (`EnablePackageValidation` with a baseline), once `Suwbackkit.Server` 0.1.0 is on nuget.org and its public API is worth keeping.
