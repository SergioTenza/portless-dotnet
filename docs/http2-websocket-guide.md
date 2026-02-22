# HTTP/2 and WebSocket Testing Guide

Complete guide for testing and verifying HTTP/2 and WebSocket functionality with Portless.NET.

## Table of Contents

1. [Quick Verification](#quick-verification)
2. [HTTP/2 Testing](#http2-testing)
3. [WebSocket Testing](#websocket-testing)
4. [SignalR Testing](#signalr-testing)
5. [Browser DevTools](#browser-devtools)
6. [Expected Outputs](#expected-outputs)

---

## Quick Verification

**Is HTTP/2 working?**

```bash
# Start proxy
portless proxy start

# Run any app (using WebApi as example)
portless webapi dotnet run --project Examples/WebApi

# Test HTTP/2
curl -I --http2 http://webapi.localhost:1355

# Expected: HTTP/2 200
```

**Is WebSocket working?**

```bash
# Start proxy
portless proxy start

# Run echo server
portless wsecho dotnet run --project Examples/WebSocketEchoServer

# Open browser
# http://wsecho.localhost:1355
# Send test message - should echo back
```

---

## HTTP/2 Testing

### Method 1: curl with HTTP/2 Prior Knowledge

**Best for:** Testing if proxy and backend both support HTTP/2

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run HTTP/2 test app (using WebApi as example)
portless webapi dotnet run --project Examples/WebApi

# Terminal 3: Test with curl
curl -I --http2-prior-knowledge http://webapi.localhost:1355
```

**Expected Output:**
```
HTTP/2 200
content-type: application/json
date: Mon, 22 Feb 2026 12:00:00 GMT
server: Kestrel
```

**What to look for:**
- `HTTP/2 200` - Confirming HTTP/2 protocol
- Not `HTTP/1.1 200` - That would indicate downgrade

---

### Method 2: curl with HTTP/2 Negotiation

**Best for:** Testing automatic HTTP/2 negotiation (real-world scenario)

```bash
curl -I --http2 http://webapi.localhost:1355
```

**Expected Output:**
```
HTTP/2 200
...
```

**Note:** If you see `HTTP/1.1 200`, this is the "silent downgrade" issue. Without HTTPS, HTTP/2 negotiation may fall back to HTTP/1.1. Use `--http2-prior-knowledge` for testing HTTP/2 over HTTP.

---

### Method 3: Verbose curl Output

**Best for:** Debugging protocol negotiation

```bash
curl -v --http2 http://webapi.localhost:1355
```

**Expected Output:**
```
* Trying 127.0.0.1:1355...
* Connected to webapi.localhost (127.0.0.1) port 1355
* using HTTP/2
* h2h3 [:method: HEAD]
* h2h3 [:path: /]
* h2h3 [:scheme: http]
* h2h3 [:authority: webapi.localhost:1355]
...
> HEAD / HTTP/2
> Host: webapi.localhost:1355
> User-Agent: curl/8.0.0
> Accept: */*
...
< HTTP/2 200
```

**What to look for:**
- `* using HTTP/2` - Confirms HTTP/2 is being used
- `> HEAD / HTTP/2` - Request line shows HTTP/2
- `< HTTP/2 200` - Response line shows HTTP/2

---

### HTTP/2 Performance Test

**Test multiplexing (multiple concurrent requests):**

```bash
# Send 10 concurrent requests
for i in {1..10}; do
  curl -I --http2 http://webapi.localhost:1355 &
done
wait
```

**Expected Behavior:**
- All requests complete quickly
- No connection errors
- HTTP/2 multiplexing handles concurrent requests efficiently

---

## WebSocket Testing

### Method 1: Browser-Based Testing (Echo Server)

**Best for:** Quick visual verification

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run echo server
portless wsecho dotnet run --project Examples/WebSocketEchoServer
```

**In Browser:**
1. Open `http://wsecho.localhost:1355`
2. Open browser DevTools Console (F12)
3. Run the following JavaScript:
   ```javascript
   const ws = new WebSocket('ws://wsecho.localhost:1355/ws');
   ws.onopen = () => {
       console.log('WebSocket connected');
       ws.send('Hello WebSocket!');
   };
   ws.onmessage = (event) => {
       console.log('Received:', event.data);
   };
   ```

**Expected:**
- Console shows "WebSocket connected"
- Console shows "Received: Hello WebSocket!"
- No connection errors

**What to look for:**
- WebSocket connection establishes (status indicator)
- Messages are echoed back immediately
- No connection errors or drops

---

### Method 2: websocat Command-Line Tool

**Best for:** Automated testing

**Install websocat:**
```bash
# macOS
brew install websocat

# Linux
cargo install websocat

# Windows (using Scoop)
scoop install websocat
```

**Test:**
```bash
# Terminal 1: Start proxy and echo server
portless proxy start
portless wsecho dotnet run --project Examples/WebSocketEchoServer

# Terminal 2: Connect with websocat
echo "Hello WebSocket!" | websocat ws://wsecho.localhost:1355/ws
```

**Expected Output:**
```
Hello WebSocket!
```

---

### Method 3: Python websockets

**Best for:** Python developers

```bash
# Terminal 1: Start proxy and echo server
portless proxy start
portless wsecho dotnet run --project Examples/WebSocketEchoServer

# Terminal 2: Python test (requires websockets package)
python3 -c "
import asyncio
import websockets

async def test():
    async with websockets.connect('ws://wsecho.localhost:1355/ws') as ws:
        await ws.send('Hello from Python!')
        response = await ws.recv()
        print(f'Received: {response}')

asyncio.run(test())
"
```

**Expected Output:**
```
Received: Hello from Python!
```

---

### Method 4: wscat (Node.js)

**Best for:** Node.js developers

**Install wscat:**
```bash
npm install -g wscat
```

**Test:**
```bash
wscat -c ws://wsecho.localhost:1355/ws
```

**Expected Output:**
```
Connected (press CTRL+C to quit)
> Hello
< Hello
```

---

### Long-Lived Connection Test

**Test WebSocket stability over extended period:**

```javascript
// Browser Console
const ws = new WebSocket('ws://wsecho.localhost:1355/ws');
let messageCount = 0;

const interval = setInterval(() => {
    if (ws.readyState === WebSocket.OPEN) {
        ws.send(`Ping ${++messageCount}`);
        console.log(`Sent: Ping ${messageCount}`);
    }
}, 10000); // Send message every 10 seconds

ws.onmessage = (event) => {
    console.log('Received:', event.data);
};

// Stop after 2 minutes
setTimeout(() => {
    clearInterval(interval);
    ws.close();
    console.log('Test complete - connection stable for 2 minutes');
}, 120000);
```

**Expected Behavior:**
- Connection remains stable beyond 60 seconds
- No timeout or disconnect
- Messages still echo after extended idle time

---

## SignalR Testing

### SignalR Chat Example

**Best for:** Testing real-world SignalR usage

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run SignalR chat server
portless chatsignalr dotnet run --project Examples/SignalRChat
```

**In Browser:**
1. Open `http://chatsignalr.localhost:1355` in Tab A
2. Open `http://chatsignalr.localhost:1355` in Tab B
3. Enter username in both tabs
4. Send message from Tab A
5. **Expected:** Message appears in both tabs

**What to look for:**
- WebSocket connection established (check DevTools)
- Real-time messages appear in all connected clients
- No connection drops or errors

---

### SignalR Console Client Test

**Best for:** Testing .NET SignalR client**

```bash
# Terminal 1: Start proxy and chat server
portless proxy start
portless chatsignalr dotnet run --project Examples/SignalRChat

# Terminal 2: Run console client
dotnet run --project Examples/SignalRChat.Client -- http://chatsignalr.localhost:1355/chathub
```

**Expected Output:**
```
Connecting to http://chatsignalr.localhost:1355/chathub...
Connected!
Enter your name: TestUser
> Hello from console
Broadcast: TestUser: Hello from console
```

---

### SignalR Transport Testing

**Test different SignalR transports:**

Open browser DevTools Console and run:

```javascript
// Test WebSocket transport
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://chatsignalr.localhost:1355/chathub", {
        transport: signalR.HttpTransportType.WebSockets
    })
    .build();

connection.start().then(() => {
    console.log("WebSocket transport: Connected");
    connection.stop();
}).catch(err => {
    console.error("Connection failed:", err);
});
```

**Expected Output:**
```
WebSocket transport: Connected
```

---

## Browser DevTools

### Open DevTools

- **Windows/Linux:** `F12` or `Ctrl+Shift+I`
- **macOS:** `Cmd+Option+I`

### Check HTTP/2 Protocol

1. Open **Network** tab
2. Make a request to `http://webapi.localhost:1355`
3. Click on the request
4. Look for **Protocol** column in headers

**Expected:**
- **Protocol:** `h2` (HTTP/2)
- If `http/1.1`, HTTP/2 negotiation failed (try `--http2-prior-knowledge`)

---

### Check WebSocket Connection

1. Open **Network** tab
2. Filter by **WS** (WebSockets)
3. Look for WebSocket connection
4. Click to inspect

**Expected:**
- **Status:** `101 Switching Protocols`
- **Type:** `websocket`
- **Protocol:** `h2` (HTTP/2) or `http/1.1`

**WebSocket Frames:**
- Click **Messages** tab
- See sent/received messages in real-time

---

### Verify SignalR Connection

1. Open **Console** tab
2. Look for SignalR connection messages

**Expected:**
```
SignalR: Connected.
SignalR: WebSocket connected.
```

**If connection fails:**
```
SignalR: Connection disconnected.
Error: WebSocket connection failed.
```

---

## Expected Outputs

### HTTP/2 Success

```
HTTP/2 200
content-type: application/json
date: Mon, 22 Feb 2026 12:00:00 GMT
server: Kestrel
```

### HTTP/2 Downgrade (Issue)

```
HTTP/1.1 200
content-type: application/json
...
```

**Solution:** Use `curl --http2-prior-knowledge` for HTTP/2 over HTTP (without HTTPS)

---

### WebSocket Success

**Browser Console:**
```
WebSocket connected to ws://wsecho.localhost:1355/ws
Message sent: Hello
Message received: Hello
```

**Network Tab:**
```
Status: 101 Switching Protocols
Type: websocket
Protocol: h2
```

---

### WebSocket Failure

**Browser Console:**
```
WebSocket connection to 'ws://wsecho.localhost:1355/ws' failed
```

**Possible causes:**
- Proxy not running
- Echo server not running
- Wrong port
- Firewall blocking connection

---

### SignalR Success

**Browser Console:**
```
SignalR: Negotiating HTTP/2 connection...
SignalR: WebSocket connected to http://chatsignalr.localhost:1355/chathub
SignalR: Connected.
```

**Chat Messages:**
- Messages appear in real-time
- All connected clients receive messages
- No delays or missed messages

---

### SignalR Failure

**Browser Console:**
```
SignalR: Connection disconnected.
Error: Connection started with WebSocket but failed to keep alive.
```

**Solution:** Check WebSocket timeout configuration and proxy settings

---

## Testing Checklist

Use this checklist to verify full protocol support:

- [ ] HTTP/2 works with `curl --http2-prior-knowledge`
- [ ] HTTP/2 works with `curl --http2` (negotiation)
- [ ] Browser DevTools show `h2` protocol
- [ ] WebSocket echo server works in browser
- [ ] WebSocket works with websocat/wscat
- [ ] WebSocket connection stable > 60 seconds
- [ ] SignalR chat connects and sends messages
- [ ] SignalR messages appear in all clients
- [ ] No errors in browser console
- [ ] No errors in proxy logs

---

## Automated Testing Script

**Save as `test-protocols.sh`:**

```bash
#!/bin/bash

echo "Portless.NET Protocol Testing Script"
echo "===================================="

# Check proxy is running
if ! portless proxy status &> /dev/null; then
    echo "Starting proxy..."
    portless proxy start
    sleep 2
fi

# Test HTTP/2
echo ""
echo "Testing HTTP/2..."
curl -I --http2-prior-knowledge http://webapi.localhost:1355 2>&1 | head -1

# Test WebSocket (requires websocat)
if command -v websocat &> /dev/null; then
    echo ""
    echo "Testing WebSocket..."
    echo "test" | websocat --one-message ws://wsecho.localhost:1355/ws
else
    echo ""
    echo "websocat not found - skipping WebSocket test"
    echo "Install with: brew install websocat (macOS) or scoop install websocat (Windows)"
fi

echo ""
echo "Testing complete!"
```

**Run:**
```bash
chmod +x test-protocols.sh
./test-protocols.sh
```

**Windows PowerShell version:**

```powershell
# Save as test-protocols.ps1

Write-Host "Portless.NET Protocol Testing Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check proxy is running
$proxyStatus = portless proxy status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Starting proxy..." -ForegroundColor Yellow
    portless proxy start
    Start-Sleep -Seconds 2
}

# Test HTTP/2
Write-Host ""
Write-Host "Testing HTTP/2..." -ForegroundColor Cyan
curl -I --http2-prior-knowledge http://webapi.localhost:1355 2>&1 | Select-Object -First 1

# Test WebSocket (requires websocat)
$websocat = Get-Command websocat -ErrorAction SilentlyContinue
if ($websocat) {
    Write-Host ""
    Write-Host "Testing WebSocket..." -ForegroundColor Cyan
    echo "test" | websocat --one-message ws://wsecho.localhost:1355/ws
} else {
    Write-Host ""
    Write-Host "websocat not found - skipping WebSocket test" -ForegroundColor Yellow
    Write-Host "Install with: scoop install websocat" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Testing complete!" -ForegroundColor Green
```

---

## Need More Help?

- [Examples README](../Examples/README.md) - Full list of examples and usage
- [SignalR Troubleshooting](signalr-troubleshooting.md) - SignalR-specific issues
- [WebSocket Echo Server Example](../Examples/WebSocketEchoServer/README.md) - WebSocket details
- [SignalR Chat Example](../Examples/SignalRChat/README.md) - SignalR details

---

*Guide: HTTP/2 and WebSocket Testing*
*Version: 1.1*
*Updated: 2026-02-22*
