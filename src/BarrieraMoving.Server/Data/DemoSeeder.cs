using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Enums;
using SkiaSharp;

namespace BarrieraMoving.Server.Data;

// Carga de DEMOSTRACIÓN: deja la aplicación como si llevara meses en marcha, con
// la plantilla completa, mudanzas en todos los estados y un día de trabajo a medias.
//
// BORRA DATOS. Por eso no se ejecuta nunca sola: hay que llamarla explícitamente
// desde el panel de administración, y solo la puede lanzar un Admin. Las tres
// cuentas de desarrollo (admin/conductor/cliente de Seed:*) NO se tocan.
public static class DemoSeeder
{
    public const string Password = "P@ssword123!";

    public static async Task<string> ResetAndSeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var photos = services.GetRequiredService<IPhotoStorage>();

        // Las cuentas que sobreviven al borrado: las de desarrollo configuradas
        var keep = new[] { config["Seed:AdminEmail"], config["Seed:DriverEmail"], config["Seed:ClientEmail"] }
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e!.ToUpperInvariant())
            .ToHashSet();

        // ---------- 1. LIMPIEZA ----------
        // El orden importa: primero lo que apunta a órdenes y usuarios.
        db.Messages.RemoveRange(db.Messages);
        db.TimeEntries.RemoveRange(db.TimeEntries);
        db.SignatureDocuments.RemoveRange(db.SignatureDocuments);
        db.PaperworkDocuments.RemoveRange(db.PaperworkDocuments);
        db.Complaints.RemoveRange(db.Complaints);
        db.QuotePhotos.RemoveRange(db.QuotePhotos);
        db.QuoteRequests.RemoveRange(db.QuoteRequests);
        db.DirectMessages.RemoveRange(db.DirectMessages);
        db.DirectParticipants.RemoveRange(db.DirectParticipants);
        db.DirectConversations.RemoveRange(db.DirectConversations);
        db.DeviceTokens.RemoveRange(db.DeviceTokens);
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        await db.SaveChangesAsync();

        db.Orders.RemoveRange(db.Orders);
        await db.SaveChangesAsync();

        var doomed = await db.Users.Where(u => !keep.Contains(u.NormalizedEmail!)).ToListAsync();
        foreach (var u in doomed) { await users.DeleteAsync(u); }
        logger.LogInformation("Demo: borradas {N} cuentas antiguas.", doomed.Count);

        // ---------- 2. PLANTILLA ----------
        var cats = await db.Categories.ToListAsync();
        int Cat(string name) => cats.FirstOrDefault(c => c.Name == name)?.Id ?? cats[0].Id;

        var drivers = new List<ApplicationUser>();
        foreach (var (name, mail) in new[]
        {
            ("Luis Ortiz", "luis.ortiz@tcl.com"),
            ("Ramón Vega", "ramon.vega@tcl.com"),
            ("Héctor Santiago", "hector.santiago@tcl.com"),
            ("Wilfredo Colón", "willy.colon@tcl.com"),
            ("José Rivera", "jose.rivera@tcl.com"),
            ("Ángel Maldonado", "angel.maldonado@tcl.com"),
            ("Efraín Burgos", "efrain.burgos@tcl.com"),
            ("Norberto Class", "norberto.class@tcl.com"),
            ("Samuel Acevedo", "samuel.acevedo@tcl.com"),
            ("Radamés Otero", "radames.otero@tcl.com"),
            ("Juan Carlos Meléndez", "jc.melendez@tcl.com"),
            ("Freddy Sepúlveda", "freddy.sepulveda@tcl.com"),
        })
        {
            drivers.Add(await MakeAsync(users, name, mail, Roles.Driver));
        }

        var office = new List<ApplicationUser>();
        foreach (var (name, mail) in new[]
        {
            ("Carmen Delgado", "carmen.delgado@tcl.com"),
            ("Yadira Morales", "yadira.morales@tcl.com"),
            ("Brenda Ocasio", "brenda.ocasio@tcl.com"),
            ("Néstor Padilla", "nestor.padilla@tcl.com"),
        })
        {
            office.Add(await MakeAsync(users, name, mail, Roles.Office));
        }

        var clients = new List<ApplicationUser>();
        foreach (var (name, mail) in new[]
        {
            ("Ana Rosario", "ana.rosario@gmail.com"),
            ("Pedro Maldonado", "pedro.maldonado@gmail.com"),
            ("Sofía Cruz", "sofia.cruz@gmail.com"),
            ("Marisol Pagán", "marisol.pagan@gmail.com"),
            ("Edwin Torres", "edwin.torres@gmail.com"),
            ("Gladys Ramos", "gladys.ramos@gmail.com"),
            ("Iván Figueroa", "ivan.figueroa@gmail.com"),
            ("Zoraida Nieves", "zoraida.nieves@gmail.com"),
            ("Rafael Ayala", "rafael.ayala@gmail.com"),
            ("Nydia Quiñones", "nydia.quinones@gmail.com"),
            ("Luis Alberto Rivera", "luisa.rivera@gmail.com"),
            ("Carmen Iris Santos", "carmeniris.santos@gmail.com"),
            ("Héctor Luis Vázquez", "hl.vazquez@gmail.com"),
            ("Awilda Serrano", "awilda.serrano@gmail.com"),
            ("Ramonita Castro", "ramonita.castro@gmail.com"),
            ("Jorge Emilio Díaz", "je.diaz@gmail.com"),
            ("Lourdes Meléndez", "lourdes.melendez@gmail.com"),
            ("Ismael Rodríguez", "ismael.rodriguez@gmail.com"),
            ("Providencia Alicea", "prov.alicea@gmail.com"),
            ("Ángela Marrero", "angela.marrero@gmail.com"),
        })
        {
            clients.Add(await MakeAsync(users, name, mail, Roles.Client));
        }

        var admin = await db.Users.FirstOrDefaultAsync(u => keep.Contains(u.NormalizedEmail!))
                    ?? office[0];

        // ---------- 3. ÓRDENES ----------
        var now = DateTime.UtcNow;
        var orders = new List<Order>();

        Order Add(string title, OrderStatus status, OrderPriority prio, string cat,
                  ApplicationUser client, ApplicationUser? driver,
                  string from, string to, int daysFromNow, string floor, string items,
                  string notes = "")
        {
            var o = new Order
            {
                Title = title,
                Description = notes,
                Status = status,
                Priority = prio,
                CategoryId = Cat(cat),
                AuthorId = client.Id,
                AssignedDriverId = driver?.Id,
                ClientName = client.DisplayName,
                ClientPhone = PhoneFor(client),
                ClientEmail = client.Email,
                OriginZone = from,
                DestinationZone = to,
                ScheduledDate = now.Date.AddDays(daysFromNow),
                Floor = floor,
                Items = items,
                CreatedAt = now.AddDays(daysFromNow - 4).AddHours(-3),
            };
            orders.Add(o);
            return o;
        }

        // --- Sin asignar: lo que la oficina tiene que resolver AHORA ---
        Add("Mudanza residencial — Ana Rosario", OrderStatus.Requested, OrderPriority.Urgent, "Mudanza Local",
            clients[0], null, "Bayamón", "Guaynabo", 0, "3er piso · Sin ascensor",
            Pack("Nevera", "Sofá seccional", "Cama queen con base", "Caja grande" ), "Se muda hoy mismo, el casero le pidió la llave.");
        Add("Mudanza comercial — Oficina en Hato Rey", OrderStatus.Requested, OrderPriority.High, "Larga Distancia",
            clients[1], null, "San Juan", "Caguas", 1, "5to piso o más · Con ascensor",
            Pack("Escritorio ejecutivo", "Vitrina / gabinete grande", "Computadora / monitor", "Cajas de documentos / archivos"));
        Add("Alquiler de camión con chofer — Sofía Cruz", OrderStatus.Requested, OrderPriority.Medium, "Mudanza Local",
            clients[2], null, "Carolina", "Trujillo Alto", 2, "Planta baja / nivel de calle",
            Pack("Juego de comedor completo", "Lavadora", "Secadora"));
        Add("Solo empaque — Marisol Pagán", OrderStatus.Requested, OrderPriority.Low, "Solo Empaque",
            clients[3], null, "Ponce", "Ponce", 4, "2do piso · Sin ascensor",
            Pack("Caja pequeña", "Caja mediana", "Artículos frágiles (cristalería, vajilla)"));

        // --- Asignadas: equipo con trabajo por delante ---
        Add("Mudanza residencial — Edwin Torres", OrderStatus.Assigned, OrderPriority.Medium, "Mudanza Local",
            clients[4], drivers[0], "Río Piedras", "Cupey", 0, "2do piso · Con ascensor",
            Pack("Sofá de 2 plazas", "Colchón matrimonial (full/queen)", "Televisor grande (43\" a 65\")", "Microondas"));
        Add("Mudanza a almacén — Gladys Ramos", OrderStatus.Assigned, OrderPriority.Medium, "Almacenamiento",
            clients[5], drivers[1], "Dorado", "Toa Baja", 1, "Planta baja / nivel de calle",
            Pack("Ropero / armario grande", "Cómoda / gavetero", "Bicicleta"));
        Add("Traslado urgente de equipo médico", OrderStatus.Assigned, OrderPriority.Urgent, "Larga Distancia",
            clients[6], drivers[3], "Mayagüez", "San Juan", 0, "Planta baja / nivel de calle",
            Pack("Equipo médico", "Generador eléctrico"), "Cliente corporativo. No puede esperar a mañana.");

        // --- En camino: MEDIODÍA, la calle en marcha ---
        var enroute1 = Add("Mudanza residencial — Zoraida Nieves", OrderStatus.EnRoute, OrderPriority.Medium, "Mudanza Local",
            clients[7], drivers[0], "Guaynabo", "Bayamón", 0, "3er piso · Con ascensor",
            Pack("Nevera", "Estufa", "Juego de 4 sillas", "Mesa de comedor"));
        var enroute2 = Add("Entrega de mercancía — Rafael Ayala", OrderStatus.EnRoute, OrderPriority.High, "Larga Distancia",
            clients[8], drivers[2], "Arecibo", "Manatí", 0, "Planta baja / nivel de calle",
            Pack("Mercancía / inventario comercial", "Materiales de construcción"));

        // --- En curso: cargando ahora mismo ---
        var inprog1 = Add("Mudanza residencial — Nydia Quiñones", OrderStatus.InProgress, OrderPriority.Medium, "Mudanza Local",
            clients[9], drivers[1], "Cataño", "Toa Baja", 0, "4to piso · Sin ascensor",
            Pack("Sofá seccional", "Cama king con base", "Ropero / armario grande", "Trotadora / caminadora"));
        var inprog2 = Add("Mudanza comercial — Local en Santurce", OrderStatus.InProgress, OrderPriority.High, "Mudanza Local",
            clients[0], drivers[4], "Santurce", "Condado", 0, "2do piso · Sin ascensor",
            Pack("Vitrina / gabinete grande", "Escritorio", "Librero / estantería", "Caja fuerte"));

        // --- Pendiente de firma: el cuello de botella de la oficina ---
        var pend1 = Add("Mudanza residencial — Pedro Maldonado", OrderStatus.PendingSignature, OrderPriority.Medium, "Mudanza Local",
            clients[1], drivers[3], "Caguas", "Cayey", 0, "Planta baja / nivel de calle",
            Pack("Juego de sala completo", "Televisor grande (43\" a 65\")", "Mesa de centro"));
        var pend2 = Add("Mudanza larga distancia — Iván Figueroa", OrderStatus.PendingSignature, OrderPriority.Medium, "Larga Distancia",
            clients[6], drivers[5], "Aguadilla", "Bayamón", -1, "3er piso · Sin ascensor",
            Pack("Piano vertical", "Juego de comedor completo", "Espejo pequeño"));

        // --- Completadas: historial que da sensación de recorrido ---
        Add("Mudanza residencial — Ana Rosario", OrderStatus.Completed, OrderPriority.Medium, "Mudanza Local",
            clients[0], drivers[0], "Vega Baja", "Manatí", -2, "2do piso · Con ascensor",
            Pack("Cómoda / gavetero", "Colchón individual (twin)", "Lámpara de mesa"));
        Add("Entrega y recogido — Sofía Cruz", OrderStatus.Completed, OrderPriority.Low, "Mudanza Local",
            clients[2], drivers[1], "Humacao", "Fajardo", -3, "Planta baja / nivel de calle",
            Pack("Juego de patio (mesa y sillas)", "Plantas"));
        Add("Mudanza comercial — Almacén Bayamón", OrderStatus.Completed, OrderPriority.High, "Almacenamiento",
            clients[3], drivers[2], "Bayamón", "Toa Baja", -5, "Planta baja / nivel de calle",
            Pack("Mercancía / inventario comercial", "Estantería / librero"));
        Add("Mudanza residencial — Edwin Torres", OrderStatus.Completed, OrderPriority.Medium, "Mudanza Local",
            clients[4], drivers[3], "Carolina", "Canóvanas", -7, "3er piso · Sin ascensor",
            Pack("Nevera", "Lavadora", "Secadora", "Cama queen con base"));
        Add("Solo empaque — Gladys Ramos", OrderStatus.Completed, OrderPriority.Low, "Solo Empaque",
            clients[5], drivers[4], "Ponce", "Ponce", -9, "2do piso · Sin ascensor",
            Pack("Caja pequeña", "Caja mediana", "Artículos frágiles (cristalería, vajilla)"));
        Add("Mudanza larga distancia — Rafael Ayala", OrderStatus.Completed, OrderPriority.Urgent, "Larga Distancia",
            clients[8], drivers[5], "San Juan", "Mayagüez", -11, "Varios niveles (casa de dos plantas)",
            Pack("Juego de sala completo", "Cama king con base", "Nevera", "Congelador (freezer)"));
        Add("Mudanza residencial — Zoraida Nieves", OrderStatus.Completed, OrderPriority.Medium, "Mudanza Local",
            clients[7], drivers[0], "Trujillo Alto", "Caguas", -14, "Planta baja / nivel de calle",
            Pack("Sofá de 2 plazas", "Mesa de noche", "Televisor pequeño (hasta 32\")"));
        Add("Alquiler de camión — Nydia Quiñones", OrderStatus.Completed, OrderPriority.Medium, "Mudanza Local",
            clients[9], drivers[1], "Guaynabo", "San Juan", -18, "Planta baja / nivel de calle",
            Pack("Bicicleta", "Coche de bebé", "Caja mediana"));

        // --- Relleno: el volumen que hace que la operación parezca real ---
        // Las de arriba son escenarios escritos a mano (chat, fotos, firmas). Estas
        // dan cuerpo: rutas, pisos y cargas distintas repartidas por toda la isla.
        var routes = new[]
        {
            ("San Juan", "Bayamón"), ("Carolina", "Loíza"), ("Ponce", "Juana Díaz"),
            ("Caguas", "Gurabo"), ("Arecibo", "Hatillo"), ("Mayagüez", "Cabo Rojo"),
            ("Guaynabo", "San Juan"), ("Toa Alta", "Dorado"), ("Humacao", "Naguabo"),
            ("Bayamón", "Toa Baja"), ("Trujillo Alto", "Carolina"), ("Cayey", "Aibonito"),
            ("Vega Baja", "Manatí"), ("Fajardo", "Ceiba"), ("Aguadilla", "Isabela"),
            ("Santurce", "Río Piedras"), ("Cataño", "Bayamón"), ("San Germán", "Mayagüez"),
            ("Yauco", "Guayanilla"), ("Barceloneta", "Arecibo"), ("Guayama", "Salinas"),
            ("Coamo", "Santa Isabel"), ("Camuy", "Quebradillas"), ("Cidra", "Aguas Buenas"),
        };
        var loads = new[]
        {
            Pack("Nevera", "Estufa", "Lavadora", "Caja mediana"),
            Pack("Sofá seccional", "Televisor grande (43\" a 65\")", "Mesa de centro"),
            Pack("Cama queen con base", "Cómoda / gavetero", "Mesa de noche"),
            Pack("Juego de comedor completo", "Vitrina / gabinete grande"),
            Pack("Caja pequeña", "Caja mediana", "Maleta", "Bolsas de ropa"),
            Pack("Escritorio", "Librero / estantería", "Computadora / monitor"),
            Pack("Ropero / armario grande", "Colchón matrimonial (full/queen)"),
            Pack("Juego de patio (mesa y sillas)", "Plantas", "Bicicleta"),
            Pack("Mercancía / inventario comercial", "Materiales de construcción"),
            Pack("Piano vertical", "Obras de arte", "Artículos frágiles (cristalería, vajilla)"),
            Pack("Trotadora / caminadora", "Equipo de gimnasio"),
            Pack("Cuna", "Coche de bebé", "Juego de 4 sillas"),
        };
        var titles = new[]
        {
            "Mudanza residencial", "Mudanza comercial", "Alquiler de camión con chofer",
            "Entrega y recogido", "Transportación de mercancía", "Mudanza a almacén",
        };
        var catNames = new[] { "Mudanza Local", "Larga Distancia", "Solo Empaque", "Almacenamiento" };
        var rnd = new Random(20260728); // semilla fija: la demo sale igual cada vez

        for (var i = 0; i < 24; i++)
        {
            var client = clients[rnd.Next(clients.Count)];
            var (from, to) = routes[i % routes.Length];

            // Reparto de estados: la mayoría cerradas (historial) y un puñado vivas
            var status = i switch
            {
                < 3 => OrderStatus.Requested,
                < 7 => OrderStatus.Assigned,
                < 9 => OrderStatus.EnRoute,
                < 11 => OrderStatus.InProgress,
                < 13 => OrderStatus.PendingSignature,
                _ => OrderStatus.Completed,
            };
            var driver = status == OrderStatus.Requested ? null : drivers[rnd.Next(drivers.Count)];
            var prio = rnd.Next(10) switch
            {
                0 => OrderPriority.Urgent,
                < 3 => OrderPriority.High,
                < 8 => OrderPriority.Medium,
                _ => OrderPriority.Low,
            };
            var day = status == OrderStatus.Completed ? -rnd.Next(1, 40)
                    : status == OrderStatus.Requested ? rnd.Next(0, 9)
                    : rnd.Next(-1, 3);

            Add($"{titles[i % titles.Length]} — {client.DisplayName}", status, prio,
                catNames[i % catNames.Length], client, driver, from, to, day,
                MovingItems.Floors[rnd.Next(MovingItems.Floors.Count)] + " · " +
                MovingItems.Elevators[rnd.Next(2)],
                loads[i % loads.Length]);
        }

        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();

        // ---------- 4. CHAT, FOTOS Y EVIDENCIA ----------
        var jpeg = MakePlaceholderJpeg();
        var thumb = MakePlaceholderJpeg(200);

        async Task<Message> PhotoAsync(Order o, ApplicationUser who, string role, string text,
                                       PhotoStage stage, int minutesAgo, double? lat = null, double? lng = null)
        {
            var name = Guid.NewGuid().ToString("N");
            return new Message
            {
                OrderId = o.Id,
                Content = text,
                UserId = who.Id,
                SenderRole = role,
                Stage = stage,
                CreatedAt = now.AddMinutes(-minutesAgo),
                AttachmentPath = await photos.SaveAsync(o.Id, $"{name}.jpg", jpeg),
                AttachmentThumbPath = await photos.SaveAsync(o.Id, $"{name}_thumb.jpg", thumb),
                Latitude = lat,
                Longitude = lng,
            };
        }

        Message Msg(Order o, ApplicationUser who, string role, string text, int minutesAgo, bool system = false) => new()
        {
            OrderId = o.Id,
            Content = text,
            UserId = who.Id,
            SenderRole = role,
            IsSystem = system,
            CreatedAt = now.AddMinutes(-minutesAgo),
        };

        var msgs = new List<Message>
        {
            Msg(enroute1, office[0], Roles.Office, "[EVENTO] El estado cambió de Assigned a EnRoute.", 95, true),
            Msg(enroute1, drivers[0], Roles.Driver, "Saliendo del almacén, llego en 25 minutos.", 92),
            Msg(enroute1, clients[7], Roles.Client, "Perfecto, aquí los espero. El portón está abierto.", 88),
            Msg(enroute1, drivers[0], Roles.Driver, "Hay tapón en la 22, vamos con unos 10 min de retraso.", 34),

            Msg(enroute2, drivers[2], Roles.Driver, "Cargado y en ruta hacia Manatí.", 61),
            Msg(enroute2, office[1], Roles.Office, "Recibido. Avísame al llegar para cerrar el albarán.", 55),

            Msg(inprog1, office[0], Roles.Office, "[EVENTO] El estado cambió de EnRoute a InProgress.", 140, true),
            Msg(inprog1, drivers[1], Roles.Driver, "Empezando a bajar. El cuarto piso sin ascensor va a tomar su tiempo.", 138),
            Msg(inprog1, clients[9], Roles.Client, "Gracias por el aviso. ¿Necesitan ayuda con algo?", 120),
            Msg(inprog1, drivers[1], Roles.Driver, "No hace falta, vamos bien. Como a las 3 terminamos.", 115),

            Msg(inprog2, drivers[4], Roles.Driver, "La caja fuerte pesa más de lo previsto, mando refuerzo.", 47),
            Msg(inprog2, office[0], Roles.Office, "Le digo a Ángel que pase cuando termine en Cayey.", 41),

            Msg(pend1, drivers[3], Roles.Driver, "Entrega terminada, todo en su sitio.", 200),
            Msg(pend1, office[0], Roles.Office, "[EVENTO] El estado cambió de InProgress a PendingSignature.", 195, true),
            Msg(pend2, drivers[5], Roles.Driver, "El piano llegó sin un rasguño. Firma pendiente del cliente.", 1400),
        };

        msgs.Add(await PhotoAsync(inprog1, drivers[1], Roles.Driver, "📷 Foto — Recogida", PhotoStage.Pickup, 132, 18.4441, -66.1572));
        msgs.Add(await PhotoAsync(inprog1, drivers[1], Roles.Driver, "El sofá ya venía con esta rotura en el brazo derecho.", PhotoStage.PreExistingDamage, 130, 18.4441, -66.1572));
        msgs.Add(await PhotoAsync(inprog2, drivers[4], Roles.Driver, "📷 Foto — Recogida", PhotoStage.Pickup, 50, 18.4505, -66.0619));
        msgs.Add(await PhotoAsync(pend1, drivers[3], Roles.Driver, "📷 Foto — Recogida", PhotoStage.Pickup, 260));
        msgs.Add(await PhotoAsync(pend1, drivers[3], Roles.Driver, "📷 Foto — Entrega", PhotoStage.Delivery, 205));
        msgs.Add(await PhotoAsync(pend2, drivers[5], Roles.Driver, "La vitrina llegó con el cristal ya astillado de origen.", PhotoStage.PreExistingDamage, 1500));

        // Conversación de relleno en el resto de órdenes: una operación real no tiene
        // veinte mudanzas con el chat vacío. Se reparte para que unas tengan hilo y
        // otras no, que tampoco todas hablan.
        var driverLines = new[]
        {
            "Voy saliendo, llego en unos 20 minutos.",
            "Ya cargamos todo, salimos para allá.",
            "Llegamos. ¿Bajo por el estacionamiento o por el frente?",
            "Terminamos aquí, todo entregado sin novedad.",
            "El ascensor está fuera de servicio, vamos a tardar un poco más.",
            "¿Confirmo que la entrega es en el segundo piso?",
        };
        var clientLines = new[]
        {
            "Perfecto, gracias por avisar.",
            "Aquí los espero, el portón queda abierto.",
            "¿Me pueden llamar cuando vengan de camino?",
            "Todo llegó bien, muchas gracias por el cuidado.",
            "Por el frente está mejor, hay más espacio.",
            "Sí, segundo piso. Apartamento 2-B.",
        };
        var officeLines = new[]
        {
            "Orden confirmada, el equipo va asignado.",
            "Cualquier cosa nos avisas por aquí.",
            "Recibido, lo anoto en el expediente.",
        };

        foreach (var o in orders.Where(o => o.AssignedDriverId is not null))
        {
            if (rnd.Next(3) == 0) continue;  // una de cada tres se queda sin hilo

            var drv = drivers.First(d => d.Id == o.AssignedDriverId);
            var cli = clients.FirstOrDefault(c => c.Id == o.AuthorId) ?? clients[0];
            var baseMin = rnd.Next(60, 3000);

            msgs.Add(Msg(o, drv, Roles.Driver, driverLines[rnd.Next(driverLines.Length)], baseMin));
            if (rnd.Next(4) > 0)
            {
                msgs.Add(Msg(o, cli, Roles.Client, clientLines[rnd.Next(clientLines.Length)], baseMin - rnd.Next(5, 25)));
            }
            if (rnd.Next(3) == 0)
            {
                msgs.Add(Msg(o, office[rnd.Next(office.Count)], Roles.Office,
                             officeLines[rnd.Next(officeLines.Length)], baseMin - rnd.Next(26, 50)));
            }
            if (o.Status == OrderStatus.Completed && rnd.Next(2) == 0)
            {
                msgs.Add(Msg(o, drv, Roles.Driver, "📷 Foto — Entrega", baseMin - 55));
            }
        }

        db.Messages.AddRange(msgs);

        // ---------- 5. FICHAJES: media jornada en marcha ----------
        var times = new List<TimeEntry>();
        // Ocho conductores fichados AHORA MISMO (turno abierto): media plantilla en la calle
        for (var i = 0; i < 8; i++)
        {
            times.Add(new TimeEntry
            {
                UserId = drivers[i].Id,
                ClockInUtc = now.AddHours(-4).AddMinutes(-i * 17),
                ClockInLatitude = 18.4 + i * 0.01,
                ClockInLongitude = -66.1 - i * 0.01,
            });
        }
        // Historial cerrado de las dos últimas semanas
        for (var d = 1; d <= 12; d++)
        {
            foreach (var drv in drivers.Take(10))
            {
                // Algún día libre suelto, que nadie trabaja 12 días seguidos
                if (rnd.Next(8) == 0) continue;
                var start = now.Date.AddDays(-d).AddHours(12);
                var entry = new TimeEntry
                {
                    UserId = drv.Id,
                    ClockInUtc = start,
                    ClockOutUtc = start.AddHours(8).AddMinutes(rnd.Next(-40, 75)),
                };
                // Un olvido de salida real: lo cerró el sistema al llegar al máximo
                if (d == 5 && drv == drivers[2])
                {
                    entry.ClockOutUtc = start.AddHours(14);
                    entry.AutoClosed = true;
                }
                times.Add(entry);
            }
        }
        db.TimeEntries.AddRange(times);

        // ---------- 6. FIRMAS Y PAPELEO ----------
        db.SignatureDocuments.AddRange(
            new SignatureDocument
            {
                OrderId = pend1.Id,
                RequestedByUserId = drivers[3].Id,
                SignerName = clients[1].DisplayName,
                SignerEmail = clients[1].Email,
                Status = SignatureDocStatus.Signed,   // esperando visto bueno de oficina
                SignedAtUtc = now.AddMinutes(-190),
                CreatedAtUtc = now.AddMinutes(-200),
                IsProvisional = true,
                EmailStatus = EmailDeliveryStatus.NotConfigured,
            },
            new SignatureDocument
            {
                OrderId = pend2.Id,
                RequestedByUserId = drivers[5].Id,
                SignerName = clients[6].DisplayName,
                SignerEmail = clients[6].Email,
                Status = SignatureDocStatus.Rejected,  // la oficina la devolvió
                SignedAtUtc = now.AddDays(-1).AddHours(-2),
                CreatedAtUtc = now.AddDays(-1).AddHours(-3),
                ReviewedByUserId = office[0].Id,
                ReviewedAtUtc = now.AddDays(-1),
                RejectReason = "La firma salió cortada y no se lee el nombre. Repetirla con el cliente presente.",
                EmailStatus = EmailDeliveryStatus.NotConfigured,
            });

        var slots = services.GetRequiredService<IPaperworkService>().GetSlots();
        var pw = new List<PaperworkDocument>();
        foreach (var (slot, i) in slots.Select((s, i) => (s, i)))
        {
            // pend1 lleva casi todo el papeleo; uno rechazado para ver el flujo real
            pw.Add(new PaperworkDocument
            {
                OrderId = pend1.Id,
                SlotKey = slot.Key,
                UploadedByUserId = drivers[3].Id,
                FilePath = $"demo/pw-{pend1.Id}-{slot.Key}.jpg",
                ThumbPath = $"demo/pw-{pend1.Id}-{slot.Key}_thumb.jpg",
                // Hash de integridad: en la demo es inventado, pero la columna es
                // obligatoria a propósito — un papel legal sin huella no debe existir.
                ContentHash = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes($"demo-{pend1.Id}-{slot.Key}"))),
                Status = i == 2 ? PaperworkStatus.Rejected : PaperworkStatus.Attached,
                RejectReason = i == 2 ? "La foto está movida, no se lee la firma del cliente." : null,
                CreatedAtUtc = now.AddMinutes(-210 + i * 5),
            });
        }
        db.PaperworkDocuments.AddRange(pw);

        // ---------- 7. RECLAMACIONES ----------
        db.Complaints.AddRange(
            new Complaint
            {
                ClientUserId = clients[4].Id,
                OrderId = orders.First(o => o.Status == OrderStatus.Completed).Id,
                Subject = "Rasguño en la puerta del ropero",
                Description = "Al llegar noté un rasguño largo en la puerta del ropero que antes no estaba.",
                Status = ComplaintStatus.Open,
                CreatedAtUtc = now.AddHours(-6),
            },
            new Complaint
            {
                ClientUserId = clients[2].Id,
                Subject = "Llegaron más tarde de lo acordado",
                Description = "Quedamos a las 9:00 y llegaron pasadas las 11:00 sin avisar.",
                Status = ComplaintStatus.InReview,
                CreatedAtUtc = now.AddDays(-2),
            },
            new Complaint
            {
                ClientUserId = clients[8].Id,
                Subject = "Faltaba una caja al terminar",
                Description = "Contamos 23 cajas al salir y 22 al llegar.",
                Status = ComplaintStatus.Resolved,
                CreatedAtUtc = now.AddDays(-9),
                OfficeResponse = "La caja apareció en el almacén y se entregó al día siguiente. Disculpas por el susto.",
                RespondedByUserId = office[1].Id,
                RespondedAtUtc = now.AddDays(-8),
            },
            new Complaint
            {
                ClientUserId = clients[11].Id,
                Subject = "Cobro de urgencia que no pedí",
                Description = "En la factura aparece el cargo de $40.00 por urgencia y yo no marqué esa opción.",
                Status = ComplaintStatus.InReview,
                CreatedAtUtc = now.AddHours(-30),
            },
            new Complaint
            {
                ClientUserId = clients[14].Id,
                Subject = "El equipo fue muy atento",
                Description = "No es una queja: quiero felicitar a Luis y su compañero, trataron los muebles con mucho cuidado.",
                Status = ComplaintStatus.Resolved,
                CreatedAtUtc = now.AddDays(-4),
                OfficeResponse = "¡Gracias por tomarse el tiempo de escribirnos! Se lo hicimos llegar al equipo.",
                RespondedByUserId = office[2].Id,
                RespondedAtUtc = now.AddDays(-3),
            },
            new Complaint
            {
                ClientUserId = clients[17].Id,
                Subject = "Falta una pata de la mesa de comedor",
                Description = "Desmontaron la mesa y al armarla falta un tornillo y una pata quedó floja.",
                Status = ComplaintStatus.Open,
                CreatedAtUtc = now.AddHours(-11),
            });

        // ---------- 8. COTIZACIONES DEL SITIO PÚBLICO ----------
        var guest = services.GetRequiredService<IGuestLookupService>();
        var quotes = new List<QuoteRequest>();
        var leads = new[]
        {
            ("Milagros Sanabria", "787-555-0142", "Mudanza residencial", "Cataño", "Bayamón", "2do piso · Con ascensor", false),
            ("Orlando Benítez", "939-555-0177", "Mudanza comercial", "Hato Rey", "Guaynabo", "5to piso o más · Con ascensor", false),
            ("Damaris Cordero", "787-555-0193", "Alquiler de camión con chofer", "Caguas", "Cidra", "Planta baja / nivel de calle", false),
            ("Héctor Lugo", "787-555-0128", "Transportación de mercancía", "Ponce", "Juana Díaz", "Planta baja / nivel de calle", true),
            ("Aida Berríos", "939-555-0165", "Entrega y recogido", "Arecibo", "Vega Alta", "3er piso · Sin ascensor", true),
            ("Nelson Irizarry", "787-555-0210", "Mudanza residencial", "Mayagüez", "Añasco", "Planta baja / nivel de calle", false),
            ("Wanda Colón", "939-555-0233", "Mudanza residencial", "Toa Baja", "Dorado", "2do piso · Sin ascensor", false),
            ("Julio Santana", "787-555-0248", "Alquiler de camión con chofer", "Humacao", "Yabucoa", "Planta baja / nivel de calle", false),
            ("Elba Rodríguez", "787-555-0256", "Solo empaque", "San Juan", "San Juan", "4to piso · Con ascensor", true),
            ("Ricardo Feliciano", "939-555-0271", "Transportación de mercancía", "Guayama", "Salinas", "Planta baja / nivel de calle", false),
            ("Maritza Ortega", "787-555-0289", "Mudanza comercial", "Bayamón", "Cataño", "Varios niveles (casa de dos plantas)", false),
        };
        foreach (var (name, phone, svc, from, to, floor, handled) in leads)
        {
            quotes.Add(new QuoteRequest
            {
                ReferenceCode = await guest.GenerateReferenceCodeAsync(),
                Name = name,
                Phone = phone,
                Email = name.Split(' ')[0].ToLowerInvariant() + "@gmail.com",
                ServiceType = svc,
                OriginZone = from,
                DestinationZone = to,
                Floor = floor,
                Items = Pack("Nevera", "Sofá de 2 plazas", "Caja mediana"),
                PreferredDate = now.AddDays(Random.Shared.Next(2, 12)).ToString("dd/MM/yyyy"),
                Details = "Solicitud enviada desde el sitio web.",
                Handled = handled,
                CreatedAtUtc = now.AddHours(-Random.Shared.Next(2, 70)),
            });
        }
        db.QuoteRequests.AddRange(quotes);

        await db.SaveChangesAsync();

        var summary = $"{drivers.Count} conductores, {office.Count} de oficina, {clients.Count} clientes, " +
                      $"{orders.Count} órdenes, {msgs.Count} mensajes, {times.Count} fichajes, " +
                      $"{quotes.Count} cotizaciones.";
        logger.LogInformation("Demo sembrada: {Summary}", summary);
        return summary;
    }

    private static string Pack(params string[] items) => MovingItems.Pack(items);

    private static string PhoneFor(ApplicationUser u) =>
        $"787-555-{Math.Abs(u.Id.GetHashCode()) % 9000 + 1000}";

    private static async Task<ApplicationUser> MakeAsync(
        UserManager<ApplicationUser> users, string displayName, string email, string role)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var u = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
        };
        var res = await users.CreateAsync(u, Password);
        if (!res.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo crear {email}: {string.Join(", ", res.Errors.Select(e => e.Description))}");
        }
        await users.AddToRoleAsync(u, role);
        return u;
    }

    // Imagen de relleno para que las miniaturas del chat no salgan rotas.
    // Es un degradado con la marca; no pretende parecer una foto real.
    private static byte[] MakePlaceholderJpeg(int size = 800)
    {
        using var bmp = new SKBitmap(size, (int)(size * 0.75));
        using (var canvas = new SKCanvas(bmp))
        {
            using var paint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(bmp.Width, bmp.Height),
                    [new SKColor(0x01, 0x1A, 0x43), new SKColor(0xFA, 0x54, 0x00)],
                    null, SKShaderTileMode.Clamp),
            };
            canvas.DrawRect(0, 0, bmp.Width, bmp.Height, paint);
        }
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, 70);
        return data.ToArray();
    }
}
