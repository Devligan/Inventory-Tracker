using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Inventory
{
    private Dictionary<string, Item> items = new();
    private DateOnly date;

    public Inventory()
    {
        Load();
        LogDateSection();
    }

    private string GenerateKey(Item item) =>
        item is PerishableItem p ? $"{p.Name};{p.ExpirationDate}" : item.Name;

    public void AddItem(Item item)
    {
        string key = GenerateKey(item);
        if (items.TryGetValue(key, out var existing))
            existing.AddItem(item.Quantity);
        else
            items[key] = item;

        LogAddition(item);
        RemoveExpiredItems();
    }

    public void SetDate(DateOnly newDate)
    {
        if (date < newDate)
        {
            date = newDate;
            LogDateSection();
            RemoveExpiredItems();
        }
        else
        {
            Console.WriteLine("Date cannot be set in the past.");
        }
    }

    public void GetDate() => Console.WriteLine($"Current date is: {date}");

    public void UseNonPerishable(string name, int quantity)
    {
        if (!items.TryGetValue(name, out var item))
        {
            Console.WriteLine("Item does not exist.");
            return;
        }
        if (item.Quantity < quantity)
        {
            Console.WriteLine("Not enough stock.");
            return;
        }
        item.TakeItem(quantity);
        if (item.Quantity == 0)
            items.Remove(name);

        LogRemoval(name, quantity);
        Update();
    }

    public void UsePerishable(string name, int quantity)
    {
        var matches = items
            .Where(e => e.Value is PerishableItem p && p.Name == name)
            .ToDictionary(e => e.Key, e => e.Value);

        if (matches.Count == 0)
        {
            Console.WriteLine("No item found with that name.");
            return;
        }

        double totalAvailable = matches.Values.Sum(i => i.Quantity);
        if (totalAvailable < quantity)
        {
            Console.WriteLine("Not enough stock to fulfill the request.");
            return;
        }

        int remaining = quantity;
        while (remaining > 0)
        {
            var earliest = matches
                .OrderBy(e => ((PerishableItem)e.Value).ExpirationDate)
                .First();

            double available = earliest.Value.Quantity;
            if (available <= remaining)
            {
                remaining -= (int)available;
                items.Remove(earliest.Key);
                matches.Remove(earliest.Key);
            }
            else
            {
                earliest.Value.TakeItem(remaining);
                remaining = 0;
            }
        }

        LogRemoval(name, quantity);
        Update();
    }

    public void ListByExpiration()
    {
        var sorted = items.Values.ToList();
        sorted.Sort(new ItemComparer.ExpirationDateComparator());
        foreach (var item in sorted)
            Console.WriteLine(item.GetDetails());
    }

    public void ListByAlphabetical()
    {
        var totals = new Dictionary<string, double>();
        foreach (var item in items.Values)
        {
            if (totals.ContainsKey(item.Name))
                totals[item.Name] += item.Quantity;
            else
                totals[item.Name] = item.Quantity;
        }
        foreach (var name in totals.Keys.OrderBy(n => n))
            Console.WriteLine($"{name} | Qty: {totals[name]}");
    }

    public void GetItemInfo(string name)
    {
        bool found = false;
        foreach (var item in items.Values.Where(i => i.Name == name))
        {
            Console.WriteLine(item.GetDetails());
            found = true;
        }
        if (!found)
            Console.WriteLine($"No item found with name: {name}");
    }

    public void GetItemInfo(string name, DateOnly? expirationDate)
    {
        bool found = false;
        if (expirationDate == null)
        {
            foreach (var item in items.Values.OfType<NonPerishableItem>().Where(p => p.Name == name))
            {
                Console.WriteLine(item.GetDetails());
                found = true;
            }
        }
        else
        {
            foreach (var item in items.Values.OfType<PerishableItem>()
                         .Where(p => p.Name == name && p.ExpirationDate == expirationDate))
            {
                Console.WriteLine(item.GetDetails());
                found = true;
            }
        }
        if (!found)
            Console.WriteLine($"No perishable item found with name: {name} and expiration: {expirationDate}");
    }

    public void Update()
    {
        try
        {
            using var writer = new StreamWriter("data.txt", false);
            writer.WriteLine($"DATE: {date}");
            foreach (var item in items.Values)
                writer.WriteLine(item.GetDetails());
        }
        catch (IOException e)
        {
            Console.WriteLine($"Error saving inventory: {e.Message}");
        }
    }

    public void LogDateSection()
    {
        try
        {
            using var writer = new StreamWriter("log.txt", true);
            writer.WriteLine($"\n=== {date} ===");
        }
        catch (IOException e)
        {
            Console.WriteLine($"Error writing log date: {e.Message}");
        }
    }

    public void LogAddition(Item item)
    {
        try
        {
            using var writer = new StreamWriter("log.txt", true);
            writer.WriteLine($"ADDED: {item.GetDetails()}");
        }
        catch (IOException e)
        {
            Console.WriteLine($"Error logging addition: {e.Message}");
        }
    }

    public void LogRemoval(string name, int quantity)
    {
        try
        {
            using var writer = new StreamWriter("log.txt", true);
            writer.WriteLine($"REMOVED: {name} | Qty: {quantity}");
        }
        catch (IOException e)
        {
            Console.WriteLine($"Error logging removal: {e.Message}");
        }
    }

    public void LogExpiration(PerishableItem item)
    {
        try
        {
            using var writer = new StreamWriter("log.txt", true);
            writer.WriteLine($"EXPIRED: {item.GetDetails()}");
        }
        catch (IOException e)
        {
            Console.WriteLine($"Error logging expiration: {e.Message}");
        }
    }

    public void RemoveExpiredItems()
    {
        var expired = items
            .Where(e => e.Value is PerishableItem p && p.ExpirationDate <= date)
            .ToList();

        foreach (var entry in expired)
        {
            LogExpiration((PerishableItem)entry.Value);
            items.Remove(entry.Key);
        }
        Update();
    }

    public void Load()
    {
        items.Clear();
        date = DateOnly.MinValue;

        if (!File.Exists("data.txt")) return;

        try
        {
            using var reader = new StreamReader("data.txt");
            string? line = reader.ReadLine();
            if (line != null && line.StartsWith("DATE:"))
                date = DateOnly.Parse(line.Replace("DATE:", "").Trim());

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('|');
                if (parts[0].Trim() == "Non-Perishable")
                {
                    string name = parts[1].Trim();
                    double qty = double.Parse(parts[2].Replace("Qty:", "").Trim());
                    AddItemInternal(new NonPerishableItem(name, qty));
                }
                else if (parts[0].Trim() == "Perishable")
                {
                    string name = parts[1].Trim();
                    double qty = double.Parse(parts[2].Replace("Qty:", "").Trim());
                    DateOnly exp = DateOnly.Parse(parts[3].Replace("Expires:", "").Trim());
                    AddItemInternal(new PerishableItem(name, qty, exp));
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine($"Error loading inventory: {e.Message}");
        }
    }

    private void AddItemInternal(Item item)
    {
        string key = GenerateKey(item);
        if (items.TryGetValue(key, out var existing))
            existing.AddItem(item.Quantity);
        else
            items[key] = item;
    }
}