using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BarrieraMoving.Server.Services;

// Envío push vía Firebase Cloud Messaging (HTTP v1) con el Admin SDK.
// La credencial es la clave de cuenta de servicio de Alexis, SIEMPRE desde
// user-secrets / variables de entorno — NUNCA en git:
//   Push:ServiceAccountJson  → el JSON entero como cadena (preferido), o
//   Push:ServiceAccountFile  → ruta a un fichero .json en el servidor.
// Si no hay ninguna de las dos, IsConfigured=false y el proceso arranca igual.
public sealed class FirebasePushSender : IPushSender
{
    private readonly ILogger<FirebasePushSender> _log;
    private readonly FirebaseMessaging? _messaging;

    public bool IsConfigured => _messaging is not null;

    public FirebasePushSender(IConfiguration config, ILogger<FirebasePushSender> log)
    {
        _log = log;
        try
        {
            var json = config["Push:ServiceAccountJson"];
            var file = config["Push:ServiceAccountFile"];

            GoogleCredential? credential = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                credential = GoogleCredential.FromJson(json);
            }
            else if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
            {
                credential = GoogleCredential.FromFile(file);
            }

            if (credential is null)
            {
                _log.LogWarning(
                    "Push NotConfigured: falta la credencial de Firebase. Añádela con " +
                    "'dotnet user-secrets set \"Push:ServiceAccountJson\" ...'. Las notificaciones se omiten.");
                return;
            }

            // Reutiliza la app por defecto si ya existe (evita el throw de doble Create)
            var app = FirebaseApp.DefaultInstance
                      ?? FirebaseApp.Create(new AppOptions { Credential = credential });
            _messaging = FirebaseMessaging.GetMessaging(app);
            _log.LogInformation("Push configurado: Firebase Cloud Messaging listo.");
        }
        catch (Exception ex)
        {
            // Una credencial mal formada NO debe tumbar el arranque del servidor
            _log.LogError(ex, "Push: fallo al inicializar Firebase. Las notificaciones quedan desactivadas.");
            _messaging = null;
        }
    }

    public async Task<IReadOnlyCollection<string>> SendAsync(
        IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken ct = default)
    {
        if (_messaging is null || tokens.Count == 0) return [];

        var tokenList = tokens.ToList();
        var dead = new List<string>();

        var multicast = new MulticastMessage
        {
            Tokens = tokenList,
            Notification = new Notification { Title = message.Title, Body = message.Body },
            Data = message.Data?.ToDictionary(kv => kv.Key, kv => kv.Value),
            Android = new AndroidConfig { Priority = Priority.High },
        };

        try
        {
            var response = await _messaging.SendEachForMulticastAsync(multicast, ct);
            for (var i = 0; i < response.Responses.Count; i++)
            {
                var r = response.Responses[i];
                if (r.IsSuccess) continue;

                var code = r.Exception?.MessagingErrorCode;
                // Token muerto: el aparato desinstaló o el token caducó → borrar
                if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                {
                    dead.Add(tokenList[i]);
                }
                else
                {
                    _log.LogWarning(r.Exception, "Push: envío fallido a un token ({Code}).", code);
                }
            }
        }
        catch (Exception ex)
        {
            // Fallo de red/servicio: no propagar; el mensaje ya se guardó, esto es best-effort
            _log.LogError(ex, "Push: fallo al enviar el multicast.");
        }

        return dead;
    }
}
