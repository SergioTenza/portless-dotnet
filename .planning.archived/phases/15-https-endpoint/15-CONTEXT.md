# Phase 15: HTTPS Endpoint - Context

**Gathered:** 2026-02-23
**Status:** Ready for planning

## Phase Boundary

Implementar dual HTTP/HTTPS endpoints en el proxy Portless.NET con certificado TLS automático. El proxy escuchará en puertos fijos 1355 (HTTP) y 1356 (HTTPS) con redirect HTTP→HTTPS cuando --https esté habilitado.

## Implementation Decisions

### Activación de HTTPS
- **Opt-in con flag --https**: `portless proxy start --https` habilita HTTPS
- **Sin flag**: Solo HTTP (puerto 1355), backward compatible con v1.1
- **Con flag**: HTTP + HTTPS simultáneos (1355 + 1356)
- **Auto-generación con --https**: Si certificado no existe, generar automáticamente (como Phase 13)
- **Logging minimalista**: "HTTPS certificate ready" sin detalles prolijos durante startup

### Gestión de certificados
- **Pre-startup validation**: Validar que cert.pfx existe y es válido antes de iniciar Kestrel
- **Sin --https**: No validar certificado (HTTP-only mode)
- **Con --https**: Error si certificado inválido: "Certificate not found. Run: portless cert install" (exit code 1)
- **Claude's discretion**: Nivel de validación (existencia vs expiración) según coherencia con ICertificateManager de Phase 13

### Configuración de puertos
- **Breaking change**: Puertos fijos HTTP=1355, HTTPS=1356 (no configurables)
- **PORTLESS_PORT deprecated**: Warning si está seteado: "PORTLESS_PORT deprecated. Fixed ports: HTTP=1355, HTTPS=1356"
- **Comunicación**: Documentar breaking change en CHANGELOG.md y migration guide v1.1→v1.2
- **Rationale**: Simplifica configuración, elimina complejidad de ports derivados

### Comportamiento dual HTTP/HTTPS
- **Con --https**: HTTP (1355) redirect 301→HTTPS (1356) para todas las requests
- **Sin --https**: Solo HTTP funciona normalmente
- **Redirect 301 permanente**: HTTP returns 301 Permanent Redirect a https://same-hostname:1356/path
- **Claude's discretion**:
  - Si /api/v1/* endpoints también redirect o se excluyen (según necesidades de management API)
  - Comportamiento HTTP sin --https flag (siempre activo o respetar flag)

## Specific Ideas

- "Quiero compatibilidad con v1.1 — HTTP-only debería seguir funcionando sin cambios"
- "El redirect 301 indica que HTTPS es permanente — browsers cachean el redirect"
- "Auto-generación de certificado durante startup simplifica el onboarding"

## Deferred Ideas

- Certificate background monitoring (deferred to Phase 17: Certificate Lifecycle)
- Integration tests for HTTPS (deferred to Phase 18: Integration Tests)
- User documentation for HTTPS (deferred to Phase 19: Documentation)

---

*Phase: 15-https-endpoint*
*Context gathered: 2026-02-23*
