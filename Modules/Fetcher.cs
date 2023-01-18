using Discord;
using HtmlAgilityPack;

namespace Snout.Modules;

public class HllSniffer
{

    public Embed Pull(List<string> listUrl)

    {
        string endAnswer = "";

        using (var client = new HttpClient())
        {

            foreach (string extractedUrl in listUrl)
            {

                try
                {
                    HttpResponseMessage? response = client.Send(new(HttpMethod.Head, extractedUrl));
                    if (response.IsSuccessStatusCode)
                    {
                        // Le site est accessible extraire son contenu

                        Console.WriteLine("J'ai testé l'URL " + extractedUrl + " et c'est OK (200)");

                        var url = extractedUrl;
                        var web = new HtmlWeb();
                        HtmlDocument? doc = web.Load(url);

                        HtmlNode? title = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/ol/li[3]/a/span");
                        HtmlNode? playerCount = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/div/div/div[1]/div[1]/dl/dd[2]");
                        HtmlNode? status = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/div/div/div[1]/div[1]/dl/dd[4]");
                        HtmlNode? ipPort = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/div/div/div[1]/div[1]/dl/dd[3]/span[2]");

                        if (title != null)
                        {
                            var answer = title.InnerText + "_" + playerCount.InnerText + "_" + status.InnerText + "_" + ipPort.InnerText;

                            if (string.IsNullOrEmpty(answer))
                            {
                                // Le site est accessible mais il n'y a pas de contenu (anti-ddos actif ?)
                                EmbedBuilder emptyAnswerEmbed = new();
                                emptyAnswerEmbed.WithTitle("🇫🇷 Hell Let Loose - Serveurs de la communauté");
                                emptyAnswerEmbed.WithDescription(":x: Protections DDOS actives. Les résultats peuvent être *incomplets* ou *indisponibles*.");
                                emptyAnswerEmbed.WithThumbnailUrl("https://static.wixstatic.com/media/da3421_111b24ae66f64f73aa94efeb80b08f58~mv2.png/v1/fit/w_2500,h_1330,al_c/da3421_111b24ae66f64f73aa94efeb80b08f58~mv2.png");
                                emptyAnswerEmbed.WithColor(new(0, 0, 255));
                                emptyAnswerEmbed.WithFooter(Program.GlobalElements.GlobalSnoutVersion + " | Source : Battlemetrics.com");
                                emptyAnswerEmbed.WithTimestamp(DateTimeOffset.UtcNow);

                                return emptyAnswerEmbed.Build();

                            }

                            endAnswer += " ~ " + answer;
                        }

                    }
                    else
                    {
                        Console.WriteLine("Le site n'est pas accessible");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Une erreur s'est produite : " + ex.Message);
                }

                Thread.Sleep(5000); // 5 secondes entre chaque HTTP request + extract de DOM

            }

        }

        var splitted = endAnswer.Split('~');
        var listed = splitted.ToList();
        listed.RemoveAt(0);

        EmbedBuilder? embed = new EmbedBuilder()
            .WithTitle("🇫🇷 Hell Let Loose - Serveurs de la communauté")
            .WithDescription("---")
            .WithThumbnailUrl("https://static.wixstatic.com/media/da3421_111b24ae66f64f73aa94efeb80b08f58~mv2.png/v1/fit/w_2500,h_1330,al_c/da3421_111b24ae66f64f73aa94efeb80b08f58~mv2.png")
            .WithColor(new(0, 0, 255))
            .WithFooter(Program.GlobalElements.GlobalSnoutVersion + " | Source : Battlemetrics")
            .WithTimestamp(DateTimeOffset.UtcNow);

        var sortedFields = listed
            .Select(element =>
            {
                var trimmedElement = element.Split('_', 4, StringSplitOptions.RemoveEmptyEntries);
                string pastille = trimmedElement[2] == "online" ? ":white_check_mark:" : ":x:";
                var joueurs = trimmedElement[1].Split('/');
                var nbJoueurs = int.Parse(joueurs[0]);
                var nbTotalJoueurs = int.Parse(joueurs[1]);

                return new
                {
                    Name = trimmedElement[0],
                    Value = $"{pastille} | Joueurs : {nbJoueurs}/{nbTotalJoueurs} ● steam://connect/{trimmedElement[3]}",
                    NbJoueurs = nbJoueurs
                };
            })
            .OrderByDescending(field => field.NbJoueurs)
            .ToList();

        foreach (var field in sortedFields)
        {
            embed.AddField(field.Name, field.Value);
        }

        Embed? endResult = embed.Build();

        return endResult;
    }

}