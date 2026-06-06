# RS232ools

A Windows serial terminal for sending and receiving data over RS232 COM ports,
with a built-in message simulator for generating and parsing structured serial
strings, and a rule-based device responder for simulating command/response
devices.

## What it does

RS232ools works with **multiple serial sessions at once**. Each session lives in
its own tab, owns its own COM port and serial settings, and contains a
**Terminal**, a **Simulator**, and an **Advanced** responder that all share that
session's connection. So you can drive several ports side by side in one window —
for example, a terminal on one device and a rule-based responder feeding another.

### Session tabs

Each tab across the top of the window is an independent serial **session**.
Inside a session you set the COM port and framing, connect, and use its Terminal,
Simulator, and Advanced sub-tabs — exactly as described below — against that
session's own port.

- **New tab** (in the title bar) opens another session, so you can connect to a
  different COM port without closing the first. Each session connects,
  disconnects, sends, receives, and logs entirely independently of the others.
- **Rename** a tab by double-clicking its label; press Enter to confirm or Esc to
  cancel. Until you rename it, a tab is named automatically after its selected
  port (e.g. `COM3`).
- **Close** a tab with the **×** on its label. Closing a session disconnects its
  port and stops any streaming or logging it was doing. The window always keeps at
  least one session open.

### Terminal

The Terminal tab is a full-duplex RS232 serial terminal. You connect to any COM
port on your machine, type or paste text into the send box and transmit it, and
watch incoming data appear live in the receive pane — all at the same time.

**Sending** works two ways: type directly into the send box and press Enter or
click Send, or use the file picker to select any file and stream its raw bytes
straight to the port.

A **Text / Hex** mode selector sits next to the send box (alongside the
line-ending selector and Send button) and controls how the typed input is
interpreted:

- **Text** (default) — the typed text is sent as-is, with the selected
  line ending (None, CR, LF, or CR+LF, defaulting to CR+LF) appended.
- **Hex** — the input is parsed as hexadecimal byte pairs and transmitted as
  raw binary bytes. Spaces are optional: `1A 2B FF` and `1A2BFF` are both
  accepted. This lets you send arbitrary binary from the keyboard without
  needing a file — for example, to send a CRLF terminator you would type
  `0D 0A`. The line-ending selector is disabled in Hex mode; include any
  terminator as bytes. Invalid hex is rejected with a warning. In Monitor
  display mode, the sent bytes are echoed in hex form.

The "Send file" option is the complementary route for larger binary payloads:
it streams any file (including `.bin`) as raw bytes directly to the port.

**Display mode.** A "Display" selector at the top of the Terminal tab controls
what appears in the pane:

- **Received only** (default) — the original behaviour: only incoming data is
  shown.
- **Monitor (sent + received)** — echoes everything written to the port (typed
  text and streamed file content) interleaved with everything read from it, live.
  Each line is tagged and coloured by direction: TX lines are prefixed `TX > `
  and shown in the brand accent colour (orange); RX lines are prefixed `RX < `
  and shown in white. This lets you watch the full conversation — or a file
  transfer — as it happens. When a sent file is echoed, its name and byte count
  are shown first; display is capped at 64 KB for large files.

**Receiving** shows all incoming data in a scrolling pane. Auto-scroll keeps the
latest data in view; a Clear button wipes the pane. Tick "Log to file" at any
point to write everything received from that moment onwards to a `.log` or `.txt`
file of your choosing, in real time.

Port settings are fully configurable before connecting: COM port (auto-detected
and refreshed when the drop-down opens), baud rate (1200–115200), data bits
(5–8), parity (None/Odd/Even/Mark/Space), stop bits (1/1.5/2), and handshake
(None/XOnXOff/RequestToSend/RequestToSendXOnXOff). All controls lock while a
connection is open. A coloured status indicator shows whether a port is connected
or disconnected, and any unexpected loss of connection (such as a USB-serial
adapter being unplugged) is detected and reported automatically.

### Simulator

The Simulator tab generates structured serial strings and can parse incoming
ones back into a table — useful for testing receivers, logging systems, or any
device that speaks a regular line-based protocol.

**Format.** Messages can be produced in four formats:

- **CSV** — delimiter-separated values (comma by default). The delimiter is
  editable; an empty delimiter falls back to a comma.
