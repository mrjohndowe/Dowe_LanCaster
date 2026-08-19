# Remote, Voice Control, and Themes

## Remote redesign

The Remote tab now uses a physical-remote-style layout:

- rounded dark remote body
- dedicated Back / Home / Replay controls
- circular directional pad and OK button
- playback controls
- volume controls
- larger keyboard-text field
- placeholder text: `Type or dictate text to your Roku...`

## Voice control

The Remote tab includes a `Start Voice Control` button.

Voice recognition uses the Windows speech recognition APIs through
`System.Speech`.

Supported commands include:

- Home
- Back
- Up
- Down
- Left
- Right
- OK / Okay / Select
- Play
- Pause
- Play Pause
- Rewind
- Fast Forward
- Forward
- Replay
- Volume Up
- Volume Down
- Mute
- Power

The feature uses the default Windows microphone and requires an installed
Windows speech recognizer.

## Dark mode

A Dark Mode checkbox is available in the main Roku toolbar.

The selected theme is saved to the normal Dowe LanCaster settings file and
restored on the next launch.

The theme applies to the complete Windows UI, including:

- application background
- panels
- controls
- text
- tabs
- text boxes
- combo boxes
- lists
- status bar

The physical Roku remote keeps its dark hardware appearance in either theme.
