using funda.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace funda
{
    class Program
    {

        private static string _urlBase;
        private static string _query;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile($"appsettings.json");
            var config = builder.Build();

           

            for (int i = 1; i <= 2; i++)
            {
                if (i == 1)
                    _query = "/amsterdam/";
                else
                    _query = "/amsterdam/tuin/";

                _urlBase = $"{config.GetSection("API_Access:UrlBase").Value}{config.GetSection("API_Access:Format").Value}{config.GetSection("API_Access:AccessKey").Value}/?type={config.GetSection("API_Access:Type").Value}&zo={_query}";

                var objectsForSale = GetAllObjectsAsync().GetAwaiter().GetResult();

                if(objectsForSale != null)
                    HandleResult(objectsForSale); Thread.Sleep(60000);
            }

            Console.ReadLine();
        }

        private static async Task<IEnumerable<ObjectForSale>> GetAllObjectsAsync()
        {
            try
            {
                var objectsForSale = new List<ObjectForSale>();
                HttpClient httpClient = new HttpClient();
                int currentPage = 1;
                int totalPages = 0;
                var nextUrl = _urlBase;

                do
                {
                    var response = await Policy
                            .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                            .WaitAndRetryAsync(new[]
                            {
                                TimeSpan.FromSeconds(60),
                                TimeSpan.FromSeconds(3),
                                TimeSpan.FromSeconds(3)
                            }, (result, timeSpan, retryCount, context) => {
                                Console.WriteLine($"Request failed with {result.Result.StatusCode}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ");
                            })
                            .ExecuteAsync(() => httpClient.GetAsync(nextUrl));

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<FundaApi>(jsonString);
                        if (result != null)
                        {
                            if (result.Objects.Any())
                            {
                                objectsForSale.AddRange(result.Objects);
                                totalPages = result.Paging.TotalPages;
                            }

                            currentPage++;
                            nextUrl = $"{_urlBase}&page={currentPage}&pagesize=25";
                        }
                    }
                    else
                        Console.WriteLine($"Response failed. Status code {response.StatusCode}");
                    
                } while (currentPage < totalPages);

               
                return objectsForSale;


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message}");
                return null;
            }
        }

        private static void HandleResult(IEnumerable<ObjectForSale> objectsForSale)
        {
            var table = objectsForSale.GroupBy(x => x.RealEstateAgentId)
                                        .Select(g => new ObjectForSale
                                        {
                                            RealEstateAgentId = g.Key,
                                            RealStateAgent = g.First().RealStateAgent,
                                            Quantity = g.Count()
                                        }).OrderByDescending(o => o.Quantity).Take(10);

            Console.WriteLine("-------------------------------");
            Console.WriteLine("     Name     |    Quantity    ");
            Console.WriteLine("-------------------------------");

            foreach (var item in table)
                Console.WriteLine(String.Format("{0,-10} | {1,-10}", item.RealStateAgent, item.Quantity));

            Console.WriteLine("-------------------------------");
        }
    }
}
