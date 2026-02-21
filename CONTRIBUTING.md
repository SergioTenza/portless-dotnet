# Contribuyendo a Portless.NET

¡Gracias por tu interés en contribuir a Portless.NET! 🎉

Este documento proporciona directrices y líneas maestras para contribuir al proyecto.

## 📋 Tabla de Contenidos

- [Código de Conducta](#código-de-conducta)
- [¿Cómo Contribuir?](#cómo-contribuir)
- [Proceso de Desarrollo](#proceso-de-desarrollo)
- [Pull Requests](#pull-requests)
- [Estándares de Código](#estándares-de-código)
- [Convenciones de Commits](#convenciones-de-commits)
- [Reportando Issues](#reportando-issues)

## 🤝 Código de Conducta

Al participar en este proyecto, te comprometes a mantener un ambiente inclusivo y respetuoso. Por favor:

- Ser respetuoso con otros contribuidores
- Ser constructivo en tus comentarios
- Aceptar crítica constructiva con gracia
- Ser empático con otros desarrolladores

## 🚀 Cómo Contribuir?

### Reporting Bugs

Antes de reportar un bug, por favor:

1. Busca [issues existentes](https://github.com/SergioTenza/portless-dotnet/issues) para evitar duplicados
2. Verifica que el bug aún existe en la última versión
3. Recopila información importante:
   - Sistema operativo y versión
   - Versión de .NET (`dotnet --version`)
   - Versión de Portless.NET
   - Pasos para reproducir el bug
   - Comportamiento esperado vs comportamiento real
   - Logs o mensajes de error relevantes

### Sugerendo Nuevas Características

Las sugerencias de características son bienvenidas. Por favor incluye:

- Una descripción clara de la característica propuesta
- Casos de uso y ejemplos de cómo sería utilizada
- Posibles alternativas o enfoques
- Si estás dispuesto a implementarla tú mismo

### Contribuyendo Código

1. **Fork el repositorio** y crea tu rama desde `main`
2. **Haz tus cambios** siguiendo los [estándares de código](#estándares-de-código)
3. **Escribe tests** si aplicable (usamos xUnit)
4. **Asegúrate que todos los tests pasen**: `dotnet test`
5. **Formatea tu código**: `dotnet format`
6. **Commitea tus cambios** usando las [convenciones de commits](#convenciones-de-commits)
7. **Push a tu fork** y abre un Pull Request

## 🔄 Proceso de Desarrollo

### Requisitos Previos

- **.NET 10 SDK** instalado
- **Git** configurado
- **Editor de código**: Visual Studio, Visual Studio Code, o JetBrains Rider

### Configuración del Entorno

```bash
# Clona tu fork
git clone https://github.com/tu-usuario/portless-dotnet.git
cd portless-dotnet

# Agrega el upstream
git remote add upstream https://github.com/SergioTenza/portless-dotnet.git

# Restaura herramientas
dotnet tool restore

# Build del proyecto
dotnet build

# Ejecuta tests
dotnet test
```

### Creando una Rama

```bash
# Actualiza tu rama main
git checkout main
git pull upstream main

# Crea una rama para tu feature/fix
git checkout -b feature/nombre-de-tu-feature
# o
git checkout -b fix/descripcion-del-bug
```

## 📨 Pull Requests

### Antes de Abrir un PR

- [ ] Tu código sigue los [estándares de código](#estándares-de-código)
- [ ] Todos los tests pasan: `dotnet test`
- [ ] El código está formateado: `dotnet format`
- [ ] Has agregado tests para nuevas funcionalidades
- [ ] Has actualizado la documentación si es necesario
- [ ] Tus commits siguen las [convenciones](#convenciones-de-commits)

### Titulo del PR

Usa un título claro y descriptivo:

- ✅ `feat: add support for custom proxy port`
- ✅ `fix: resolve race condition in port allocation`
- ✅ `docs: update README with new command syntax`

### Descripción del PR

Incluye en la descripción:

- **Motivación**: Por qué este cambio es necesario
- **Cambios**: Lista de modificaciones realizadas
- **Tests**: Cómo probaste los cambios
- **Screenshots**: Si aplicable (para cambios de UI/CLI)
- **Breaking changes**: Si este cambio introduce cambios que rompen compatibilidad

## 📐 Estándares de Código

### C# Style Guidelines

Seguimos las convenciones estándar de C# y .NET:

- **PascalCase** para clases, métodos, propiedades públicas
- **camelCase** para parámetros, variables locales
- **_camelCase** para campos privados
- **PascalCase** para constantes

```csharp
public class ProxyService
{
    private readonly ILogger<ProxyService> _logger;
    private const int DefaultPort = 1355;

    public ProxyService(ILogger<ProxyService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(int port)
    {
        // Implementación
    }
}
```

### Organización de Archivos

- Un archivo por clase (generalmente)
- Nombre del archivo = nombre de la clase
- Usar `partial class` solo cuando es generado o es estrictamente necesario

### Comentarios

- **XML Documentation** para APIs públicas
- Comentarios inline solo para lógica compleja o no obvia

```csharp
/// <summary>
/// Starts the proxy server on the specified port.
/// </summary>
/// <param name="port">The port to listen on.</param>
/// <returns>A task representing the async operation.</returns>
public async Task StartAsync(int port)
{
    // ...
}
```

### Imports

- Usar `using` statements en el top del archivo
- Usar `global usings` en `Usings.cs` cuando sea apropiado
- Ordenar alfabéticamente
- Remover `using` no utilizados

## 📝 Convenciones de Commits

Usamos el formato [Conventional Commits](https://www.conventionalcommits.org/):

```
<tipo>(<alcance>): <descripción>

[opcional: cuerpo]

[opcional: footer]
```

### Tipos

- `feat`: Nueva funcionalidad
- `fix`: Corrección de bug
- `docs`: Cambios en documentación
- `style`: Cambios de formato (código, no lógica)
- `refactor`: Refactorización de código
- `test`: Agregar o actualizar tests
- `chore`: Cambios en proceso de build, herramientas, etc.
- `perf`: Mejoras de rendimiento

### Ejemplos

```bash
feat(cli): add interactive mode for command selection
fix(proxy): resolve race condition in port allocation
docs(readme): update installation instructions
refactor(core): extract port allocation to separate service
test(proxy): add integration tests for proxy lifecycle
```

### Formato de Commit Message

```bash
# Bueno
feat(cli): add --port flag to proxy start command

# Permite specifying custom port instead of default 1355

# Closes #123

# Malo
add port flag
fix stuff
update docs
```

## 🐛 Reportando Issues

### Plantilla de Bug Report

```markdown
**Describe el bug**
Una descripción clara del problema.

**Pasos para reproducir**
1. Ejecuta '...'
2. Presiona '....'
3. Scroll down to '....'
4. Ve el error

**Comportamiento esperado**
Una descripción de lo que deberías ver.

**Screenshots**
Si es aplicable, agrega screenshots.

**Entorno**
- OS: [ej. Windows 11, macOS 14, Ubuntu 22.04]
- .NET Version: [ej. 10.0.0]
- Portless.NET Version: [ej. 0.1.0]

**Logs**
Adjunta logs relevantes si los hay.

**Contexto adicional**
Información adicional que ayude a entender el problema.
```

### Plantilla de Feature Request

```markdown
**¿Tu feature request está relacionado con un problema?**
Una descripción clara del problema. Ej: Siempre me frustra cuando...

**Describe la solución que te gustaría**
Una descripción clara de lo que quieres que pase.

**Describe alternativas que has considerado**
Una descripción de soluciones alternativas que has considerado.

**Contexto adicional**
Cualquier otro contexto o screenshots sobre el feature request.
```

## 🧪 Testing

Ejecutamos tests usando xUnit:

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con coverage
dotnet test /p:CollectCoverage=true

# Ejecutar tests específicos
dotnet test --filter "FullyQualifiedName~ProxyService"
```

## 📚 Recursos Adicionales

- [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [xUnit Documentation](https://xunit.net/docs/getting-started)

## 💬 Comunicación

- **Issues**: Para bugs y feature requests
- **Discussions**: Para preguntas y debates generales
- **Pull Requests**: Para contribuciones de código

---

De nuevo, ¡gracias por contribuir a Portless.NET! 🙏
