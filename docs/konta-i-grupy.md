# Konta, logowanie i grupy

Stan: wersja 3, logowanie zaimplementowane (2026-10-02). Uwzględnia: logowanie Google/Facebook/Microsoft z parowaniem kont, brak domyślnej grupy, kreator na start. Model katalogów opisuje [katalogi.md](katalogi.md).

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
| Brak konta z tym e-mailem, dostawca potwierdza e-mail | Tworzymy konto bez hasła, e-mail potwierdzony, dodajemy UserLogin |
| Jest konto z tym e-mailem, dostawca potwierdza e-mail (`email_verified`), konto lokalne ma potwierdzony e-mail | **Parujemy automatycznie** i logujemy |
| Jest konto z tym e-mailem, ale lokalnie e-mail **niepotwierdzony** | Parujemy, ale usuwamy hasło i unieważniamy sesje tego konta (patrz niżej) |
| Jest konto z tym e-mailem, ale dostawca nie potwierdza e-maila | Nie parujemy (409 `email_not_verified`); użytkownik loguje się hasłem |
| Brak konta, dostawca nie potwierdza e-maila | Tworzymy konto z niepotwierdzonym e-mailem; logowanie po potwierdzeniu linkiem (`/auth/resendConfirmationEmail`) |
| Dostawca nie zwraca e-maila (zdarza się w Facebooku) | Odrzucamy (400 `email_required`); podanie e-maila ręcznie dojdzie później |

**Dlaczego usuwamy hasło przy niepotwierdzonym koncie:** ktoś mógłby wcześniej założyć konto na cudzy e-mail z własnym hasłem i czekać. Gdy właściciel e-maila zaloguje się przez Google, bez tego kroku napastnik nadal miałby dostęp hasłem do sparowanego konta.

**Microsoft:** dla kont firmowych (Entra ID) claim `email` nie jest weryfikowany przez Microsoft. Automatycznie parujemy tylko konta osobiste Microsoft albo gdy token zawiera potwierdzenie domeny (`xms_edov`); w pozostałych przypadkach e-mail traktujemy jako niepotwierdzony.

W ustawieniach konta: lista powiązanych logowań, możliwość odłączenia (o ile zostaje hasło albo inne logowanie) i ustawienia hasła dla konta społecznościowego.

### Jak to działa technicznie

- Klient (web lub mobile) loguje się u dostawcy i dostaje jego token (Google/Microsoft/Apple: ID token, Facebook: access token).
- Wysyła go na `POST /auth/external/{provider}`. API weryfikuje token u dostawcy, stosuje reguły z tabeli i wydaje **własne** tokeny dostępu i odświeżania.
- Logowanie hasłem: endpointy ASP.NET Core Identity (`MapIdentityApi`), te same tokeny.
- Dzięki temu web i aplikacja mobilna korzystają z jednego API, bez przekierowań i ciasteczek po stronie API.
- Apple warto dodać razem z aplikacją na iOS.

## 2. Grupy

Brak domyślnej grupy. Po pierwszym logowaniu, jeśli użytkownik nie należy do żadnej grupy, widzi **kreator**:

1. Utwórz grupę: sama nazwa. Rodzaju grupy nie ma (decyzja z 2026-10-03): dom i firma działają tak samo.
2. Kolejne lokalizacje (sklepy, magazyny, oddziały) można dodać później.
3. Albo: dołącz do istniejącej grupy kodem/linkiem z zaproszenia.

Użytkownik może należeć do wielu grup i przełącza aktywną grupę w aplikacji.

```
Group
  Id, Name, CreatedAt

Membership
  GroupId, UserId, Role (Owner | Admin | Member), JoinedAt
  unikalne (GroupId, UserId); każda grupa ma co najmniej jednego Ownera

Location
  Id, GroupId, Name, Address?, IsDefault

Invitation
  Id, GroupId, Email?, Role, Code, ExpiresAt, AcceptedAt?
```

Role: `Owner` (wszystko, w tym usunięcie grupy i przekazanie własności), `Admin` (członkowie, lokalizacje, katalogi), `Member` (praca na wpisach).
Grupa zawsze ma co najmniej jednego Ownera: ostatni Owner nie może odejść ani zmienić sobie roli, dopóki nie nada jej komuś innemu.
Każda grupa dostaje przy tworzeniu jedną domyślną lokalizację o nazwie grupy. UI pokazuje lokalizacje dopiero wtedy, gdy grupa ma więcej niż jedną.