- **NMEA 0183** — sentences framed as `$<payload>*CS`, where `CS` is the
  standard XOR checksum expressed as two uppercase hex digits. The checksum can
  be toggled on or off. The field delimiter is editable.
- **Plain** — field values concatenated with no separator at all. The delimiter
  box is disabled for this format.
- **Hex** — the payload's bytes written as space-separated uppercase hex pairs
  (e.g. `48 65 6C`). The field values are joined using the configured delimiter
  before encoding, so the fields round-trip on parse when a delimiter is set;
  with no delimiter the decoded text comes back as a single value.

**Fields.** You define an ordered list of fields, each with a name and one of
six types:

- **Fixed text** — a constant string (e.g. a sentence identifier such as `GPGGA`).
- **Random integer** — a whole number drawn uniformly from a configurable
  `[Min, Max]` range.
- **Random decimal** — a real number from `[Min, Max]`, rounded to a configurable
  number of decimal places.
- **Incrementing counter** — starts at `Min` and advances by one with each
  message sent.
- **Timestamp** — the current time formatted with a configurable .NET format
  string (default `HHmmss.ff`).
- **Sine wave** — a value that oscillates between `Min` (trough) and `Max`
  (peak), advancing one sample per message. The wave starts at the midpoint.
  **Period** sets the number of messages per full cycle. **Precision** controls
  the number of decimal places, the same as for Random decimal.

The same field list drives both generation and parsing, so a format round-trips
without any separate configuration.

**Sending.** Click **Preview** to see what the next generated string will look
like (without advancing any counters). Click **Generate & send** to generate a
fresh string and transmit it immediately over the open port. Tick **Stream
every** and enter an interval in milliseconds (minimum 50 ms, default 1000 ms)
to send a newly generated string automatically at that rate. Streaming stops
automatically on disconnect.

**Receiving.** Tick **Parse incoming into table** to have every incoming
line split into a live table whose columns match the defined field names. For
NMEA, a dedicated checksum column shows whether each sentence's checksum was
valid, absent, or invalid. The table is bounded to the last 1000 rows. Click
**Clear table** to reset it.

**Save / load config.** Click **Save config…** to write the current simulator
setup — format kind, delimiter, NMEA checksum flag, stream interval, and the
full ordered field list — to a human-readable JSON file. Click **Load config…**
to restore a previously saved setup from file. Loading stops any active stream
before applying the new configuration.

### Advanced

The Advanced tab is a rule-based device responder. Where the Simulator generates
fixed-structure messages on a timer, the Advanced tab listens and replies: each
incoming command is matched against a list of rules, and the matching rule's reply
is sent back automatically. This makes it possible to simulate a device that
responds differently depending on what it receives.

**Rules.** You define an ordered list of rules. Each rule has:

- **Enabled** — a toggle to activate or deactivate the rule without deleting it.
- **Name** — a label used in the activity log.
- **Match (RX)** — the pattern to match against incoming lines. By default this is
  a `{placeholder}` template: literal text must match literally, and each
  `{name}` captures a token from the corresponding position (e.g. `READ {raw}`
  matches the line `READ 100` and captures `raw = 100`). The whole line must
  match. Tick a rule's **Regex** toggle to treat the pattern as a raw regular
  expression with named capture groups instead.
- **Reply (TX)** — the template sent back when the rule matches. Any `{name}`
  placeholder in the template is replaced with the captured or derived value of
  that name (e.g. `VAL={scaled}` becomes `VAL=10` if `scaled` has been computed
  as 10).

The **first enabled rule whose pattern matches** wins; later rules are not tried.

**Variables.** Each rule has an optional list of derived variables. Each variable
has a name and an expression evaluated against the captured values (and any
earlier derived variables in the same list). Expressions support:

- Arithmetic: `+` `-` `*` `/` `%`
- Comparison: `==` `!=` `<` `<=` `>` `>=`
- Boolean logic: `&&` `||` `!`
- Ternary: `cond ? a : b`
- Parentheses, numeric literals, and `true` / `false` (booleans are 1 / 0)

For example, given a captured variable `raw`, you might define `scaled = raw *
0.1` to divide it by ten, or `state = level > 50 ? 1 : 0` to produce a flag.
The computed value is then available as `{scaled}` or `{state}` in the Reply
template.

