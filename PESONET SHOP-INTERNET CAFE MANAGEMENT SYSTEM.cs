using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace PisonetShop
{
    
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


    public class User : BaseEntity
    {
       
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }


    public class Computer : BaseEntity
    {
    
        public bool IsOccupied { get; set; }
        public decimal RatePerHour { get; set; } = 30m;
    }

    public class Product : BaseEntity
    {
        
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public class Session
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int ComputerId { get; set; }
        public string CustomerName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal RatePerHour { get; set; }

        [JsonIgnore]
        public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;

        public decimal ComputeAmount()
        {
            double minutes = Math.Ceiling(Duration.TotalMinutes);
            return Math.Round((decimal)minutes * (RatePerHour / 60m), 2);
        }
    }

    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Details { get; set; }
    }

    public class ShopData
    {
        public List<User> Users { get; set; } = new();
        public List<Computer> Computers { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Session> Sessions { get; set; } = new();
        public List<Transaction> Transactions { get; set; } = new();
    }

    public static class Persistence
    {
        private static string FilePath => Path.Combine(AppContext.BaseDirectory, "pisonet_data.json");

        public static ShopData Load()
        {
            if (!File.Exists(FilePath))
            {
                var data = CreateDefaultData();
                Save(data);
                return data;
            }

            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<ShopData>(json) ?? CreateDefaultData();
            }
            catch
            {
                
                return CreateDefaultData();
            }
        }

        public static void Save(ShopData data)
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static ShopData CreateDefaultData()
        {
            var data = new ShopData();
          
            data.Users.Add(new User { Id = 1, Name = "admin", Username = "admin", Password = "admin123", Role = "Admin" });
            data.Users.Add(new User { Id = 2, Name = "staff", Username = "staff", Password = "staff123", Role = "Staff" });

            for (int i = 1; i <= 8; i++)
                data.Computers.Add(new Computer { Id = i, Name = $"PC-{i:D2}" });

            data.Products.Add(new Product { Id = 1, Name = "Bottled Water", Price = 20m, Stock = 30 });
            data.Products.Add(new Product { Id = 2, Name = "Snack", Price = 35m, Stock = 20 });

            return data;
        }
    }

    class Program
    {
        static ShopData Data;
        static User CurrentUser;

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = " Pisonet Shop System ";

            Data = Persistence.Load();
            Login();
            MainMenu();
        }

        static void Login()
        {
            while (CurrentUser == null)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("====================================");
                Console.WriteLine("          ADMIN / STAFF             ");
                Console.WriteLine("====================================");
                Console.ResetColor();

                Console.Write("Username: ");
                string username = Console.ReadLine();
                Console.Write("Password: ");
                string password = ReadPassword();

                CurrentUser = Data.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

                if (CurrentUser == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n❌ Invalid login. Try again!");
                    Console.ResetColor();
                    Console.ReadKey();
                }
            }
        }

        static string ReadPassword()
        {
            string pass = "";
            ConsoleKey key;

            do
            {
                var info = Console.ReadKey(true);
                key = info.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass[..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(info.KeyChar))
                {
                    pass += info.KeyChar;
                    Console.Write("*");
                }
            } while (key != ConsoleKey.Enter);

            Console.WriteLine();
            return pass;
        }

        static void MainMenu()
        {
            string[] menu =
            {
                "Manage Computers",
                "Manage Products",
                "Start Session",
                "Stop Session",
                "Sell Product",
                "View Reports",
                "Exit"
            };

            while (true)
            {
                int index = NavigateMenu(menu);

                switch (index)
                {
                    case 0: ManageComputers(); break;
                    case 1: ManageProducts(); break;
                    case 2: StartSession(); break;
                    case 3: StopSession(); break;
                    case 4: SellProduct(); break;
                    case 5: ShowReports(); break;
                    case 6: Persistence.Save(Data); return;
                }
            }
        }

        static int NavigateMenu(string[] options)
        {
            int index = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("-----------------------------------------");
                Console.WriteLine("|           Enter to select             |");
                Console.WriteLine("-----------------------------------------");
                Console.ResetColor();

                for (int i = 0; i < options.Length; i++)
                {
                    Console.ForegroundColor = (i == index) ? ConsoleColor.Green : ConsoleColor.White;
                    Console.WriteLine($"{i + 1}. {options[i]}");
                }

                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow) index = (index == 0) ? options.Length - 1 : index - 1;
                if (key == ConsoleKey.DownArrow) index = (index + 1) % options.Length;

            } while (key != ConsoleKey.Enter);

            return index;
        }

        static void Pause(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n" + msg);
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        
        static void ManageComputers()
        {
            while (true)
            {
                string[] baseOptions = Data.Computers
                    .Select(c => $"{c.Name} - {(c.IsOccupied ? "Occupied" : "Available")} - ₱{c.RatePerHour:N2}/hr")
                    .Concat(new[] { "Add Computer", "Edit Computer", "Remove Computer", "Back" })
                    .ToArray();

                int index = NavigateMenu(baseOptions);

                if (index == baseOptions.Length - 1) break;
                if (index == baseOptions.Length - 4) AddComputer();
                else if (index == baseOptions.Length - 3) EditComputer();
                else if (index == baseOptions.Length - 2) RemoveComputer();
            }
        }

        static void AddComputer()
        {
            Console.Write("Computer Name: ");
            string name = Console.ReadLine();

            Console.Write("Rate per hour: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal rate))
            {
                Pause("❌ Invalid rate.");
                return;
            }

            int id = Data.Computers.Any() ? Data.Computers.Max(c => c.Id) + 1 : 1;
            Data.Computers.Add(new Computer { Id = id, Name = name, RatePerHour = rate });

            Persistence.Save(Data);
            Pause("✅ Computer added!");
        }

        static void EditComputer()
        {
            if (!Data.Computers.Any()) { Pause("No computers available."); return; }

            int sel = NavigateMenu(Data.Computers.Select(c => c.Name).ToArray());
            if (sel == -1) return;

            var c = Data.Computers[sel];

            Console.Write($"New name ({c.Name}): ");
            string newName = Console.ReadLine();

            Console.Write($"New rate ({c.RatePerHour:N2}): ");
            if (decimal.TryParse(Console.ReadLine(), out decimal rate))
                c.RatePerHour = rate;

            if (!string.IsNullOrWhiteSpace(newName))
                c.Name = newName;

            Persistence.Save(Data);
            Pause("✅ Computer updated!");
        }

        static void RemoveComputer()
        {
            if (!Data.Computers.Any()) { Pause("No computers available."); return; }

            int sel = NavigateMenu(Data.Computers.Select(c => c.Name).ToArray());
            if (sel == -1) return;

            var c = Data.Computers[sel];

            if (c.IsOccupied)
            {
                Pause("❌ Cannot remove occupied PC.");
                return;
            }

            Data.Computers.Remove(c);
            Persistence.Save(Data);
            Pause("🗑️ Computer removed!");
        }


        static void ManageProducts()
        {
            while (true)
            {
                string[] baseOptions = Data.Products
                    .Select(p => $"{p.Name} - ₱{p.Price:N2} (Stock: {p.Stock})")
                    .Concat(new[] { "Add Product", "Edit Product", "Remove Product", "Back" })
                    .ToArray();

                int index = NavigateMenu(baseOptions);

                if (index == baseOptions.Length - 1) break;
                if (index == baseOptions.Length - 4) AddProduct();
                else if (index == baseOptions.Length - 3) EditProduct();
                else if (index == baseOptions.Length - 2) RemoveProduct();
            }
        }

        static void AddProduct()
        {
            Console.Write("Product Name: ");
            string name = Console.ReadLine();

            Console.Write("Price: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                Pause("Invalid price.");
                return;
            }

            Console.Write("Stock: ");
            if (!int.TryParse(Console.ReadLine(), out int stock))
            {
                Pause("Invalid stock.");
                return;
            }

            int id = Data.Products.Any() ? Data.Products.Max(p => p.Id) + 1 : 1;
            Data.Products.Add(new Product { Id = id, Name = name, Price = price, Stock = stock });

            Persistence.Save(Data);
            Pause("✅ Product added!");
        }

        static void EditProduct()
        {
            if (!Data.Products.Any()) { Pause("No products to edit."); return; }

            int sel = NavigateMenu(Data.Products.Select(p => p.Name).ToArray());
            if (sel == -1) return;

            var product = Data.Products[sel];

            Console.Write($"New name ({product.Name}): ");
            string newName = Console.ReadLine();

            Console.Write($"New price ({product.Price:N2}): ");
            if (decimal.TryParse(Console.ReadLine(), out decimal price))
                product.Price = price;

            Console.Write($"New stock ({product.Stock}): ");
            if (int.TryParse(Console.ReadLine(), out int stock))
                product.Stock = stock;

            if (!string.IsNullOrWhiteSpace(newName))
                product.Name = newName;

            Persistence.Save(Data);
            Pause("✅ Product updated!");
        }

        static void RemoveProduct()
        {
            if (!Data.Products.Any()) { Pause("No products to remove."); return; }

            int sel = NavigateMenu(Data.Products.Select(p => p.Name).ToArray());
            if (sel == -1) return;

            var p = Data.Products[sel];
            Data.Products.Remove(p);

            Persistence.Save(Data);
            Pause("🗑️ Product removed!");
        }

       

        static void StartSession()
        {
            while (true)
            {
                var free = Data.Computers.Where(c => !c.IsOccupied).ToList();
                if (!free.Any()) { Pause("No free PCs available."); return; }

                string[] options = free.Select(c => $"{c.Name} - ₱{c.RatePerHour:N2}/hr").Concat(new[] { "Back" }).ToArray();
                int sel = NavigateMenu(options);
                if (sel == options.Length - 1) break;

                var pc = free[sel];

                Console.Write("Customer name (Enter = Walk-in): ");
                string cust = Console.ReadLine();

                var session = new Session
                {
                    ComputerId = pc.Id,
                    CustomerName = string.IsNullOrWhiteSpace(cust) ? "Walk-in" : cust,
                    StartTime = DateTime.Now,
                    RatePerHour = pc.RatePerHour
                };

                pc.IsOccupied = true;
                Data.Sessions.Add(session);
                Persistence.Save(Data);

                Pause($"💻 Session started on {pc.Name}");
            }
        }

        static void StopSession()
        {
            while (true)
            {
                var active = Data.Sessions.Where(s => s.EndTime == null).ToList();
                if (!active.Any()) { Pause("No active sessions."); return; }

                string[] options = active.Select(s =>
                {
                    var pc = Data.Computers.First(c => c.Id == s.ComputerId);
                    return $"{pc.Name} - {s.CustomerName} (Started: {s.StartTime:HH:mm})";
                }).Concat(new[] { "Back" }).ToArray();

                int sel = NavigateMenu(options);
                if (sel == options.Length - 1) break;

                var session = active[sel];
                session.EndTime = DateTime.Now;

                var pcRef = Data.Computers.First(c => c.Id == session.ComputerId);
                pcRef.IsOccupied = false;

                decimal total = session.ComputeAmount();

                Data.Transactions.Add(new Transaction
                {
                    Type = "Session",
                    Amount = total,
                    Details = $"{pcRef.Name} - {session.CustomerName}"
                });

                Persistence.Save(Data);
                Pause($"Total: ₱{total:N2}");
            }
        }

        static void SellProduct()
        {
            if (!Data.Products.Any()) { Pause("No products."); return; }

            int sel = NavigateMenu(Data.Products.Select(p => $"{p.Name} - ₱{p.Price:N2} (Stock: {p.Stock})").ToArray());
            if (sel == -1) return;

            var prod = Data.Products[sel];

            Console.Write("Quantity: ");
            if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0 || qty > prod.Stock)
            {
                Pause("Invalid quantity.");
                return;
            }

            prod.Stock -= qty;
            decimal total = prod.Price * qty;

            Data.Transactions.Add(new Transaction
            {
                Type = "Product",
                Amount = total,
                Details = $"{qty}x {prod.Name}"
            });

            Persistence.Save(Data);
            Pause($"✅ Sold! Total: ₱{total:N2}");
        }

        
       
        static DateTime GetStartOfWeek(DateTime date)
        {
          
            DayOfWeek firstDay = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int diff = (date.DayOfWeek - firstDay + 7) % 7;
            return date.AddDays(-diff).Date;
        }

     
        static (decimal Income, int Players) GetDailyPlayerCountAndIncome(DateTime date)
        {
           
            decimal income = Data.Transactions
                .Where(t => t.Timestamp.Date == date.Date)
                .Sum(t => t.Amount);

       
            int playerCount = Data.Transactions
                .Where(t => t.Timestamp.Date == date.Date && t.Type == "Session")
                .Count();

            return (income, playerCount);
        }

        static void ShowReports()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("====================================");
            Console.WriteLine("          TRANSACTIONS REPORT       ");
            Console.WriteLine("====================================");
            Console.ResetColor();

      
            var orderedTransactions = Data.Transactions.OrderByDescending(t => t.Timestamp).ToList();
            Console.WriteLine($"{"Date/Time",-20} | {"Type",-10} | {"Amount",-10} | Details");
            Console.WriteLine("------------------------------------------------------------------");
            foreach (var t in orderedTransactions.Take(20)) 
                Console.WriteLine($"{t.Timestamp:MM/dd/yyyy HH:mm,-20} | {t.Type,-10} | ₱{t.Amount,10:N2} | {t.Details}");

            Console.WriteLine("\n====================================");
            Console.WriteLine("          INCOME & TRAFFIC SUMMARY  ");
            Console.WriteLine("====================================");

           
            DateTime today = DateTime.Today;
            DateTime startOfWeek = GetStartOfWeek(today);

          
            var transactionsThisYear = Data.Transactions
                .Where(t => t.Timestamp.Year == today.Year)
                .ToList();

            
            var todayData = GetDailyPlayerCountAndIncome(today);
            decimal todayIncome = todayData.Income;
            int todayPlayers = todayData.Players;

          

            decimal yearIncome = transactionsThisYear.Sum(t => t.Amount);

            decimal monthIncome = transactionsThisYear
                .Where(t => t.Timestamp.Month == today.Month)
                .Sum(t => t.Amount);

            decimal weekIncome = Data.Transactions
                .Where(t => t.Timestamp.Date >= startOfWeek)
                .Sum(t => t.Amount);

         

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Total Income (All Time): ₱{Data.Transactions.Sum(t => t.Amount):N2}");
            Console.WriteLine("------------------------------------");

           
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Today's Income:        ₱{todayIncome:N2} (Players: {todayPlayers})");
            Console.ResetColor();

          
            Console.WriteLine($"This Week's Income:    ₱{weekIncome:N2}");
            Console.WriteLine($"This Month's Income:   ₱{monthIncome:N2}");
            Console.WriteLine($"This Year's Income:    ₱{yearIncome:N2}");
            Console.ResetColor();

            Pause("");
        }
    }
}