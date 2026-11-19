# Handoff: `academy-whatsapp-bot` – POC de consulta de saldo

> Versión simplificada del handoff original. Objetivo: un **POC sencillo**, que funcione en local para demos y que sirva como proyecto de portafolio (hoja de vida + GitHub).

**Repositorio:** `academy-whatsapp-bot` (público).
**Stack:** .NET 10 (C#), ASP.NET Core Minimal API, WhatsApp Cloud API (Meta), ngrok.
**Ejecución:** local, encendido solo para demos. Sin nube, sin Docker.
**IA:** no en esta fase. Solo aparece en el Roadmap del README. Si se agrega un agente (LLM + tool calling), el repo se renombra a `academy-whatsapp-agent`.

---

## 1. Caso de uso

**El acudiente escribe "saldo" → el bot responde cuánto debe cada alumno y su link de pago.** Cualquier otro mensaje → texto de ayuda.

1. Meta envía el mensaje a `POST /webhook` (vía ngrok).
2. La API valida la firma, encola el mensaje y responde `200` de inmediato.
3. Un `BackgroundService` procesa el mensaje: busca los alumnos activos por teléfono y responde.

## 2. Principio: lo más simple posible

- **Monolito simple:** un proyecto API + un proyecto de pruebas, organizado por carpetas de funcionalidad.
- Sin capas ni abstracciones "para el futuro". Solo dos interfaces, porque hacen falta para las pruebas: `IStudentRepository` e `IWhatsAppClient`.
- Detección de intención: `if` con palabras clave. Sin detectores, routers ni flujos.
- Sin lógica de casos especiales (vencidos, estados mixtos, etc.).

## 3. Convenciones

- **Todo el código, nombres, comentarios, logs, commits y el README en inglés.** Convenciones estándar de C#.
- **Solo los textos que ve el usuario de WhatsApp en español**, centralizados en `Balance/Messages.cs`.
- Columnas de datos en `snake_case` (`guardian_phone`), mapeadas con `JsonNamingPolicy.SnakeCaseLower`.
- `Nullable` y `TreatWarningsAsErrors` activados (`Directory.Build.props`). `.editorconfig` + `dotnet format`.
- Commits pequeños, en inglés e imperativo (`Add webhook verification`).

## 4. Estructura

```
academy-whatsapp-bot/
├── .github/workflows/ci.yml          # build + format check + tests
├── data/students.json                # fake data only
├── docs/demo-checklist.md            # steps to turn the demo on
├── src/AcademyWhatsAppBot.Api/
│   ├── Program.cs                    # DI, options, endpoints
│   ├── Webhook/                      # GET verify, POST receive, signature, payload DTOs
│   ├── WhatsApp/                     # WhatsAppOptions, IWhatsAppClient, WhatsAppClient
│   ├── Processing/                   # MessageQueue (Channel), MessageWorker (BackgroundService)
│   ├── Balance/                      # BalanceService (builds the reply), Messages (Spanish texts + keywords)
│   ├── Data/                         # Student, IStudentRepository, JsonStudentRepository
│   └── Common/                       # PhoneNormalizer, TextNormalizer, formatting helpers
├── tests/AcademyWhatsAppBot.Tests/
│   ├── Fixtures/                     # sample Meta payloads
│   ├── Unit/
│   └── Integration/                  # WebApplicationFactory + fake IWhatsAppClient
├── .editorconfig  .gitignore  Directory.Build.props  global.json
├── LICENSE (MIT)  README.md
└── AcademyWhatsAppBot.slnx
```

## 5. Reglas de respuesta

- **Intención:** el texto normalizado (minúsculas, sin tildes, espacios colapsados) contiene `saldo`, `cuanto debo` o `deuda` → saldo. Si no → ayuda. Mensajes que no son de texto → ayuda.
- **Alumnos:** se buscan por `guardian_phone` y se ignoran los inactivos. Si no queda ninguno → "no registrado".
- **Un solo mensaje** con todos los alumnos del acudiente. Cada línea es independiente:
  - con saldo: `• {studentName} – {period}: ${balance} (vence el {dueDate})` + `Paga aquí: {paymentLink}`
  - saldo 0: `• {studentName} – al día ✅`
- Saludo con el `guardian_name` del primer alumno.

Textos (en `Messages.cs`):

```
Hola {guardianName} 👋
Este es el estado de cuenta:

• Juan Pérez – Noviembre 2026: $120.000 (vence el 5 de diciembre)
  Paga aquí: https://example.com/pay/A001
• Ana Pérez – al día ✅

Si ya pagaste, envía el comprobante por este chat.
```

```
No encontramos tu número registrado en la academia.
Por favor comunícate con la administración para actualizar tus datos.
```

```
Hola 👋 Escribe *saldo* para consultar tu estado de cuenta.
Si necesitas otra cosa, en breve te atiende una persona.
```

Formato `es-CO`: moneda `"$" + balance.ToString("N0", es-CO)` → `$120.000`; fecha `ToString("d 'de' MMMM", es-CO)` → `5 de diciembre`.

## 6. Datos (`data/students.json`)

Campos: `student_id`, `student_name`, `guardian_name`, `guardian_phone` (ej. `573001234567`), `period`, `balance` (`decimal`), `due_date` (`DateOnly`), `payment_link`, `is_active`.

Datos ficticios: un acudiente con 1 alumno con saldo, uno con 2 hermanos, uno al día y uno inactivo. Tu número real va en `data/students.local.json` (ignorado por Git), seleccionado con `Data:FilePath`. La ruta se resuelve relativa a `ContentRootPath`.

## 7. Configuración

`appsettings.json` sin secretos. Secretos con User Secrets:

```
dotnet user-secrets set "WhatsApp:AccessToken" "<token>"
dotnet user-secrets set "WhatsApp:PhoneNumberId" "<phone-number-id>"
dotnet user-secrets set "WhatsApp:VerifyToken" "<any-string>"
dotnet user-secrets set "WhatsApp:AppSecret" "<app-secret>"
dotnet user-secrets set "WhatsApp:GraphApiVersion" "<current-version>"
```

`WhatsAppOptions` con `ValidateOnStart`. Las pruebas de integración inyectan valores falsos.

## 8. Detalles técnicos que sí se mantienen

- **GET /webhook:** si `hub.mode == "subscribe"` y `hub.verify_token` coincide → `200` con `hub.challenge` en texto plano; si no → `403`.
- **Firma:** `X-Hub-Signature-256` = HMAC-SHA256 del cuerpo crudo con `AppSecret`. Comparar con `CryptographicOperations.FixedTimeEquals`. Inválida → `401`.
- **Payload:** recorrer `entry[].changes[].value.messages[]`. Ignorar `statuses`.
- **Respuesta rápida:** encolar en `Channel<IncomingMessage>` y responder `200` al instante.
- **Deduplicación:** por `message.id` con `IMemoryCache` (unas horas). Un solo consumidor, sin condiciones de carrera.
- **Errores:** se registran en log y no tumban el worker. Loguear el código de error de la Graph API.
- **Privacidad:** teléfonos enmascarados en logs (`57300***4567`). Nunca datos reales ni secretos en el repo.
- **Envío:** `POST https://graph.facebook.com/{version}/{phoneNumberId}/messages` con `HttpClient` tipado. Verificar el formato vigente en la documentación de Meta.

## 9. Fuera de alcance

IA o agentes, Google Sheets, plantillas y recordatorios, recepción de comprobantes, nube, Docker, panel administrativo, multi-idioma.

## 10. Orden de trabajo

1. Estructura del repo: `.gitignore`, `.editorconfig`, `Directory.Build.props`, `global.json`, `LICENSE`, README inicial, solución, proyectos `Api` y `Tests`.
2. `WhatsAppOptions` + User Secrets + `ValidateOnStart`.
3. `GET /webhook` y verificación con ngrok + Meta.
4. `POST /webhook` con validación de firma; solo loguea el mensaje.
5. `WhatsAppClient`: responder "hola" a cualquier mensaje (hola mundo de punta a punta).
6. Cola + worker + deduplicación.
7. Utilidades, datos, `BalanceService` y textos, con pruebas unitarias.
8. Pruebas de integración del webhook.
9. GitHub Actions (`ci.yml`).
10. `docs/demo-checklist.md`, GIF de la demo y README completo (problema, GIF, "runs locally on demand", diagrama, stack, cómo correrlo, roadmap).

## 11. Criterios de aceptación

- [ ] "saldo", "SALDO" y "Hola, ¿cuál es mi saldo?" responden el estado de cuenta.
- [ ] Hermanos → un solo mensaje con ambos. Saldo 0 → "al día". Teléfono desconocido o solo inactivos → "no registrado". Otro texto o tipo → ayuda.
- [ ] `statuses` no generan respuesta; un mensaje duplicado no se responde dos veces; firma inválida → `401`.
- [ ] Formatos `$120.000` y `5 de diciembre`.
- [ ] Error claro al arrancar si falta configuración.
- [ ] Sin secretos ni datos reales en el repo ni en el historial.
- [ ] CI en verde (build, `dotnet format --verify-no-changes`, tests).
- [ ] README en inglés con GIF y roadmap; `LICENSE` MIT; `docs/demo-checklist.md`.

## 12. Roadmap (para el README)

- **Phase 1 – Rule-based bot (current):** balance check by keywords, runs locally on demand.
- **Phase 2 – AI agent:** LLM with tool calling to query real data, human handoff, guardrails.
- **Phase 3 – Proactive & real data:** payment reminders with templates, Google Sheets, receipt handling.

## 13. Instrucciones para el asistente de IA

- Trabajar paso a paso según la sección 10; al terminar cada paso, explicar cómo probarlo.
- No agregar lógica ni abstracciones fuera de este documento sin preguntar.
- Explicaciones en español y breves; código y commits en inglés.
- Verificar contra la documentación vigente de Meta cualquier detalle de la API.
- Antes de cada commit, revisar que no haya secretos ni datos reales.
