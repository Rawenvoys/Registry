# Katalogi definiowane przez użytkownika

Stan: propozycja, 2026-10-02.

Aplikacja nie wie, czy ktoś prowadzi rejestr win, bydła czy utworów muzycznych. Użytkownik sam tworzy **katalogi**, nadaje im nazwę i definiuje pola. Kod zna tylko pojęcia: katalog, pole, wpis, stan.

## Przykłady

| Grupa | Katalog | Pola |
|---|---|---|
| Dom | Wina | Szczep (wybór), Rocznik (liczba), Kraj (wybór), Cena (kwota) |
| Gospodarstwo | Bydło | Nr kolczyka (tekst), Rasa (wybór), Data urodzenia (data), Matka (odwołanie do Bydło) |
| Hardstyle | Labele | Kraj (wybór), Rok założenia (liczba) |
| Hardstyle | Artyści | Kraj (wybór), Aliasy (lista tekstów) |
| Hardstyle | Wydania | Label (odwołanie do Labele), Artyści (wiele odwołań do Artyści), Typ (EP/LP/Single), Data wydania (data) |

Relacje typu Label, Artyści, EP to po prostu pola typu **odwołanie** między katalogami tej samej grupy.

## Model

```
Catalog
  Id, GroupId, Name, Description?, TracksStock (bool), Visibility (Private | Public), CreatedAt

FieldDefinition
  Id, CatalogId, Key, Label, Type, IsRequired, Order, IsArchived
  Options (dla Choice/MultiChoice), TargetCatalogId (dla Reference)

Item
  Id, CatalogId, Name, Values (JSON: { fieldId: wartość }), CreatedBy, CreatedAt, UpdatedAt

Stock                          (tylko gdy Catalog.TracksStock)
  ItemId, LocationId, Quantity
```

Typy pól na start: `Text`, `LongText`, `Integer`, `Decimal`, `Money`, `Date`, `Boolean`, `Choice`, `MultiChoice`, `Reference`, `MultiReference`. Zdjęcia jako osobny krok później.

Każdy wpis ma zawsze `Name`, żeby listy, wyszukiwanie i odwołania działały bez konfiguracji.

## Reguły

- Wartości walidowane w Domain względem definicji pól (typ, wymagalność, dozwolone opcje, istnienie wpisu w odwołaniu).
- Pola z danymi nie są usuwane, tylko archiwizowane. Zmiana typu tylko między zgodnymi typami (np. Integer na Decimal); inaczej nowe pole.
- Ilości są per lokalizacja (`Stock`), więc ten sam katalog działa w domu z jedną lokalizacją i w firmie z kilkoma.
- Katalog z `TracksStock = false` to czysty katalog informacji (np. Artyści) bez ilości.

## Widoczność

Na start wszystkie katalogi są prywatne (`Visibility = Private`), widoczne tylko dla członków grupy.
Docelowo katalog może być publiczny, wspólny dla wielu użytkowników jak na Discogs. Pole jest w modelu od początku, żeby później nie migrować danych; reguły publicznych katalogów (kto edytuje, moderacja, jak prywatna kolekcja odwołuje się do publicznego wpisu) zaprojektujemy, gdy do tego dojdziemy.

## Szablony

Gotowe zestawy katalogów do wyboru w kreatorze grupy, np. "Piwniczka z winem" albo "Kolekcja Hardstyle" (Labele, Artyści, Wydania). Szablon to dane (JSON), nie kod: przy wyborze kopiujemy definicje do grupy i od tej chwili użytkownik może je dowolnie zmieniać.

## Technicznie

- SQLite: `Item.Values` jako kolumna JSON; filtrowanie i sortowanie po polach przez funkcje JSON SQLite w zapytaniach EF Core.
- Formularze i listy w Blazor budowane dynamicznie z definicji pól (jeden komponent edytora na typ pola) w projekcie `UI`, więc web i mobile mają je za darmo.
