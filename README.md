# Registry

Platforma do ewidencji zbiorów i zapasów: konta, grupy (dom, firma z lokalizacjami) i moduły dziedzinowe.
Pierwszy moduł: zapasy wina.

## Struktura

- `src/Registry.Domain` - encje i reguły dziedzinowe
- `src/Registry.Application` - przypadki użycia
- `src/Registry.Infrastructure` - EF Core, Identity, integracje
- `src/Registry.Api` - ASP.NET Core API
- `tests/Registry.UnitTests`
- `docs/` - decyzje projektowe

## Uruchomienie

```
dotnet build
dotnet test
dotnet run --project src/Registry.Api
```

Wymaga .NET 10 SDK.
