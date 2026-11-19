# academy-whatsapp-bot

WhatsApp bot (proof of concept) for a sports academy: parents text **"saldo"** and instantly get their children's outstanding balance and a payment link.

> 🚧 Work in progress. The demo runs locally on demand (ASP.NET Core + ngrok).

## Stack

- .NET 10, ASP.NET Core Minimal API
- WhatsApp Cloud API (Meta) webhooks
- xUnit, GitHub Actions

## Roadmap

- **Phase 1 – Rule-based bot (current):** balance check by keywords, runs locally on demand.
- **Phase 2 – AI agent:** LLM with tool calling to query real data, human handoff, guardrails.
- **Phase 3 – Proactive & real data:** payment reminders with templates, Google Sheets, receipt handling.

## License

[MIT](LICENSE)
