# Preset name checks

Run from the repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File scripts/Test-PresetNames.ps1
```

This compiles the actual configuration and preset UI classes against explicit
Unity/BSML doubles and PowerShell's bundled Newtonsoft.Json. Test configuration
files are written to a unique system temporary directory; game UserData is never
used. No Beat Saber installation is required for these checks.

Coverage: legacy JSON, optional names, Unicode text elements and Paste, duplicate
names/index identity, blank fallback, control/newline sanitization, length limits,
Cancel, stale modal targets, save failure rollback, restart/delete, and comparison
of all non-name serialized preset values before/after Rename. BSML assertions keep
seven rows and literal, bounded name labels.

The checks do not render the actual BSML keyboard. In VR, review the Rename/Delete
button layout, long-name ellipsis/tooltips, PASTE/CANCEL keys, and Unicode glyphs.
For local builds, suppress the upstream project's deployment post-build event:
`/p:DeployToGame=false /p:PostBuildEvent=`.
