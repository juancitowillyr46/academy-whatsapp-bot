# Demo checklist

The bot runs locally on demand. This guide covers the one-time setup and the steps to turn the demo on.

> Meta's dashboard changes often; menu names may differ slightly.

## One-time setup

### 1. Meta app

1. In [Meta for Developers](https://developers.facebook.com), create an app with the use case **"Connect with customers through WhatsApp"** and link it to a business portfolio.
2. In **WhatsApp → API Setup**, note the **Phone number ID**, the **WhatsApp Business Account ID** and the Graph API version shown in the sample request.
3. Add your phone as a **test recipient** (the test number can only message numbers in that list).
4. In **App settings → Basic**, copy the **App secret**.
5. Optional but recommended: create a **System User** in Meta Business settings, assign it the app and the WhatsApp account, and generate a token with `whatsapp_business_messaging` and `whatsapp_business_management`. The token from API Setup expires within hours.

### 2. Local secrets

From `src/AcademyWhatsAppBot.Api`:

```bash
dotnet user-secrets set "WhatsApp:AccessToken" "<token>"
dotnet user-secrets set "WhatsApp:PhoneNumberId" "<phone-number-id>"
dotnet user-secrets set "WhatsApp:AppSecret" "<app-secret>"
dotnet user-secrets set "WhatsApp:VerifyToken" "<any-string-you-choose>"
dotnet user-secrets set "WhatsApp:GraphApiVersion" "<vXX.X>"
```

Never put these values in `appsettings.json`.

### 3. ngrok

1. Create a free account at [ngrok.com](https://ngrok.com), install the agent and run `ngrok config add-authtoken <authtoken>`.
2. Claim your free **static domain** in the ngrok dashboard so the webhook URL never changes.

### 4. Webhook in Meta

With the API and ngrok running (see below):

1. **WhatsApp → Configuration → Webhook**: set the callback URL to `https://<your-domain>/webhook` and the same `VerifyToken`, then **Verify and save**.
2. Subscribe to the **messages** webhook field.
3. Make sure the app is subscribed to the WhatsApp Business Account. If messages never reach the API, run:

   ```bash
   curl -X POST "https://graph.facebook.com/<version>/<waba-id>/subscribed_apps" \
        -H "Authorization: Bearer <token>"
   ```

   `GET` on the same URL lists the subscribed apps; your app must appear there.

## Every demo (about 5 minutes)

1. Terminal 1: `dotnet run --project src/AcademyWhatsAppBot.Api` (listens on port 5000).
2. Terminal 2: `ngrok http 5000 --url https://<your-domain>`.
3. Open `https://<your-domain>/` and check it says `academy-whatsapp-bot is running`.
4. Send a message from your phone to the test number and confirm the reply before the demo starts.
5. When finished, stop ngrok and the API with `Ctrl+C`.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| API does not start: `The X field is required` | Missing User Secret |
| Meta cannot verify the webhook | API or ngrok not running, wrong URL, or `VerifyToken` mismatch (`Webhook verification rejected` in the log) |
| Webhook verified but messages never arrive | Not subscribed to **messages**, or the app is not subscribed to the WhatsApp Business Account (see step 4.3). The ngrok inspector at `http://127.0.0.1:4040` shows whether Meta sent anything |
| `401` / `Webhook rejected: invalid signature` | Wrong `AppSecret` |
| Message arrives but no reply, error `190` | Access token expired |
| Message arrives but no reply, error `131030` | Recipient not in the test number's allow list |
| The bot answers twice | Deduplication not working or the webhook responded too slowly |
| Browser shows an ngrok warning page | Normal on the free plan; it only affects browsers, not Meta's requests |