**Auto-respond.** The **Auto-respond** master toggle is enabled only while the
port is open. When turned on, every complete incoming line is run through the
rules: if a rule matches, its reply (plus the configured **Reply ending** —
None, CR, LF, or CR+LF, defaulting to CR+LF) is sent immediately. Lines that
match no rule are silently ignored. Any rule or expression error is reported in
the log without stopping the responder.

**Activity log.** Each match produces a timestamped log entry showing the
received line, the matched rule name, and the transmitted reply — for example,
`RX READ 100  ->  [scale]  TX VAL=10`. Errors (bad pattern, bad expression,
send failure) are also logged. A Clear button wipes the log.

**Starter rules.** The tab opens with two example rules pre-loaded: a simple
`PING` → `PONG` echo, and a `READ {raw}` → `VAL={scaled}` rule that captures a
number, computes `scaled = raw * 0.1`, and replies with the scaled value. These
demonstrate capture and arithmetic in a small, working example.

## Installation

**Prerequisites**

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

**Build from source**

```
git clone https://github.com/bdh-utils/RS232ools.git
cd RS232ools
dotnet build RS232ools.sln
```

## Usage

Run the app directly from the project directory:

```
dotnet run --project RS232ools
```

Or run the compiled executable produced in `RS232ools\bin\`:

```
RS232ools\bin\Debug\net8.0-windows\RS232ools.exe
```

**Typical Terminal workflow**

1. Select a COM port, set baud rate and framing (the default 9600-8-N-1 suits
   most devices).
2. Click **Connect**. The status bar turns green and shows the port name.
3. Type a command in the send box and press Enter (or click **Send**) to
   transmit. Incoming data appears immediately in the receive pane.
4. To watch both sides of the conversation, select **Monitor (sent + received)**
   from the Display selector. TX lines appear in orange; RX lines in white.
5. To send raw binary bytes from the keyboard, select **Hex** from the send mode
   selector and type the bytes as hex pairs (e.g. `0D 0A` for CRLF). The
   line-ending selector disables automatically; include any terminator as bytes.
6. To send a file, click **Choose file...**, select the file, then click
   **Send file**. In Monitor mode, the file's content is echoed into the pane
   alongside any responses.
7. To capture incoming data, tick **Log to file** and choose a destination path.
8. Click **Disconnect** when done.
9. To work with another port at the same time, click **New tab** in the title bar
   and repeat — each tab is an independent session. Double-click a tab to rename
   it, or click its **×** to close it.

**Typical Simulator workflow**

1. Connect to a port as above (the Simulator shares its session's connection).
2. Switch to the **Simulator** sub-tab.
3. Choose a format (CSV, NMEA, Plain, or Hex) and configure the field list.
4. Click **Preview** to check the output, then **Generate & send** to transmit
   one message, or tick **Stream every** and set an interval to transmit
   continuously.
5. To inspect incoming messages, tick **Parse incoming into table**; each line
   will appear as a row with a column per field.
6. To save the current setup for reuse, click **Save config…** and choose a
   location. To restore it later, click **Load config…**.

**Typical Advanced workflow**

1. Connect to a port as above (the Advanced tab shares its session's connection).
2. Switch to the **Advanced** sub-tab.
3. Review the two starter rules or click **Add rule** to create your own. For
   each rule, set the Name, Match pattern, and Reply template. Use
   `{placeholder}` syntax in the Match pattern to capture tokens by name.
4. To derive new values from the captures, select a rule and click **Add
   variable** in the Variables panel. Give the variable a name and an expression
   (e.g. `raw * 0.1`). The variable's name can then be used as a `{placeholder}`
   in the rule's Reply template.
5. Set the **Reply ending** to match what the receiving device expects (usually
   CR+LF).
6. Tick **Auto-respond**. The responder is now active: each incoming line that
   matches a rule will receive its reply immediately. Watch the activity log to
   confirm matches and replies.
7. Untick **Auto-respond** or click **Disconnect** to stop.

## About bdh-utils

This app is part of **bdh-utils** — a collection of small, free, no-nonsense
utility apps. Every bdh-utils app is **fully AI-developed**: designed, written,
and documented with AI. The aim is simple, dependable tools that each do one job
well — no ads, no tracking, no upsells, no accounts, no clutter.

## License

RS232ools is free and open-source software, released under the
[Apache License 2.0](LICENSE). See the [bdh-utils GitHub
organisation](https://github.com/bdh-utils) for further details.