API (zaimplementowane):

| Endpoint | Co robi |
|---|---|
| `GET /groups` | Grupy zalogowanego użytkownika z jego rolą; pusta lista oznacza, że klient pokazuje kreator |
| `POST /groups` | Tworzy grupę z domyślną lokalizacją |
| `GET /groups/{id}` | Członkowie i lokalizacje; 404 dla grup, do których użytkownik nie należy |
| `PUT /groups/{id}` | Zmiana nazwy (Owner, Admin) |
| `DELETE /groups/{id}` | Usuwa grupę z katalogami, członkostwami i zaproszeniami (Owner) |
| `PUT /groups/{id}/members/{userId}` | Zmiana roli: Owner zmienia każdemu, Admin tylko między Member i Admin |
| `DELETE /groups/{id}/members/{userId}` | Usunięcie członka (Owner każdego, Admin tylko Membera); własne id oznacza wyjście z grupy |
| `POST /groups/{id}/locations` | Kolejna lokalizacja (Owner, Admin) |
| `GET /groups/{id}/invitations` | Aktywne zaproszenia: nieużyte i niewygasłe (Owner, Admin) |
| `POST /groups/{id}/invitations` | Kod zaproszenia ważny 7 dni, opcjonalnie tylko dla podanego e-maila (Owner, Admin; zaprosić Ownera może tylko Owner) |
| `DELETE /groups/{id}/invitations/{code}` | Unieważnia kod (Owner, Admin) |
| `POST /invitations/{code}/accept` | Dołącza zalogowanego użytkownika z rolą z zaproszenia; kod działa raz |

## 3. Ekrany

Wspólne dla web i mobile, w `src/PresentationKit`:

| Ekran | Adres | Co robi |
|---|---|---|
| Logowanie | `/login` | E-mail i hasło albo przycisk dostawcy; przy niepotwierdzonym e-mailu pozwala wysłać link ponownie |
| Rejestracja | `/register` | Zakłada konto i prosi o kliknięcie linku z wiadomości |
| Kreator | `/setup` | Nowa grupa (sama nazwa) albo dołączenie kodem; `/setup?code=…` od razu wypełnia kod |
| Start | `/` | Lista grup; gdy jest pusta, przekierowuje do kreatora |
| Grupa | `/groups/{id}` | Zakładki: katalogi, członkowie z zaproszeniami, ustawienia (nazwa, wyjście, usunięcie) |
| Katalog | `/groups/{id}/catalogs/{catalogId}` | Nazwa, zmiana nazwy i usunięcie; tu pojawią się wpisy |

Sesja (token dostępu i odświeżania) jest zapisywana w `localStorage` na webie i w `SecureStorage` na telefonie, a token dostępu odświeża się sam na minutę przed wygaśnięciem.
Logowanie u dostawcy na webie otwiera okienko (OAuth implicit flow), które wraca na `signin-callback.html`. W aplikacji mobilnej przyciski dostawców pojawią się, gdy dodamy logowanie przez `WebAuthenticator`.

## 4. Furtka na firmy z oddziałami

- Stany ilościowe wpisów zawsze wiszą na `Location`, nie na `Group`. Dom ma jedną lokalizację, sieć sklepów ma ich kilka, model ten sam.
- Ograniczenie pracownika do wybranych lokalizacji dodamy później jako `LocationAccess(UserId, LocationId, Role)`, bez zmian w istniejących tabelach.

## 5. Decyzje techniczne

- .NET 10, ASP.NET Core minimal API, EF Core, SQLite.
- Na Kubernetesie: jedna replika API z plikiem bazy na wolumenie trwałym (SQLite nie obsługuje zapisu z wielu podów). Migracje uruchamiają się przy starcie API.
- ASP.NET Core Identity dla kont, haseł, potwierdzeń e-mail i blokad.
- Warstwy: Domain, Application, Infrastructure, Api. Bez MediatR i bez repozytoriów nad EF Core dla prostych operacji.
