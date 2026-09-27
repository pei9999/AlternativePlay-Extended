$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$files = @(
 'AlternativePlay/Models/Configuration.cs',
 'AlternativePlay/Models/ConfigurationData.cs',
 'AlternativePlay/Models/ConfigurationData075.cs',
 'AlternativePlay/Models/ConfigurationIconSummary.cs',
 'AlternativePlay/Models/IconNames.cs',
 'AlternativePlay/UI/PlayModeSelectOption.cs',
 'AlternativePlay/UI/AlternativePlayView.cs',
 'AlternativePlay/UI/PlayModeSelectTab.cs',
 'tests/PresetName.Checks.cs'
) | ForEach-Object { Join-Path $root $_ }
$references = @(Get-ChildItem (Join-Path $PSHOME 'ref/*.dll') | Select-Object -ExpandProperty FullName) + [Newtonsoft.Json.JsonConvert].Assembly.Location
# PowerShell's bundled Newtonsoft targets an older System.Runtime but runs in this host.
Add-Type -Path $files -ReferencedAssemblies ($references | Select-Object -Unique) -CompilerOptions '/nowarn:1701'
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('AlternativePlay-PresetNames-' + [Guid]::NewGuid().ToString('N'))
[PresetNameChecks]::Run($scratch)
foreach ($file in @('AlternativePlayView', 'PlayModeSelectTab')) {
 [xml]$bsml = Get-Content (Join-Path $root "AlternativePlay/UI/$file.bsml") -Raw
 $name = $bsml.SelectSingleNode('//text[@text="~DisplayName"]')
 if (!$name -or $name.GetAttribute('word-wrapping') -ne 'false' -or $name.GetAttribute('rich-text') -ne 'false' -or $name.GetAttribute('overflow-mode') -ne 'Ellipsis') { throw 'Name label can overflow or interpret markup.' }
 if ($bsml.SelectSingleNode('//custom-list').GetAttribute('visible-cells') -ne '7') { throw 'Preset list row count changed.' }
}
[xml]$main = Get-Content (Join-Path $root 'AlternativePlay/UI/AlternativePlayView.bsml') -Raw
if ($main.SelectNodes('//modal-keyboard[@on-enter="OnRenameConfirmed"]|//button[@on-click="OnRenameClicked"]').Count -ne 2) { throw 'Missing Rename modal/binding.' }
'Preset BSML keeps seven rows, bounds literal name labels, and binds the installed modal-keyboard API.'
