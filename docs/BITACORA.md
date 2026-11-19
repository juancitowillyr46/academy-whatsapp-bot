# Bitácora del proyecto `academy-whatsapp-bot`

Registro de lo que se ha hecho, las decisiones tomadas y por qué, los problemas encontrados y lo que queda pendiente. Fechas: 18 y 19 de noviembre de 2026.

> Esta bitácora no contiene secretos ni datos personales. Los valores reales (tokens, App Secret, IDs de Meta, número de teléfono, dominio de ngrok) viven solo en User Secrets y en archivos ignorados por Git.

---

## Resumen

| | |
|---|---|
| **Qué es** | Bot de WhatsApp (POC de portafolio) para una academia deportiva: el acudiente pregunta por su saldo y recibe el estado de cuenta de sus hijos con el link de pago |
| **Repositorio** | https://github.com/juancitowillyr46/academy-whatsapp-bot (público, CI en verde) |
| **Stack** | .NET 10 LTS, ASP.NET Core Minimal API, WhatsApp Cloud API (Meta), ngrok, Ollama (`qwen2.5:3b`), Microsoft.Extensions.AI, xUnit, GitHub Actions |
| **Estado** | Fase 1 (bot por reglas) y fase 2 (IA local) completas y publicadas |
| **Pruebas** | 72 (unitarias e integración), sin necesidad de secretos, red ni modelo |
| **Evaluación de IA** | 40 mensajes etiquetados: 77,5 % → 90 % |

---

## 1. Análisis y planificación (18 nov)

- Se analizó el handoff original (`HANDOFF (1).md`). Era sólido pero pensado para un proyecto más grande: tres proyectos, `IIntentDetector`, `FlowRouter`, varias capas "para el futuro".
- **Decisión:** hacer un **POC lo más simple posible** que sirva de portafolio. Monolito con dos proyectos (`Api` + `Tests`) organizado por carpetas de funcionalidad; sin abstracciones que todavía no hagan falta.
- **Nombre del repo:** se descartó `whatsapp-ai-assistant` (genérico y prometía IA que no existía) y luego `academy-whatsapp-bot-ai`. Quedó **`academy-whatsapp-bot`**.
- El proyecto se creó en `C:\Data\Source\Repos\academy-whatsapp-bot` y el handoff simplificado quedó en [`docs/HANDOFF.md`](HANDOFF.md).

## 2. Configuración de Meta y ngrok (18–19 nov)

- **Meta for Developers:** app creada con el caso de uso de WhatsApp, número de prueba, celular propio como destinatario de prueba, App Secret y token de usuario del sistema (permanente).
  - Problema: al generar el token salía *"No hay permisos disponibles"*. **Causa:** la app no estaba asignada al usuario del sistema. **Solución:** asignar la app y la cuenta de WhatsApp con control total.
- **Secretos:** se guardaron por error en `appsettings.json` (archivo que se sube al repo). Se detectó antes del primer commit y se movieron a **User Secrets**; `appsettings.json` quedó con valores vacíos.
- **ngrok:** winget instalaba una versión antigua (3.3.1); se instaló desde la Microsoft Store (3.39) y se configuró un dominio estático gratuito. La página de aviso de ngrok solo afecta a navegadores, no a las peticiones de Meta.
- **Webhook:** verificado en Meta y suscrito al campo `messages`.
  - Problema: los mensajes nunca llegaban. **Causa:** la app no estaba suscrita a la cuenta de WhatsApp Business (WABA); solo lo estaba una app interna de Meta. **Solución:** `POST /{waba-id}/subscribed_apps` con el token. Documentado en [`demo-checklist.md`](demo-checklist.md).

## 3. Fase 1: bot por reglas (19 nov)

| Paso | Commit | Qué se hizo |
|---|---|---|
| 1–4 | `c145cf0` | Estructura (`.slnx`, `global.json`, `Directory.Build.props`, `.editorconfig`, MIT), opciones validadas al arrancar, `GET /webhook` (verificación) y `POST /webhook` con firma HMAC-SHA256 |
| 5 | `5fa7a16` | `WhatsAppClient`: primer "hola" de ida y vuelta con el celular real |
| 6 | `a689017` | Cola en memoria (`Channel`) + `BackgroundService` + deduplicación (`IMemoryCache`). Documentación de arquitectura y checklist de demo |
| 7 | `8778f48` | Respuesta de saldo: datos JSON ficticios, hermanos en un solo mensaje, formatos `$120.000` y `5 de diciembre`, primeras pruebas unitarias. Datos reales del celular en `data/students.local.json` (ignorado por Git) |
| 8 | `0c03e1b` | Pruebas de integración con `WebApplicationFactory`, payloads firmados y cliente de WhatsApp falso. Se verificó que detectan errores rompiendo la deduplicación a propósito |
| 9 | `d908ad2` | GitHub Actions: restore, formato, build en Release y pruebas |
| 10 | `8997744`, `5e06754` | README completo, publicación en GitHub, secret scanning y push protection, GIF de la demo |

