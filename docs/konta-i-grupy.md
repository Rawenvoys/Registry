# Konta, logowanie i grupy

Stan: propozycja, wersja 3 (2026-10-02). Uwzględnia: logowanie Google/Facebook/Microsoft z parowaniem kont, brak domyślnej grupy, kreator na start. Model katalogów opisuje [katalogi.md](katalogi.md).

## 1. Konto i logowanie

```
User (ASP.NET Core Identity)
  Id, Email (unikalny), EmailConfirmed, PasswordHash? (null przy koncie tylko społecznościowym),
  DisplayName, CreatedAt

UserLogin (tabela Identity AspNetUserLogins)
  UserId, Provider (Google | Facebook | Microsoft | Apple), ProviderKey (id u dostawcy)
```

Jedno konto = jedna osoba. Konto może mieć hasło, jedno lub kilka powiązanych logowań zewnętrznych, albo oba naraz.

### Reguły logowania zewnętrznego

| Sytuacja | Co robimy |
|---|---|
| Para (Provider, ProviderKey) już istnieje | Logujemy na powiązane konto |
| Brak konta z tym e-mailem | Tworzymy konto bez hasła, e-mail potwierdzony, dodajemy UserLogin |
| Jest konto z tym e-mailem, dostawca potwierdza e-mail (`email_verified`), konto lokalne ma potwierdzony e-mail | **Parujemy automatycznie** i logujemy |
| Jest konto z tym e-mailem, ale lokalnie e-mail **niepotwierdzony** | Parujemy, ale usuwamy hasło i unieważniamy sesje tego konta (patrz niżej) |
| Dostawca nie potwierdza e-maila albo go nie zwraca (zdarza się w Facebooku) | Nie parujemy automatycznie; prosimy o e-mail i potwierdzenie linkiem |

**Dlaczego usuwamy hasło przy niepotwierdzonym koncie:** ktoś mógłby wcześniej założyć konto na cudzy e-mail z własnym hasłem i czekać. Gdy właściciel e-maila zaloguje się przez Google, bez tego kroku napastnik nadal miałby dostęp hasłem do sparowanego konta.

**Microsoft:** dla kont firmowych (Entra ID) claim `email` nie jest weryfikowany przez Microsoft. Automatycznie parujemy tylko konta osobiste Microsoft albo gdy token zawiera potwierdzenie domeny (`xms_edov`); w pozostałych przypadkach jak wiersz ostatni w tabeli.

W ustawieniach konta: lista powiązanych logowań, możliwość odłączenia (o ile zostaje hasło albo inne logowanie) i ustawienia hasła dla konta społecznościowego.

### Jak to działa technicznie

- Klient (web lub mobile) loguje się u dostawcy i dostaje jego token (Google/Microsoft/Apple: ID token, Facebook: access token).
- Wysyła go na `POST /auth/external/{provider}`. API weryfikuje token u dostawcy, stosuje reguły z tabeli i wydaje **własne** tokeny dostępu i odświeżania.
- Logowanie hasłem: endpointy ASP.NET Core Identity (`MapIdentityApi`), te same tokeny.
- Dzięki temu web i aplikacja mobilna korzystają z jednego API, bez przekierowań i ciasteczek po stronie API.
- Apple warto dodać razem z aplikacją na iOS.

## 2. Grupy

Brak domyślnej grupy. Po pierwszym logowaniu, jeśli użytkownik nie należy do żadnej grupy, widzi **kreator**:

1. Utwórz grupę: nazwa + rodzaj (Prywatna, Dom, Firma).
2. Dla firmy: pierwsza lokalizacja (nazwa, adres), można dodać kolejne od razu albo później.
3. Albo: dołącz do istniejącej grupy kodem/linkiem z zaproszenia.

Użytkownik może należeć do wielu grup i przełącza aktywną grupę w aplikacji.

```
Group
  Id, Name, Kind (Personal | Household | Business), CreatedAt

Membership
  GroupId, UserId, Role (Owner | Admin | Member), JoinedAt
  unikalne (GroupId, UserId); każda grupa ma co najmniej jednego Ownera

Location
  Id, GroupId, Name, Address?, IsDefault

Invitation
  Id, GroupId, Email?, Role, Code, ExpiresAt, AcceptedAt?
```

Role: `Owner` (wszystko, w tym usunięcie grupy i przekazanie własności), `Admin` (członkowie, lokalizacje), `Member` (praca na danych modułu).
Dla grup Prywatna i Dom tworzymy jedną domyślną lokalizację, ukrytą w UI.

## 3. Furtka na firmy z oddziałami

- Stany ilościowe wpisów zawsze wiszą na `Location`, nie na `Group`. Dom ma jedną lokalizację, sieć sklepów ma ich kilka, model ten sam.
- Ograniczenie pracownika do wybranych lokalizacji dodamy później jako `LocationAccess(UserId, LocationId, Role)`, bez zmian w istniejących tabelach.

## 4. Decyzje techniczne

- .NET 10, ASP.NET Core minimal API, EF Core, PostgreSQL.
- ASP.NET Core Identity dla kont, haseł, potwierdzeń e-mail i blokad.
- Warstwy: Domain, Application, Infrastructure, Api. Bez MediatR i bez repozytoriów nad EF Core dla prostych operacji.
