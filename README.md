# TestCaseSmartDocs

Расширение Docsvision: запрос контрагента из CRM, запись JSON в `CounterpartyInfo` и окно WebClient с данными, загрузкой и ошибками.

## Требования и настройка

.NET SDK 8, Node.js 22.14+, Docsvision 6.1 / WebClient 18. Версии SDK закреплены в проектах; для другого поколения Docsvision потребуется адаптация.

1. Перенесите `CounterpartyIntegration` из `Server/appsettings.example.json` в конфигурацию WebClient. Укажите URL CRM и реальный `SectionAlias` (либо `SectionId`) секции с текстовым полем `CounterpartyInfo`.
2. CRM должна отвечать на `GET /api/counterparties/{id}` объектом с обязательными строками `id`, `name`; необязательные поля: `inn`, `kpp`, `legal_address`, `phone`, `email`. В ответе WebClient адрес называется `legalAddress`.
3. Соберите проект командами ниже. В каталоге сайта WebClient поместите серверную `SmartDocs.CounterpartyIntegration.dll` в `Extensions`, а `Client/dist/extension.js` — в `Content/Modules/SmartDocs.CounterpartyIntegration/`. Перезапустите WebClient.
4. В Конструкторе Web-разметок добавьте кнопку и назначьте её событию нажатия обработчик `showCounterparty`. В сохранённой карточке кнопка открывает окно ввода ID и загрузки данных.

Серверный маршрут: `POST /api/Counterparty/GetCounterpartyForCard`, JSON `{ "cardId": "GUID", "counterpartyId": "42" }`. Используются сессия текущего пользователя, его права и стандартный ответ SDK с обновлением timestamp карточки.

## Локальный предпросмотр без Docsvision

Из корня проекта в PowerShell:

```powershell
cd Client
npm.cmd ci
npm.cmd run preview
```

Откройте http://127.0.0.1:4173. Выберите тестовый ответ CRM и задержку, нажмите «Открыть окно контрагента»,
затем «Получить тестовые данные». ID `42` уже подставлен; можно ввести любой непустой ID.
Доступны полные и неполные данные, отсутствие контрагента, недоступность CRM, ошибка соединения и некорректный ответ.
Задержка до 10 секунд позволяет проверить загрузку и блокировку повторного запроса. После ошибки можно повторить запрос;
для смены сценария закройте окно. Остановка сервера — `Ctrl+C`. Другой порт: `$env:PORT = '4174'` перед запуском.

Предпросмотр использует настоящий компонент диалога и клиентскую проверку ответа, заменяя транспорт тестовыми данными в браузере.
Docsvision, сервер .NET и CRM не нужны. Реальная запись в `CounterpartyInfo`, права доступа и серверная интеграция здесь не проверяются.
Тема и общие стили Docsvision не подключены, поэтому оформление в WebClient может отличаться.
Сервер слушает только `127.0.0.1`; сборка предпросмотра находится в `Client/dist/preview.js` отдельно от `extension.js`.

## Запуск тестов

Клиентские тесты, включая предпросмотр, без Docsvision и CRM (из корня проекта):

```powershell
cd Client
npm.cmd ci
npm.cmd run build
npm.cmd test
```

Сборка нужна для тестов готовых JS-файлов. Все серверные и клиентские тесты: из корня проекта запустите
`powershell -NoProfile -File scripts/Test.ps1` (требуются .NET SDK и установленные зависимости клиента).

## Сборка и проверка

Из корня проекта в PowerShell:

```powershell
dotnet build Server/SmartDocs.CounterpartyIntegration/SmartDocs.CounterpartyIntegration.csproj -c Release
Push-Location Client; npm ci; Pop-Location
powershell -NoProfile -File scripts/Test.ps1
powershell -NoProfile -File scripts/Pack.ps1
```

Тесты проверяют HTTP/JSON, ошибки карточек через моки настоящего SDK, маршрут и интерфейс. Сквозной запуск требует настроенных Docsvision и CRM. Архив исходников с пояснениями: `artifacts/TestCaseSmartDocs.zip`.

Основа подключения: [официальные примеры WebClient](https://github.com/Docsvision/WebClient-Samples/tree/master/ServerExtensions/CreateCard) и [API записи карточки](https://help.docsvision.com/dv5/programmer/dv5/cards/edit-card/).
