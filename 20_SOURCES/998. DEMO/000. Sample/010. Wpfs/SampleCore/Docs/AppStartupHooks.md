# Dreamine App Startup Hooks

Dreamine generates application bootstrap code from `[DreamineEntry]`.

The following optional partial hooks are available:

| Hook | When it runs | Typical use |
|---|---|---|
| `RegisterBefore()` | Before Dreamine initialization | Register custom services before auto-registration |
| `ConfigureDreamine(DreamineWpfOptions options)` | Before `DreamineAppBuilder.Initialize` | Change default region name or bootstrap options |
| `RegisterAfter()` | After Dreamine initialization | Startup navigation or app-specific post setup |
| `ShowMainWindow()` | During startup | Manual window creation when not using `StartupUri` |