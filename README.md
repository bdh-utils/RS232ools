# RS232ools

A Windows serial terminal for sending and receiving data over RS232 COM ports.

## What it does

RS232ools is a full-duplex RS232 serial terminal. You connect to any COM port on
your machine, type or paste text into the send box and transmit it, and watch
incoming data appear live in the receive pane — all at the same time.

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

**Typical workflow**

1. Select a COM port, set baud rate and framing (the default 9600-8-N-1 suits
   most devices).
2. Click **Connect**. The status bar turns green and shows the port name.
3. Type a command in the send box and press Enter (or click **Send**) to
   transmit. Incoming data appears immediately in the receive pane.
4. To send a file, click **Choose file...**, select the file, then click
   **Send file**.
5. To capture incoming data, tick **Log to file** and choose a destination path.
6. Click **Disconnect** when done.

## About bdh-utils

This app is part of **bdh-utils** — a collection of small, free, no-nonsense
utility apps. Every bdh-utils app is **fully AI-developed**: designed, written,
and documented with AI. The aim is simple, dependable tools that each do one job
well — no ads, no tracking, no upsells, no accounts, no clutter.

## License

RS232ools is free and open-source software, released under the
[Apache License 2.0](LICENSE). See the [bdh-utils GitHub
organisation](https://github.com/bdh-utils) for further details.
