using Discord;

namespace Snout
{
    // Cette classe permet l'utilisation des notifications personnalisées sous un format plus esthéthique. Elle retoure un embed.

    public enum NotificationType
    {
        Error,
        Info,
        Success
    }

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
                    imageUrl = ""; // image : croix rouge
                    break;
                case NotificationType.Info:
                    color = new Color(0, 0, 255); // bleu
                    imageUrl = ""; // image : point d'exclamation
                    break;
                case NotificationType.Success:
                    color = new Color(0, 255, 0); // vert
                    imageUrl = ""; // image : coche verte
                    break; 
                default:
                    color = new Color(0, 0, 0); // noir
                    imageUrl = ""; // image vide
                    break;
            }

            var embed = new EmbedBuilder()
                .WithTitle(_title)
                .WithColor(color)
                .WithDescription(_message)
                .WithThumbnailUrl(imageUrl)
                .Build();

            return embed;
        }
    }

}
