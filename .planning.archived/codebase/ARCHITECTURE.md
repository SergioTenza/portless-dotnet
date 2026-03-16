# Architecture

**Analysis Date:** 2026-02-19

## Pattern Overview

**Overall:** Clean Architecture with loose coupling

**Key Characteristics:**
- Separation of concerns into distinct projects
- CLI application for user interaction
- Core library for shared functionality
- Proxy component for reverse proxy capabilities
- Testing project for validation

## Layers

**CLI Layer:**
- Purpose: Provides command-line interface and user interaction
- Location: `Portless.Cli/`
- Contains: Console application entry point
- Depends on: Core library (not yet implemented)
- Used by: End users

**Core Layer:**
- Purpose: Contains shared business logic and utilities
- Location: `Portless.Core/`
- Contains: Currently empty placeholder for shared functionality
- Depends on: No dependencies
- Used by: CLI and Proxy layers

**Proxy Layer:**
- Purpose: Implements reverse proxy functionality
- Location: `Portless.Proxy/`
- Contains: Web application with YARP reverse proxy
- Depends on: .NET Web SDK, YARP package
- Used by: HTTP clients through routing configuration

**Test Layer:**
- Purpose: Unit and integration testing
- Location: `Portless.Tests/`
- Contains: Test infrastructure (currently empty)
- Depends on: xunit testing framework
- Used by: Development process

## Data Flow

**Configuration Update Flow:**

1. HTTP POST to `/api/v1/add-host` endpoint
2. Request contains RouteConfig and ClusterConfig
3. YARP's InMemoryConfigProvider updates routing
4. Proxy applies new configuration immediately

**Request Processing Flow:**

1. Incoming HTTP request reaches reverse proxy
2. YARP routes based on current configuration
3. Request forwarded to appropriate backend
4. Response returned to client

**State Management:**
- Configuration stored in memory via InMemoryConfigProvider
- Runtime updates through HTTP API
- No persistent storage currently implemented

## Key Abstractions

**Reverse Proxy Abstraction:**
- Purpose: HTTP routing and load balancing
- Implementation: YARP (Yet Another Reverse Proxy)
- Pattern: Reverse proxy with dynamic configuration

**Configuration Management:**
- Purpose: Dynamic routing configuration
- Implementation: HTTP API for updates
- Pattern: Real-time configuration without restart

**CLI Abstraction:**
- Purpose: Command-line interface
- Implementation: Spectre.Console.Cli framework
- Pattern: Console application with rich output

## Entry Points

**CLI Entry Point:**
- Location: `Portless.Cli/Program.cs`
- Triggers: Command execution
- Responsibilities: Display welcome banner, coordinate with core components

**Proxy Entry Point:**
- Location: `Portless.Proxy/Program.cs`
- Triggers: HTTP server startup
- Responsibilities: Initialize reverse proxy, expose configuration API

## Error Handling

**Strategy:** Basic error handling with minimal validation

**Patterns:**
- Direct exception handling in proxy endpoint
- No structured error responses currently
- Minimal logging implemented

## Cross-Cutting Concerns

**Logging:** Not implemented yet
**Validation:** Minimal validation on configuration updates
**Authentication:** Not implemented - all endpoints currently public

---

*Architecture analysis: 2026-02-19*
