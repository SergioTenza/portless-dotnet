# Phase 04-01: Port Pooling and Core Allocator - Summary

**Executed:** 2026-02-20
**Status:** ✅ Completed
**Wave:** 1

## Objective
Implement port pooling and lifecycle management to prevent port conflicts and enable port reuse when processes terminate.

## Implementation Summary

### 1. IPortPool Interface and PortPool Implementation ✅
**Created Files:**
- `Portless.Core/Services/IPortPool.cs` - Interface defining port allocation tracking API
- `Portless.Core/Services/PortPool.cs` - Thread-safe port pool implementation

**Key Features:**
- `Dictionary<int, int>` tracking port → PID mappings
- Thread-safe operations with `lock(_lock)` for all public methods
- Methods: `Allocate(port, pid)`, `ReleaseByPid(pid)`, `ReleaseByPort(port)`, `IsPortAllocated(port)`
- Integrated logging for allocation/release events
- Supports multi-port scenarios per PID (ReleaseByPid removes all mappings)

**Verification:** Compiled successfully with no warnings.

### 2. PortAllocator Moved to Portless.Core ✅
**Modified Files:**
- `Portless.Core/Services/PortAllocator.cs` - NEW: Enhanced allocator with pooling
- `Portless.Core/Services/IPortAllocator.cs` - NEW: Updated interface with PID parameter
- `Portless.Core/Extensions/ServiceCollectionExtensions.cs` - Updated registration
- `Portless.Cli/Services/PortAllocator.cs` - Changed to wrapper delegating to Core
- `Portless.Cli/Services/IPortAllocator.cs` - DELETED: Resolved ambiguity
- `Portless.Cli/Program.cs` - Removed duplicate registration

**Key Changes:**
- `AssignFreePortAsync(int pid)` now accepts PID parameter for tracking
- Checks `_portPool.IsPortAllocated(port)` before TCP binding
- Calls `_portPool.Allocate(port, pid)` after successful port detection
- CLI version delegates to Core.PortAllocator (no code duplication)
- ServiceCollectionExtensions registers IPortPool and IPortAllocator as singletons

**Architecture Pattern:**
```
Portless.Core (Source of Truth)
├── IPortAllocator (interface)
├── PortAllocator (implementation + IPortPool injection)
└── IPortPool (interface)
    └── PortPool (implementation with PID tracking)

Portless.Cli (Thin Wrapper)
└── PortAllocator (delegates to Core.PortAllocator)
```

**Verification:** Solution builds without errors, all references resolve to Core version.

## Success Criteria Met
✅ PortPool service created with PID tracking and thread-safety
✅ PortAllocator moved to Portless.Core and enhanced with pooling
✅ PortAllocator.AssignFreePortAsync(int pid) accepts PID parameter
✅ PortAllocator integrates with PortPool for allocation tracking
✅ CLI PortAllocator delegates to Core PortAllocator (no duplication)
✅ Solution builds without compilation errors

## Technical Decisions

### Thread-Safety Approach
**Decision:** Use `lock` statements on private `_lock` object for all PortPool operations.
**Rationale:** RouteCleanupService runs on background thread; concurrent access requires synchronization.

### Dependency Injection Pattern
**Decision:** Register both IPortPool and IPortAllocator as singletons in AddPortlessPersistence().
**Rationale:** PortPool maintains in-memory state; single instance ensures consistent tracking across application.

### Interface Naming
**Decision:** Deleted Portless.Cli.Services.IPortAllocator, use only Portless.Core.Services.IPortAllocator.
**Rationale:** Resolved CS0104 ambiguous reference error; CLI wrapper uses fully qualified name when needed.

## Integration with RouteCleanupService
The next phase (04-02) will:
1. Inject IPortAllocator into RouteCleanupService constructor
2. Call ReleasePortAsync for dead processes in ExecuteAsync
3. Complete the port lifecycle: allocate → track → release → reuse

## Files Modified
- `Portless.Core/Services/IPortPool.cs` (NEW)
- `Portless.Core/Services/PortPool.cs` (NEW)
- `Portless.Core/Services/IPortAllocator.cs` (NEW)
- `Portless.Core/Services/PortAllocator.cs` (NEW)
- `Portless.Core/Extensions/ServiceCollectionExtensions.cs` (MODIFIED)
- `Portless.Cli/Services/PortAllocator.cs` (MODIFIED)
- `Portless.Cli/Services/IPortAllocator.cs` (DELETED)
- `Portless.Cli/Program.cs` (MODIFIED)

## Next Steps
Proceed to 04-02-PLAN.md for cross-platform PORT injection and RouteCleanupService integration.