**Publicación:**
- Antes del push se revisó todo el historial en busca de secretos y se cambió el autor de los commits al **correo noreply de GitHub** para no exponer el correo personal.
- El push falló porque la sesión de `gh` no tenía el permiso `workflow` (necesario para subir `.github/workflows`). Se resolvió con `gh auth login -s workflow`.
- **GIF de la demo:** grabado en el celular y editado con ffmpeg (recorte de barra de estado y teclado, cortes de tiempos muertos; 20 s, ~2 MB). Los nombres del registro propio se cambiaron por ficticios antes de grabar; el video original no se sube.

## 4. Fase 2: IA local (19 nov)

### Decisiones previas

- El plan inicial era usar Claude por API. Se aclaró que **el plan Pro no incluye acceso a la API** (son productos separados) y que **Managed Agents** (agentes alojados por Anthropic) no hacía falta para este caso.
- Sin presupuesto para APIs de pago (ni Claude, ni OpenRouter con modelos de Anthropic, ni `glm-5.2:cloud` de Ollama, que pidió créditos), se eligió **Ollama local y gratuito**, detrás de `Microsoft.Extensions.AI` (`IChatClient`) para poder cambiar de proveedor después.

### Validación de modelos (CPU, Ryzen 7, 16 GB)

| Modelo | Enfoque | Resultado | Tiempo |
|---|---|---|---|
| `llama3.1:8b` | La IA redacta la respuesta con herramientas | **Inventó un monto y una fecha** | 26 s |
| `qwen2.5-coder:3b` | La IA redacta la respuesta con herramientas | 0/6 (escribía las herramientas como texto) | 7 s |
| `qwen2.5-coder:3b` | La IA solo clasifica (JSON) | 11/14 | 3,5 s |
| **`qwen2.5:3b`** | **La IA solo clasifica (JSON)** | **12/14** | **3,7 s** |

**Decisión clave:** la IA **solo clasifica la intención** con un esquema JSON cerrado; **el código escribe todas las respuestas** con los datos del remitente. Así no puede inventar montos, filtrar datos de otra familia ni ser manipulada por *prompt injection*. También se eliminó la intención `other_family`: era confusa para el modelo e innecesaria, porque la respuesta de saldo solo contiene los alumnos de quien escribe.

### Pasos

| Paso | Commit | Qué se hizo |
|---|---|---|
| 1 | `3d6845f` | `IIntentClassifier` + `KeywordIntentClassifier`; `ReplyService` arma la respuesta a partir de la intención (sin cambiar el comportamiento) |
| 2 | `e929aae` | `LlmIntentClassifier` (Ollama, JSON schema, temperatura 0, ejemplos en el prompt) y `HybridIntentClassifier` (reglas primero, modelo después, vuelta a reglas si falla o tarda) |
| 3 | `793f85a` | Respuestas para *handoff* (con registro en el log), saludo y fuera de tema |
| 4 | `6e05a52` | Evaluaciones: consola `tools/AcademyWhatsAppBot.Evals`, 40 casos, **77,5 % → 90 %** al pasar saludos y agradecimientos a reglas |
| — | `854ca5d` | Diagrama de arquitectura en el README; acciones de CI actualizadas a `checkout@v7` y `setup-dotnet@v6` (Node 24) |
| — | `cd4eb79` | Roadmap del README detallado con casillas por sub-tarea |

- **Prueba real desde el celular:** "hasta cuándo puedo pagar?" → saldo; "Ya hice la transferencia" → atención humana; "Recomiéndame una película" → redirección. "Buenas tardes" fallaba (→ fuera de tema), lo que motivó la regla de saludos medida en las evaluaciones.
- **Repo:** no se renombró a `academy-whatsapp-agent` porque es un bot con clasificación por IA, no un agente que use herramientas. Se agregaron los topics `llm`, `ollama` y `ai`.

### Incidente corregido

Desde el paso 7 de la fase 1, `secrets.json` cambió a formato plano (`"WhatsApp:AppSecret"`) y las revisiones de secretos previas a cada commit leían el formato anidado, por lo que podían no estar buscando el token ni el App Secret. Se detectó en la fase 2, se repitió la revisión sobre **todo el historial local y de GitHub** con el formato correcto (**ningún secreto publicado**) y las revisiones ahora abortan si algún valor no se carga.

---

## Pendientes

| Pendiente | Estado |
|---|---|
| Regenerar App Secret y token de Meta, y authtoken de ngrok (aparecieron en la conversación de desarrollo) | Pospuesto por decisión del dueño del proyecto |
| Fase 3: memoria corta por número, conjunto de evaluación con frases nuevas, modelo alojado opcional | Próximo paso |
| Fase 4: recordatorios con plantillas, Google Sheets, recepción de comprobantes | Futuro |

## Cómo retomar

1. Leer [`architecture.md`](architecture.md) y [`demo-checklist.md`](demo-checklist.md).
2. Encender la demo: `dotnet run --project src/AcademyWhatsAppBot.Api` + `ngrok http 5000 --url https://<dominio>`, con Ollama activo.
3. Antes de cambiar la IA, medir con `dotnet run --project tools/AcademyWhatsAppBot.Evals -- --label <nombre>` y comparar con [`evals/README.md`](../evals/README.md).
