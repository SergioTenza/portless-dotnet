# Phase 4: Port Management - Context

**Gathered:** 2026-02-20
**Status:** Ready for planning

## Phase Boundary

Sistema detecta automáticamente puertos libres en el rango 4000-4999, los asigna a procesos sin conflictos, inyecta la variable de entorno PORT en el comando ejecutado, y libera los puertos cuando los procesos terminan para permitir su reutilización.

## Implementation Decisions

### PORT Variable Injection
- Solo inyectar variable `PORT` (no `PORTLESS_HOST` ni otras adicionales)
- El comando es responsable de leer y usar la variable PORT
- Portless solo inyecta, no valida si el comando la usa

### Claude's Discretion
- **Cross-platform injection mechanism**: Elegir entre pre-exec injection vs OS-specific wrappers según lo más simple de implementar en .NET 10
- **Validation de uso de PORT**: Warning opcional si el comando no parece usar ${PORT} o %PORT%, basarse en el balance entre helpfulness y complejidad
- **Fallback on injection failure**: Ejecutar sin PORT o fallar con error, según el UX que tenga más sentido

## Specific Ideas

- Keep it simple — Portless injects PORT, the app uses it. If the app doesn't, that's the app's concern.
- No agregar PORTLESS_HOST para mantener bajo acoplamiento

## Deferred Ideas

None — discussion stayed within phase scope.

---

*Phase: 04-port-management*
*Context gathered: 2026-02-20*
