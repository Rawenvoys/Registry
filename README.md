# Registry

Generyczna platforma do prowadzenia rejestrów. Użytkownik sam definiuje katalogi (np. wina, bydło, wydania muzyczne) i ich pola; aplikacja nie zna dziedziny.

## Struktura

- `src/Domain` - encje i reguły
- `src/Application` - przypadki użycia
- `src/Infrastructure` - EF Core, Identity, integracje
- `src/Contracts` - DTO współdzielone przez API i klientów
- `src/Api` - ASP.NET Core API
- `src/UI` - wspólne komponenty Blazor (Razor Class Library) dla web i mobile
- `src/WebApp` - Blazor WebAssembly, host dla `UI`
- `src/MobileApp` - .NET MAUI Blazor Hybrid, host dla `UI`
- `tests/UnitTests`
- `docs/` - decyzje projektowe

Przestrzenie nazw i nazwy assembly dostają prefiks `Registry.` z `Directory.Build.props`.

## Uruchomienie

```
dotnet test tests/UnitTests
dotnet run --project src/Api
dotnet run --project src/WebApp
```

Wymaga .NET 10 SDK. MobileApp dodatkowo wymaga workloadu MAUI (`dotnet workload install maui-android`, na Windows/macOS także `maui`).
