# Phase 8: Integration Tests Automation for .NET Tool - Context

**Gathered:** 2026-02-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Sistema de pruebas de integración automatizadas para validar que Portless.NET funcione correctamente como dotnet tool instalado globalmente, incluyendo validación de comandos CLI y comportamiento del proxy YARP.

</domain>

<decisions>
## Implementation Decisions

### Alcance de Tests
- Cobertura completa: happy path + edge cases + failure scenarios
- Componentes a probar: CLI commands + Proxy YARP
- Modos de ejecución: Instalación global + ejecución local (dotnet run) + paquete NuGet
- Tipo de tests: Unit tests + E2E tests para máxima cobertura

### Ejecución de Tests
- Ejecución con tool real instalado (no simulaciones/mocks)
- Frecuencia: Manual on-demand (antes de cada release)
- Organización de tests: Proyectos separados por tipo (UnitTests, IntegrationTests, E2ETests)
- Paralelización: A criterio de Claude según el escenario

### Aislamiento y Cleanup
- Cleanup en cada test (tearDown individual)
- Si el cleanup falla: Loggear warnings pero continuar
- Directorio temporal único por test (GUID/timestamp)
- Puertos: Usar rango real (4000-4999) con detección dinámica de puertos libres

### Validación Cross-Platform
- Plataformas: Windows + Linux (Ubuntu/Debian)
- Método de validación: Manual (ejecutar tests en cada plataforma)
- Criterio de éxito: Tests pasando es suficiente
- Diferencias de plataforma: Mismos tests en todas las plataformas (abstraer diferencias)

### Claude's Discretion
- Paralelización de tests según el escenario (secuenciales vs paralelos)
- Implementación exacta de mejecanismos de cleanup
- Estrategia para manejar race conditions en asignación de puertos

</decisions>

<specifics>
## Specific Ideas

- Tests deben ser realistas con tool real instalado, no mocks
- Cada test es independiente con su propio directorio temporal
- Validación manual en Windows y Linux antes de releases
- Estructura por tipo de test (Unit/Integration/E2E) para claridad

</specifics>

<deferred>
## Deferred Ideas

- macOS en la validación cross-platform (futuro si hay demanda)
- CI/CD automatizado (GitHub Actions) - puede ser fase futura
- Tests de performance/load testing - fuera de alcance de esta fase

</deferred>

---

*Phase: 08-integration-tests-automation-for-dotnet-tool*
*Context gathered: 2026-02-21*
