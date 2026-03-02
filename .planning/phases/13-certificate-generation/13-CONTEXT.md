# Phase 13: Certificate Generation - Context

**Gathered:** 2026-02-22
**Status:** Ready for planning

## Phase Boundary

Automatic generation of local Certificate Authority (CA) and wildcard TLS certificates for `.localhost` domains using .NET native APIs only. Certificates persist to disk with secure file permissions and metadata for lifecycle management. This phase delivers the foundational certificate infrastructure; trust installation and HTTPS proxy endpoints are separate phases.

## Implementation Decisions

### Estrategia de generación
- **Primera generación**: Preguntar al usuario la primera vez si desea generar certificados automáticamente o manualmente
  - **Phase 13 scope**: Logger notification message (user consent via CLI interaction deferred to Phase 14)
  - **Phase 14 scope**: Interactive CLI prompt for user consent before first-time generation
- **Certificados existentes**: Reutilizar siempre si existen, sin preguntar
- **Período de validez**: 5 años (1825 días) para tanto CA como certificado wildcard
- **Almacenamiento**: A criterio de Claude (tres archivos separados vs CA incrustado en JSON)

### Metadatos y validación
- **Metadatos en cert-info.json**: Mínimo esencial (fingerprint SHA-256, fechas creación/expiración, versión de Portless)
- **Validación de integridad**: A criterio de Claude (nivel apropiado para desarrollo local)
- **Formato de fechas/fingerprints**: A criterio de Claude (formato más estándar y portable)

### Manejo de errores
- **Sin permisos en ~/.portless**: Error claro + sugerencia (ejecutar como administrator o ajustar permisos)
- **Certificados corruptos**: Regenerar automáticamente con warning al usuario
- **Errores de API .NET**: A criterio de Claude (balance entre UX y debugging)

### Seguridad de claves
- **Protección PFX**: Sin contraseña, solo seguridad por permisos de archivos
- **Permisos Windows**: Full Control solo para el usuario actual (SYSTEM y Administrators también tienen acceso)
- **Permisos inseguros**: Advertir en startup si otros usuarios pueden leer, pero continuar normalmente

### Claude's Discretion
- **Estrategia de almacenamiento**: Tres archivos separados (ca.pfx, cert.pfx, cert-info.json) o CA incrustado en JSON según el diseño más limpio
- **Validación de integridad**: Cargar PFX y verificar clave privada; opcionalmente comparar fingerprint con cert-info.json
- **Formato de metadatos**: Fingerprint como hex, fechas en ISO 8601 o Unix timestamp según estándares más portables
- **Manejo de excepciones**: Capturar excepciones específicas vs genéricas según balance apropiado de UX y debugging

## Specific Ideas

No hay requisitos específicos - abierto a enfoques estándar de .NET para certificados de desarrollo.

## Deferred Ideas

None - discussion stayed within phase scope.

---

*Phase: 13-certificate-generation*
*Context gathered: 2026-02-22*
