# Schemat bazy (ustalenia z 2026-10-03)

Stan po rozmowie z Łukaszem w wątku o modelu rekordów. To jest aktualny zakres MVP; tam, gdzie różni się od [katalogi.md](katalogi.md) (typy pól, wartości w JSON, stany), obowiązuje ten plik. Diagram: [schemat-bazy.svg](schemat-bazy.svg).

```
Users            (Id, Email, DisplayName, ...)                       -- ASP.NET Core Identity, z konta-i-grupy.md
Groups           (Id, Name)                                         -- z konta-i-grupy.md, bez Kind
Memberships      (GroupId → Groups, UserId → Users, Role: Owner | Admin | Member, JoinedAt)
Catalogs         (Id, GroupId → Groups, Name, CreatedAt)
Publishers       (Id, CatalogId → Catalogs, Name)
Items            (Id, CatalogId → Catalogs, Title, ReleaseDate?, PublisherId? → Publishers, CreatedAt)

-- później
FieldDefinitions (Id, CatalogId → Catalogs, Name)
ItemFieldValues  (ItemId → Items, FieldDefinitionId → FieldDefinitions, Value)
MovementTypes    (Id, CatalogId → Catalogs, Name, Effect: Increase | Decrease | None)
Movements        (Id, ItemId → Items, MovementTypeId → MovementTypes, Date, Note?)
```

Decyzje:
- Brak tabel domenowych (wino, płyta); wszystko w ogólnych tabelach.
- Items: wspólne kolumny Title, ReleaseDate (może być sam rok), Publisher ("Wydawca", nie "Producent", bo w muzyce producent to osoba). Zmiana etykiet wspólnych pól per katalog (np. "Winnica" zamiast "Wydawca") później.
- Publishers per katalog (żeby przy winach nie podpowiadało labeli muzycznych). Wpis może wskazywać tylko wydawcę z własnego katalogu (pilnuje kod).
- Pola własne odłożone na później (Łukasz, 2026-10-03). Gdy wrócą: `FieldDefinitions` tylko z nazwą, wartości tekstowe w tabeli `ItemFieldValues` (nie JSON), na co Łukasz się zgodził.
- Brak liczby sztuk na rekordzie. Stany i ruchy są opcją katalogu (katalog bez typów ruchów, np. baza wydań hardstyle, nie ma stanów). Odłożone na później.
- Usunięte (Łukasz, 2026-10-03): `Groups.Kind` (lokalizacje pokazujemy dopiero, gdy grupa ma więcej niż jedną) i `Catalogs.Visibility` (publiczne katalogi dodadzą kolumnę migracją z domyślnym Private, gdy do nich dojdziemy).
