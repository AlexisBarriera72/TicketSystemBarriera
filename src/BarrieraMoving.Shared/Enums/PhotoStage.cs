namespace BarrieraMoving.Shared.Enums;

// Etapa de una foto de la mudanza. Documentar el ANTES y el DESPUÉS es la mejor
// defensa ante una reclamación por daños: se ve el estado en que se recogió el
// mueble y el estado en que se entregó, con hora y GPS del servidor.
public enum PhotoStage
{
    General = 0,   // foto suelta del chat (comportamiento anterior)
    Pickup = 1,    // Recogida: estado de los artículos ANTES de moverlos
    Delivery = 2,  // Entrega: estado al dejarlos en el destino
}
