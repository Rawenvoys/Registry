# Registry

Generyczna platforma do prowadzenia rejestrów. Użytkownik sam definiuje katalogi (np. wina, bydło, wydania muzyczne) i ich pola; aplikacja nie zna dziedziny.

## Struktura

- `src/Domain` - encje i reguły
- `src/Application` - przypadki użycia
- `src/Infrastructure` - EF Core, Identity, integracje
- `src/Contracts` - DTO współdzielone przez API i klientów
- `src/Client` - typowany klient API (Refit) używany przez UI w web i mobile
- `src/Api` - ASP.NET Core API
- `src/PresentationKit` - wspólne ekrany Blazor (Razor Class Library) dla web i mobile
- `src/WebApp` - Blazor WebAssembly, host dla `PresentationKit`
- `src/MobileApp` - .NET MAUI Blazor Hybrid, host dla `PresentationKit`
- `tests/UnitTests`, `tests/IntegrationTests`
- `docs/` - decyzje projektowe

Przestrzenie nazw i nazwy assembly dostają prefiks `Registry.` z `Directory.Build.props`.

## Uruchomienie

```
dotnet test tests/UnitTests
dotnet run --project src/Api
dotnet run --project src/WebApp
```

Wymaga .NET 10 SDK. Linki potwierdzające e-mail API na razie tylko loguje w konsoli.

Logowanie przez Google, Microsoft i Facebook w WebApp włącza się, podając identyfikatory aplikacji w `src/WebApp/wwwroot/appsettings.json` (`ExternalLogin:*:ClientId`) oraz te same identyfikatory w API (`Authentication:External`). U dostawcy trzeba dodać adres powrotu `https://localhost:7037/signin-callback.html` i zezwolić na tokeny ID (implicit flow). Bez identyfikatora przycisk danego dostawcy się nie pokazuje.

 MobileApp dodatkowo wymaga workloadu MAUI (`dotnet workload install maui-android`, na Windows/macOS także `maui`).
