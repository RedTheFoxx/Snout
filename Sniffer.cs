using Discord;
using HtmlAgilityPack;

namespace Snout
{
    public class HllSniffer {

        public Embed Pull(List<string> listUrl)

        {
            string endAnswer = "";

            using (var client = new HttpClient())
            {

                foreach (string extractedUrl in listUrl)
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
                            var ipPort = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/div/div/div[1]/div[1]/dl/dd[3]/span[1]");

                            if (title != null)
                            {
                                var answer = "";
                                answer = title.InnerText + "_" + playerCount.InnerText + "_" + status.InnerText + "_" + ipPort.InnerText;
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

            int serverCounter = 1;

            foreach (var element in listed)
            {
                var trimmedElement = element.Split('_', 4, StringSplitOptions.RemoveEmptyEntries);
                embed.AddField(serverCounter + ". " + trimmedElement[0], " Joueurs : " + trimmedElement[1] + " ● Statut : " + trimmedElement[2] + " ● IP : " + trimmedElement[3]);
                serverCounter++;    
            }

            var endResult = embed.Build();
            return endResult;
        }

    }
}
