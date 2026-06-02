# RS232ools

A Windows serial terminal for sending and receiving data over RS232 COM ports,
with a built-in message simulator for generating and parsing structured serial
strings.

## What it does

RS232ools is organised into two tabs that share a single serial connection:
**Terminal** and **Simulator**. Connect once and use either tab — or both at
the same time.

### Terminal

The Terminal tab is a full-duplex RS232 serial terminal. You connect to any COM
port on your machine, type or paste text into the send box and transmit it, and
watch incoming data appear live in the receive pane — all at the same time.

**Sending** works two ways: type directly into the send box and press Enter or
click Send, or use the file picker to select any file and stream its raw bytes
straight to the port. A selectable line-ending option (None, CR, LF, or CR+LF,
defaulting to CR+LF) is appended to each typed transmission.

**Receiving** shows all incoming data in a scrolling monospace pane. Auto-scroll
keeps the latest data in view; a Clear button wipes the pane. Tick "Log to file"
at any point to write everything received from that moment onwards to a `.log` or
`.txt` file of your choosing, in real time.

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

**Format.** Messages can be produced as plain **CSV** (delimiter-separated
values, comma by default) or as **NMEA 0183** sentences. NMEA output is framed
as `$<payload>*CS`, where `CS` is the standard XOR checksum expressed as two
uppercase hex digits; the checksum can be toggled on or off. The field delimiter
is editable for both formats.

**Fields.** You define an ordered list of fields, each with a name and one of
five types:

- **Fixed text** — a constant string (e.g. a sentence identifier such as `GPGGA`).
- **Random integer** — a whole number drawn uniformly from a configurable
  `[Min, Max]` range.
- **Random decimal** — a real number from `[Min, Max]`, rounded to a configurable
  number of decimal places.
- **Incrementing counter** — starts at `Min` and advances by one with each
  message sent.
- **Timestamp** — the current time formatted with a configurable .NET format
  string (default `HHmmss.ff`).

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
4. To send a file, click **Choose file...**, select the file, then click
   **Send file**.
5. To capture incoming data, tick **Log to file** and choose a destination path.
6. Click **Disconnect** when done.

**Typical Simulator workflow**

1. Connect to a port as above (the Simulator tab shares the same connection).
2. Switch to the **Simulator** tab.
3. Choose a format (CSV or NMEA) and configure the field list.
4. Click **Preview** to check the output, then **Generate & send** to transmit
   one message, or tick **Stream every** and set an interval to transmit
   continuously.
5. To inspect incoming messages, tick **Parse incoming into table**; each line
   will appear as a row with a column per field.

## About bdh-utils

This app is part of **bdh-utils** — a collection of small, free, no-nonsense
utility apps. Every bdh-utils app is **fully AI-developed**: designed, written,
and documented with AI. The aim is simple, dependable tools that each do one job
well — no ads, no tracking, no upsells, no accounts, no clutter.

## License

RS232ools is free and open-source software, released under the
[Apache License 2.0](LICENSE). See the [bdh-utils GitHub
organisation](https://github.com/bdh-utils) for further details.
