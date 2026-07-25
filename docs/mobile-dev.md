# App móvil (BarrieraMoving.Mobile) — guía de desarrollo

## Compilar

```
dotnet build src/BarrieraMoving.Mobile/BarrieraMoving.Mobile.csproj
```

Solo Android por ahora. Para añadir iOS más adelante: editar `<TargetFrameworks>`
a `net10.0-android;net10.0-ios` (compilar iOS requiere un Mac con Xcode).

## Ejecutar en el emulador

1. Arranca el servidor: `dotnet run --project src/BarrieraMoving.Server` (puerto 5070).
2. Arranca el emulador (Visual Studio → Android Device Manager, o
   `emulator -avd <nombre>` desde `C:\Program Files (x86)\Android\android-sdk\emulator`).
3. Despliega la app:
   ```
   dotnet build src/BarrieraMoving.Mobile/BarrieraMoving.Mobile.csproj -t:Run -f net10.0-android
   ```
   (o F5 desde Visual Studio con el proyecto Mobile como inicio).
4. La app apunta por defecto a `http://10.0.2.2:5070` — el alias del emulador
   para el `localhost` del PC. No hay que configurar nada.

## Ejecutar en un teléfono físico

1. El servidor debe escuchar fuera de localhost:
   ```
   dotnet run --project src/BarrieraMoving.Server --urls http://0.0.0.0:5070
   ```
   Acepta el aviso del Firewall de Windows (redes privadas).
2. Teléfono y PC en la misma red Wi-Fi. Averigua la IP del PC con `ipconfig`
   (ej. `192.168.1.20`).
3. En la pantalla de login de la app, abre **Servidor** y escribe
   `http://192.168.1.20:5070`. Se guarda para las siguientes veces.
4. Activa la depuración USB en el teléfono y despliega con `-t:Run` igual que
   en el emulador (el dispositivo aparece en `adb devices`).

## HTTP vs HTTPS en desarrollo

Android no confía en el certificado de desarrollo de ASP.NET (`dotnet dev-certs`),
así que en desarrollo la app usa **HTTP plano**. Eso está permitido **solo en
builds Debug** (`UsesCleartextTraffic = true` en `MainApplication.cs`, activo
únicamente bajo `#if DEBUG`). En Release el tráfico en claro está prohibido:
producción exige HTTPS con certificado real — nunca desactives esa restricción.

## Dónde viven las credenciales

- Tokens (access + refresh): **SecureStorage** (Android Keystore). Nunca en
  Preferences, archivos o logs.
- La URL del servidor: Preferences (no es un secreto).
- El refresh token rota: cada uso emite uno nuevo y revoca el anterior. El
  logout llama a `POST /api/v1/auth/logout` para revocarlo también en el servidor.
