using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CircuitLimitTest
{
    public class DatabaseTest
    {
        private readonly string _dbPath;

        public DatabaseTest()
        {
            var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CircuitLimitData");
            _dbPath = Path.Combine(outputDir, "CircuitLimitData.db");
        }

        public async Task<bool> TestDatabaseStorage()
        {
            try
            {
                Console.WriteLine("🗄️ Starting Database Storage Test");
                
                // Create database and table
                await CreateDatabase();
                
                // Load test data from JSON file
                var testData = await LoadTestData();
                if (testData == null || testData.Count == 0)
                {
                    Console.WriteLine("❌ No test data found");
                    return false;
                }

                // Store data in database
                await StoreDataInDatabase(testData);
                
                // Verify data was stored correctly
                var storedCount = await VerifyStoredData();
                
                Console.WriteLine($"✅ Database test completed. Stored {storedCount} records.");
                return storedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database test failed: {ex.Message}");
                return false;
            }
        }

        private async Task CreateDatabase()
        {
            var connectionString = $"Data Source={_dbPath};Version=3;";
            
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS CircuitLimitData (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InstrumentToken TEXT NOT NULL,
                    TradingSymbol TEXT NOT NULL,
                    Strike DECIMAL NOT NULL,
                    OptionType TEXT NOT NULL,
                    Expiry DATE NOT NULL,
                    Timestamp DATETIME NOT NULL,
                    LastPrice DECIMAL NOT NULL,
                    Open DECIMAL NOT NULL,
                    High DECIMAL NOT NULL,
                    Low DECIMAL NOT NULL,
                    Volume INTEGER NOT NULL,
                    OpenInterest INTEGER NOT NULL,
                    LowerCircuitLimit DECIMAL NOT NULL,
                    UpperCircuitLimit DECIMAL NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            using var command = new SQLiteCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();
            
            Console.WriteLine("📊 Database table created successfully");
        }

        private async Task<List<TestDataPoint>?> LoadTestData()
        {
            try
            {
                var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CircuitLimitData");
                var files = Directory.GetFiles(outputDir, "CircuitLimitData_*.json");
                
                if (files.Length == 0)
                {
                    Console.WriteLine("❌ No JSON data files found");
                    return null;
                }

                var latestFile = files[^1]; // Get the latest file
                var jsonContent = await File.ReadAllTextAsync(latestFile);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var data = JsonSerializer.Deserialize<List<TestDataPoint>>(jsonContent, options);
                Console.WriteLine($"📁 Loaded {data?.Count ?? 0} records from {Path.GetFileName(latestFile)}");
                
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load test data: {ex.Message}");
                return null;
            }
        }

        private async Task StoreDataInDatabase(List<TestDataPoint> data)
        {
            var connectionString = $"Data Source={_dbPath};Version=3;";
            
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var insertSql = @"
                INSERT INTO CircuitLimitData 
                (InstrumentToken, TradingSymbol, Strike, OptionType, Expiry, Timestamp, 
                 LastPrice, Open, High, Low, Volume, OpenInterest, LowerCircuitLimit, UpperCircuitLimit)
                VALUES 
                (@InstrumentToken, @TradingSymbol, @Strike, @OptionType, @Expiry, @Timestamp,
                 @LastPrice, @Open, @High, @Low, @Volume, @OpenInterest, @LowerCircuitLimit, @UpperCircuitLimit)";

            foreach (var item in data)
            {
                using var command = new SQLiteCommand(insertSql, connection);
                command.Parameters.AddWithValue("@InstrumentToken", item.InstrumentToken);
                command.Parameters.AddWithValue("@TradingSymbol", item.TradingSymbol);
                command.Parameters.AddWithValue("@Strike", item.Strike);
                command.Parameters.AddWithValue("@OptionType", item.OptionType);
                command.Parameters.AddWithValue("@Expiry", item.Expiry.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@Timestamp", item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@LastPrice", item.LastPrice);
                command.Parameters.AddWithValue("@Open", item.Open);
                command.Parameters.AddWithValue("@High", item.High);
                command.Parameters.AddWithValue("@Low", item.Low);
                command.Parameters.AddWithValue("@Volume", (int)item.Volume);
                command.Parameters.AddWithValue("@OpenInterest", (int)item.OpenInterest);
                command.Parameters.AddWithValue("@LowerCircuitLimit", item.LowerCircuitLimit);
                command.Parameters.AddWithValue("@UpperCircuitLimit", item.UpperCircuitLimit);

                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"   💾 Stored: {item.TradingSymbol}");
            }
        }

        private async Task<int> VerifyStoredData()
        {
            var connectionString = $"Data Source={_dbPath};Version=3;";
            
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var countSql = "SELECT COUNT(*) FROM CircuitLimitData";
            using var command = new SQLiteCommand(countSql, connection);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());

            // Also verify circuit limit calculations
            var verifySql = @"
                SELECT TradingSymbol, LastPrice, LowerCircuitLimit, UpperCircuitLimit,
                       ROUND(LastPrice * 0.8, 2) as ExpectedLower,
                       ROUND(LastPrice * 1.2, 2) as ExpectedUpper
                FROM CircuitLimitData";

            using var verifyCommand = new SQLiteCommand(verifySql, connection);
            using var reader = await verifyCommand.ExecuteReaderAsync();

            Console.WriteLine("\n🔍 Verification Results:");
            while (await reader.ReadAsync())
            {
                var symbol = reader.GetString("TradingSymbol");
                var ltp = reader.GetDecimal("LastPrice");
                var lcl = reader.GetDecimal("LowerCircuitLimit");
                var ucl = reader.GetDecimal("UpperCircuitLimit");
                var expectedLcl = reader.GetDecimal("ExpectedLower");
                var expectedUcl = reader.GetDecimal("ExpectedUpper");

                var lclMatch = Math.Abs(lcl - expectedLcl) < 0.01m;
                var uclMatch = Math.Abs(ucl - expectedUcl) < 0.01m;

                Console.WriteLine($"   {symbol}: LTP={ltp:F2}, LCL={lcl:F2} {(lclMatch ? "✅" : "❌")}, UCL={ucl:F2} {(uclMatch ? "✅" : "❌")}");
            }

            return count;
        }

        public async Task GenerateDatabaseReport()
        {
            try
            {
                var connectionString = $"Data Source={_dbPath};Version=3;";
                
                using var connection = new SQLiteConnection(connectionString);
                await connection.OpenAsync();

                var reportSql = @"
                    SELECT 
                        TradingSymbol,
                        OptionType,
                        Strike,
                        LastPrice,
                        LowerCircuitLimit,
                        UpperCircuitLimit,
                        ROUND(((UpperCircuitLimit - LowerCircuitLimit) / LastPrice) * 100, 2) as CircuitRangePercent,
                        Volume,
                        OpenInterest,
                        Expiry,
                        CreatedAt
                    FROM CircuitLimitData 
                    ORDER BY TradingSymbol";

                using var command = new SQLiteCommand(reportSql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CircuitLimitData");
                var reportPath = Path.Combine(outputDir, $"DatabaseReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                using var writer = new StreamWriter(reportPath);
                await writer.WriteLineAsync("Circuit Limit Database Report");
                await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await writer.WriteLineAsync(new string('=', 80));
                await writer.WriteLineAsync();

                while (await reader.ReadAsync())
                {
                    await writer.WriteLineAsync($"Symbol: {reader.GetString("TradingSymbol")}");
                    await writer.WriteLineAsync($"Type: {reader.GetString("OptionType")} | Strike: {reader.GetDecimal("Strike")}");
                    await writer.WriteLineAsync($"LTP: {reader.GetDecimal("LastPrice"):F2}");
                    await writer.WriteLineAsync($"Circuit Limits: {reader.GetDecimal("LowerCircuitLimit"):F2} - {reader.GetDecimal("UpperCircuitLimit"):F2} ({reader.GetDecimal("CircuitRangePercent"):F1}%)");
                    await writer.WriteLineAsync($"Volume: {reader.GetInt32("Volume")} | OI: {reader.GetInt32("OpenInterest")}");
                    await writer.WriteLineAsync($"Expiry: {reader.GetString("Expiry")}");
                    await writer.WriteLineAsync($"Stored: {reader.GetString("CreatedAt")}");
                    await writer.WriteLineAsync(new string('-', 40));
                }

                Console.WriteLine($"📊 Database report generated: {reportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to generate database report: {ex.Message}");
            }
        }
    }
} 