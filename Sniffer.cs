using Discord;
using HtmlAgilityPack;

namespace Snout
{
    public class HllSniffer {

        public Embed Pull()

        {
            // Créer un tableau pour stocker les URL
            string[] tableauURL = new string[6];

            // Ajouter chaque URL au tableau
            tableauURL[0] = "https://www.battlemetrics.com/servers/hll/17380658"; // La Jungle
            tableauURL[1] = "https://www.battlemetrics.com/servers/hll/10626575"; // HLL France
            tableauURL[2] = "https://www.battlemetrics.com/servers/hll/15169632"; // LpF
            tableauURL[3] = "https://www.battlemetrics.com/servers/hll/13799070"; // CfR
            tableauURL[4] = "https://www.battlemetrics.com/servers/hll/14971018"; // ARES
            tableauURL[5] = "https://www.battlemetrics.com/servers/hll/14245343"; // ARC Team

            string endAnswer = "";

            using (var client = new HttpClient())
            {

                foreach (string extractedUrl in tableauURL)
                {

                    try
                    {
                        var response = client.Send(new HttpRequestMessage(HttpMethod.Head, extractedUrl));
                        if (response.IsSuccessStatusCode)
                        {
                            // Le site est accessible extraire son contenu

                            Console.WriteLine("J'ai testé l'URL " + extractedUrl + " et c'est OK (200)");

                            var url = extractedUrl;
                            var web = new HtmlWeb();
                            var doc = web.Load(url);

                            var title = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/ol/li[3]/a/span");
                            var playerCount = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/div/div/div[1]/div[1]/dl/dd[2]");
                            var status = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/div/div/div[1]/div[1]/dl/dd[4]");

                            if (title != null)
                            {
                                var answer = "";
                                answer = title.InnerText + "_" + playerCount.InnerText + "_" + status.InnerText;
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

            var embed = new EmbedBuilder()
                .WithTitle("🇫🇷 Statut des serveurs FR HLL")
                .WithThumbnailUrl("https://static.wixstatic.com/media/da3421_111b24ae66f64f73aa94efeb80b08f58~mv2.png/v1/fit/w_2500,h_1330,al_c/da3421_111b24ae66f64f73aa94efeb80b08f58~mv2.png")
                .WithColor(new Color(0, 0, 255))
                .WithFooter("Snout v1.0.1 | Source : Battlemetrics")
                .WithTimestamp(DateTimeOffset.UtcNow);

            foreach (var element in listed)
            {
                var trimmedElement = element.Split('_', 3, StringSplitOptions.RemoveEmptyEntries);
                embed.AddField(trimmedElement[0], " Joueurs : " + trimmedElement[1] + " ● Statut : " + trimmedElement[2]);
            }

            var endResult = embed.Build();

            return endResult;
        }

    }
}