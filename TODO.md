# TODO

Decided, not built yet; each arrives as its own change when it is needed.

- Move to `suwgate check` and `suwgate.yml` when `@maniumek/suwgate` 0.1.0 is published: it replaces the check script, the repository guards in `tests/Suwbackkit.Gate` and the hooks' scripts, and CI calls suwgate's reusable workflows. The acceptance-criteria check of the specs comes with it.
- Server 0.2 options: a shared refusal format (code/fix), alfusion's "non-null field is required" schema transformer, OpenTelemetry, .NET 10 AddValidation.
- `Suwbackkit.Cli`: the CLI and SDK skeleton that suwagcms and suwageval each carry a twin of today.
- `Suwbackkit.Ai`: the LLM client wrappers from suwageval (about 100 lines), when a second app needs them.
- Procedures: the AI procedure engine lives in suwchatbot (repos/suwchatbot); alfusion has its own, deterministic procedure engine. Not worth merging into suwbackkit now. Revisit when a second app needs procedures, e.g. suwstockdrill, whose plan builds AI procedures as nodes like suwchatbot: then compare suwchatbot's and alfusion's engines and decide whether a shared `Suwbackkit.Procedures` pays off.
- Package validation against the last published version (`EnablePackageValidation` with a baseline), once `Suwbackkit.Server` 0.1.0 is on nuget.org and its public API is worth keeping.
