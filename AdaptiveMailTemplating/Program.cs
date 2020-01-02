using AdaptiveCards;
using AdaptiveCards.Rendering;
using AdaptiveCards.Rendering.Html;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AdaptiveMailTemplating
{
    class Program
    {
        public static IConfigurationRoot configuration;
        public static async Task Main(string[] args)
        {
            configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
               .AddJsonFile("appsettings.json", false)
               .AddJsonFile("appsettings.local.json", true)
               .Build();

            var templateJson = File.ReadAllText("Template1.json", Encoding.UTF8);
            var data = new
            {
                tenantname = "Stavanger",
                categoryname = "Avløp",
                statusname = "Under prosessering",
                deadline = "2020-02-07T09:20:00Z",
                taskurl = "https://melding.powel.net/stavanger/tasks/some-random-id"
            };

            var dataJson = JsonConvert.SerializeObject(data);
            var transformer = new AdaptiveCards.Templating.AdaptiveTransformer();
            var transformed = transformer.Transform(templateJson, dataJson);

            var hostConfig = new AdaptiveHostConfig
            {
                SupportsInteractivity = true
            };

            var renderer = new AdaptiveCardRenderer(hostConfig);


            // make link html simpler - Outlook is not good at rendering html!
            renderer.ElementRenderers.Remove<AdaptiveOpenUrlAction>();
            renderer.ElementRenderers.Set<AdaptiveOpenUrlAction>((action, context) =>
            {
                var aTag = new HtmlTag("a").AddClass("ac-action-openUrl");
                aTag.Attributes.Add("href", action.Url.ToString());
                aTag.Text = action.Title;
                return aTag;
            });

            var parsedCard = AdaptiveCard.FromJson(transformed);
            parsedCard.Card.Lang = "nb-no"; // set to whatever language makes more sense
            var renderedCard = renderer.RenderCard(parsedCard.Card);

            var htmlContent = renderedCard.Html.ToString();
            var from = new EmailAddress("noreply@powel.com", "Ikke svar");
            var subject = "Ny oppgave fra Powel Melding";
            var to = new EmailAddress("jarle.nygard@powel.no", "Jarle Nygård");
            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);

            var client = new SendGridClient(configuration["SendgridApiKey"]);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
