using Discord;

namespace Snout;

// Cette classe permet l'utilisation des notifications personnalisées sous un format plus esthéthique. Elle retourne un embed.

public class CustomNotification
{
    private readonly NotificationType _type;
    private readonly string _message;
    private readonly string _title;

    public CustomNotification(NotificationType type, string title, string message) // Constructeur de la notification
    {
        _type = type;
        _message = message;
        _title = title;
    }

    public Embed BuildEmbed()
    {

        Color color;
        string imageUrl;
        switch (_type)
        {
            case NotificationType.Error:
                color = new Color(255, 0, 0); // rouge
                imageUrl = "https://cdn-icons-png.flaticon.com/512/189/189678.png"; // image : croix rouge
                break;
            case NotificationType.Info:
                color = new Color(0, 0, 255); // bleu
                imageUrl = "https://cdn-icons-png.flaticon.com/512/5828/5828450.png"; // image : point d'exclamation
                break;
            case NotificationType.Success:
                color = new Color(0, 255, 0); // vert
                imageUrl = "https://cdn-icons-png.flaticon.com/512/1709/1709977.png"; // image : coche verte
                break; 
            default:
                color = new Color(0, 0, 0); // noir
                imageUrl = ""; // image vide
                break;
        }

        var embed = new EmbedBuilder()
            .WithTitle(_title)
            .WithAuthor("Snout", imageUrl)
            .WithColor(color)
            .WithDescription(_message)
            .Build();

        return embed;
    }
}
