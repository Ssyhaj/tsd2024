using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using GoldSavings.App.Model;
using GoldSavings.App.Client;
using GoldSavings.App.Services;
using System.Threading.Tasks;

namespace GoldSavings.App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, Gold Investor!");

        GoldDataService dataService = new GoldDataService();
        DateTime startDate = new DateTime(2019, 01, 01);
        DateTime endDate = DateTime.Now;
        Console.WriteLine(endDate);
        List<GoldPrice> goldPrices = new List<GoldPrice>();

        for (int year = startDate.Year; year < endDate.Year; year++)
        {
            DateTime yearStart = new DateTime(year, 1, 1);
            DateTime yearEnd = new DateTime(year, 12, 31);
            var yearlyData = await dataService.GetGoldPrices(yearStart, yearEnd);
            goldPrices.AddRange(yearlyData);
            Console.WriteLine($"Retrieved {yearlyData.Count} records for {year}.");
        }
        for (int year = 2025; year <= endDate.Year; year++)
        {
            DateTime yearStart = new DateTime(year, 1, 1);
            DateTime yearEnd = DateTime.Now;
            var yearlyData = await dataService.GetGoldPrices(yearStart, yearEnd);
            goldPrices.AddRange(yearlyData);
            Console.WriteLine($"Retrieved {yearlyData.Count} records for {year}.");
        }

        if (goldPrices.Count == 0)
        {
            Console.WriteLine("No data found. Exiting.");
            return;
        }

        Console.WriteLine($"Retrieved: {goldPrices.Count} records. Ready for analysis.");

        GoldAnalysisService analysisService = new GoldAnalysisService(goldPrices);
        var avgPrice = analysisService.GetAveragePrice();
        GoldResultPrinter.PrintSingleValue(Math.Round(avgPrice, 2), "Average Gold Price");

        // LINQ Queries
        var top3Highest = goldPrices
            .Where(g => g.Date >= DateTime.Now.AddYears(-1))
            .OrderByDescending(g => g.Price)
            .Take(3);

        var top3Lowest = goldPrices
            .Where(g => g.Date >= DateTime.Now.AddYears(-1))
            .OrderBy(g => g.Price)
            .Take(3);

        Console.WriteLine("Top 3 Highest Prices:");
        foreach (var price in top3Highest) Console.WriteLine(price.Date + ": " + price.Price);

        Console.WriteLine("Top 3 Lowest Prices:");
        foreach (var price in top3Lowest) Console.WriteLine(price.Date + ": " + price.Price);

        // January 2020 >5% profit
        var jan2020Price = goldPrices.FirstOrDefault(g => g.Date.Year == 2020 && g.Date.Month == 1)?.Price;
        if (jan2020Price.HasValue)
        {
            var profitableDays = goldPrices.Where(g => g.Price > jan2020Price * 1.05 && g.Date.Year >=2020).Select(g => g.Date);
            Console.WriteLine("Days when profit exceeded 5% since Jan 2020:");
            //foreach (var date in profitableDays) Console.WriteLine(date);
        }

        // Second ten ranking prices 
        var rankedPrices = goldPrices.Where(g => g.Date.Year >= 2019 && g.Date.Year <= 2022)
                                     .OrderByDescending(g => g.Price)
                                     .Skip(10).Take(3);
        Console.WriteLine("3 Dates opening the second ten ranking (2019-2022):");
        foreach (var price in rankedPrices) Console.WriteLine(price.Date + ": " + price.Price);

        // Average of gold prices for 2020, 2023, 2024  
        var years = new[] { 2020, 2023, 2024 };
        foreach (var year in years)
        {
            var avg = goldPrices.Where(g => g.Date.Year == year).Select(g => g.Price).DefaultIfEmpty(0).Average();
            Console.WriteLine($"Average Price in {year}: {avg}");
        }

        // Best buy and sell dates
        var priceRange = goldPrices.Where(g => g.Date.Year >= 2020 && g.Date.Year <= 2024).ToList();
        var minPrice = priceRange.MinBy(g => g.Price);
        var maxPrice = priceRange.Where(g => g.Date > minPrice.Date).MaxBy(g => g.Price);
        if (minPrice != null && maxPrice != null)
        {
            var roi = ((maxPrice.Price - minPrice.Price) / minPrice.Price) * 100;
            Console.WriteLine($"Best Buy Date: {minPrice.Date}, Price: {minPrice.Price}");
            Console.WriteLine($"Best Sell Date: {maxPrice.Date}, Price: {maxPrice.Price}");
            Console.WriteLine($"Potential ROI: {roi:F2}%");
        }

        // Save to XML
        SaveToXml(goldPrices, "gold_prices.xml");
        
        Console.WriteLine("Gold prices saved to XML file.");
        string filePath = Path.GetFullPath("gold_prices.xml");
        Console.WriteLine($"The gold_prices.xml file is located at: {filePath}");

        // Read XML in one instruction
        var loadedGoldPrices = (List<GoldPrice>)new XmlSerializer(typeof(List<GoldPrice>)).Deserialize(new StreamReader("gold_prices.xml"));
        Console.WriteLine("Gold prices loaded from XML file.");

        Console.WriteLine("Loaded Gold Prices from XML:");
        /*foreach (var price in loadedGoldPrices)
        {
            Console.WriteLine(price.Date + ": " + price.Price);
        }
        */
    }

    static void SaveToXml(List<GoldPrice> prices, string filename)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(List<GoldPrice>));
        using (StreamWriter writer = new StreamWriter(filename))
        {
            serializer.Serialize(writer, prices);
        }
    }

}
