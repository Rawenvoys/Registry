# Schemat bazy (ustalenia z 2026-10-03, uzupełnione 2026-10-04)

Aktualny zakres MVP; tam, gdzie różni się od [katalogi.md](katalogi.md) (typy pól, wartości w JSON, stany), obowiązuje ten plik. Diagram: [schemat-bazy.svg](schemat-bazy.svg) (pokazuje tylko katalogi i wpisy).

```
-- konta i grupy (konta-i-grupy.md)
AspNetUsers      (Id, Email, DisplayName?, CreatedAt, ...)                 -- ASP.NET Core Identity
Groups           (Id, Name, CreatedAt)
Memberships      (GroupId → Groups, UserId, Role: Owner | Admin | Member, JoinedAt)   -- PK (GroupId, UserId)
Locations        (Id, GroupId → Groups, Name, Address?, IsDefault)
Invitations      (Id, GroupId → Groups, Email?, Role, Code (unikalny), CreatedBy, CreatedAt, ExpiresAt, AcceptedAt?, AcceptedBy?)

-- katalogi i wpisy
Catalogs         (Id, GroupId → Groups, Name, CreatedAt)
Publishers       (Id, CatalogId → Catalogs, Name)
Items            (Id, CatalogId → Catalogs, Title, ReleaseDate?, PublisherId? → Publishers, CreatedAt)

-- później
FieldDefinitions (Id, CatalogId → Catalogs, Name)
ItemFieldValues  (ItemId → Items, FieldDefinitionId → FieldDefinitions, Value)
MovementTypes    (Id, CatalogId → Catalogs, Name, Effect: Increase | Decrease | None)
Movements        (Id, ItemId → Items, MovementTypeId → MovementTypes, Date, Note?)
```

Usuwanie:
- Grupa usuwa swoje członkostwa, lokalizacje, zaproszenia i katalogi (cascade).
- Katalog usuwa swoje wpisy i wydawców (cascade).
- Usunięcie wydawcy zostawia wpisy, tylko czyści im `PublisherId` (SET NULL).

Decyzje:
- Brak tabel domenowych (wino, płyta); wszystko w ogólnych tabelach.
- Items: wspólne kolumny Title, ReleaseDate, Publisher ("Wydawca", nie "Producent", bo w muzyce producent to osoba). Zmiana etykiet wspólnych pól per katalog (np. "Winnica" zamiast "Wydawca") później.
- `ReleaseDate` to data częściowa zapisana tekstem: `2020`, `2020-05` albo `2020-05-17` (typ `PartialDate` w Domain pilnuje formatu). Nie DateTime/DateOnly, bo sam rocznik musiałby udawać 1 stycznia. Tekst w tym formacie sortuje się jak data.
- Publishers per katalog (żeby przy winach nie podpowiadało labeli muzycznych). Wpis może wskazywać tylko wydawcę z własnego katalogu; pilnuje tego Domain (`Item`), baza sprawdza tylko, że wydawca istnieje. Złożony FK `(CatalogId, PublisherId)` odrzucony, bo przy SET NULL SQLite zerowałby też `CatalogId`.
- Nazwa katalogu unikalna w grupie (bez względu na wielkość liter) pilnowana w kodzie, nie indeksem.
- Pola własne odłożone na później (Łukasz, 2026-10-03). Gdy wrócą: `FieldDefinitions` tylko z nazwą, wartości tekstowe w tabeli `ItemFieldValues` (nie JSON).
- Brak liczby sztuk na rekordzie. Stany i ruchy są opcją katalogu (katalog bez typów ruchów, np. baza wydań hardstyle, nie ma stanów). Odłożone na później, tak samo jak to, czy ruch ma ilość, cenę i lokalizację.
- Usunięte (Łukasz, 2026-10-03): `Groups.Kind` (lokalizacje pokazujemy dopiero, gdy grupa ma więcej niż jedną) i `Catalogs.Visibility` (publiczne katalogi dodadzą kolumnę migracją z domyślnym Private, gdy do nich dojdziemy).

Otwarte:
- `Memberships.UserId`, `Invitations.CreatedBy` i `AcceptedBy` nie mają FK do `AspNetUsers`, więc usunięcie konta zostawi osierocone wiersze.
