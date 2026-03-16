# Phase 5: Process Management - Context

**Gathered:** 2026-02-21
**Status:** Ready for planning

## Phase Boundary

Sistema spawnea procesos, trackea PIDs, y limpia rutas automáticamente cuando terminan. Los procesos se ejecutan como background processes con PORT inyectada, el sistema monitorea su estado mediante polling, y realiza cleanup coordinado de rutas y procesos.

## Implementation Decisions

### Process execution mode
- Ejecución directa (exec) sin shell wrapper — más rápido y menos overhead
- Stdout/stderr heredados de la CLI (inherit) — output visible en tiempo real
- Stdout y stderr mergeados en un solo stream — simple y suficiente
- Working directory: current directory de donde se llama `portless run`

### Process tracking & lifecycle
- Polling cada 5 segundos para verificar si proceso sigue vivo
- Metadata guardada por proceso: PID + hostname + puerto + command + start time (básico)
- Reaping de procesos zombies — detection y cleanup proper

### Signal forwarding behavior
- Solo SIGTERM se forwarda al proceso — menos complejo, asume graceful shutdown
- 10 segundos de timeout después de SIGTERM antes de marcar como no-responsivo
- Ctrl+C en la CLI forwarda SIGTERM al app — shutdown coordinado
- Cuando proxy se detiene con procesos activos: prompt al usuario para decidir si terminarlos

### Cleanup & error handling
- Cleanup de rutas en el siguiente ciclo de polling (no inmediato) — menos llamadas al proxy
- Procesos que crash (exit != 0) se limpian igual que normal exit — no rutas zombie
- Proxy restart: re-attach todos los procesos existentes leyendo routes.json — recupera estado
- Procesos orphan (terminados pero sin PID en routes.json): grace period de 5 minutos antes de cleanup

### Claude's Discretion
- Implementación exacta del mecanismo de polling (timer, thread, background service)
- Manejo de race conditions entre cleanup y proxy restart
- Formato exacto de almacenamiento de metadata en routes.json
- Implementación de reaping de zombies cross-platform

## Specific Ideas

- Priorizar simplicidad y velocidad sobre features avanzadas de monitoring
- Comportamiento expected de CLI: output visible, working directory intuitivo
- Graceful shutdown es responsabilidad del proceso — Portless solo forwarda SIGTERM
- Prevención de data loss: grace periods y re-attach en restart

## Deferred Ideas

None — discussion stayed within phase scope.

---

*Phase: 05-process-management*
*Context gathered: 2026-02-21*
